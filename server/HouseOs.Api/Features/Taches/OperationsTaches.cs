using HouseOs.Api.Domaine;
using HouseOs.Api.Features.Auth;
using HouseOs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HouseOs.Api.Features.Taches;

public record ErreurValidation(string Champ, string Message);

public enum StatutCompletion { Introuvable, DejaCompletee, Ok }

public record ResultatCompletion(StatutCompletion Statut, Occurrence? Prochaine);

/// <summary>
/// Logique de la tranche Taches partagée entre les endpoints REST et les outils MCP.
/// Les erreurs sont des valeurs (jamais des IResult) : chaque consommateur les traduit
/// dans son protocole (ValidationProblem côté REST, McpException côté MCP).
/// </summary>
public static class OperationsTaches
{
    /// <summary>Valide la requête et ajoute la tâche au contexte, sans SaveChanges.</summary>
    public static (Tache? Tache, ErreurValidation? Erreur) PreparerTache(
        HouseOsDbContext db,
        CreerTacheRequete requete,
        Guid creeParId,
        DateTimeOffset maintenant,
        DateOnly aujourdhui)
    {
        if (string.IsNullOrWhiteSpace(requete.Titre))
        {
            return (null, new ErreurValidation("titre", "Le titre est requis."));
        }

        var (spec, erreur) = ConvertirRecurrence(requete.Recurrence);
        if (erreur is not null)
        {
            return (null, new ErreurValidation("recurrence", erreur));
        }
        var (strategie, erreurStrategie) = ConvertirStrategie(requete.Strategie);
        if (erreurStrategie is not null)
        {
            return (null, new ErreurValidation("strategie", erreurStrategie));
        }

        var titre = requete.Titre.Trim();
        var description = string.IsNullOrWhiteSpace(requete.Description) ? null : requete.Description.Trim();

        var tache = spec.Mode == ModeRecurrence.Ponctuelle
            ? Tache.CreerPonctuelle(titre, description, requete.Echeance, requete.AssigneAId,
                creeParId, maintenant)
            : Tache.CreerRecurrente(titre, description, spec, strategie, requete.AssigneAId,
                requete.Echeance, creeParId, maintenant, aujourdhui);

        tache.ZoneId = requete.ZoneId;
        tache.EquipementId = requete.EquipementId;

        db.Taches.Add(tache);
        return (tache, null);
    }

    /// <summary>
    /// Applique une modification à une tâche déjà chargée (avec ses occurrences en attente
    /// incluses), sans SaveChanges. Réaligne l'occurrence en attente sur la nouvelle définition.
    /// </summary>
    public static async Task<ErreurValidation?> ModifierTacheAsync(
        HouseOsDbContext db,
        Tache tache,
        ModifierTacheRequete requete,
        DateOnly aujourdhui)
    {
        if (string.IsNullOrWhiteSpace(requete.Titre))
        {
            return new ErreurValidation("titre", "Le titre est requis.");
        }

        var (spec, erreur) = ConvertirRecurrence(requete.Recurrence);
        if (erreur is not null)
        {
            return new ErreurValidation("recurrence", erreur);
        }
        var (strategie, erreurStrategie) = ConvertirStrategie(requete.Strategie);
        if (erreurStrategie is not null)
        {
            return new ErreurValidation("strategie", erreurStrategie);
        }

        tache.Titre = requete.Titre.Trim();
        tache.Description = string.IsNullOrWhiteSpace(requete.Description) ? null : requete.Description.Trim();
        tache.AssigneAId = requete.AssigneAId;
        tache.ZoneId = requete.ZoneId;
        tache.EquipementId = requete.EquipementId;
        tache.Strategie = strategie;
        tache.Recurrence = spec;

        // Réaligner l'occurrence en attente sur la nouvelle définition.
        var enAttente = tache.Occurrences.SingleOrDefault(o => o.Statut == StatutOccurrence.EnAttente);
        if (enAttente is not null)
        {
            enAttente.Echeance = spec.Mode switch
            {
                ModeRecurrence.Ponctuelle => requete.Echeance,
                _ => requete.Echeance ?? await RecalculerEcheance(db, tache, spec, aujourdhui),
            };
            enAttente.AssigneAId = requete.AssigneAId ?? enAttente.AssigneAId;
        }

        return null;
    }

    /// <summary>
    /// Complète une occurrence au nom d'un utilisateur : journal, puis matérialisation de la
    /// prochaine occurrence si la tâche est récurrente. Fait SaveChanges.
    /// </summary>
    public static async Task<ResultatCompletion> CompleterAsync(
        HouseOsDbContext db,
        Guid occurrenceId,
        Guid utilisateurId,
        string? notes,
        DateTimeOffset maintenant)
    {
        var occurrence = await db.Occurrences
            .Include(o => o.Tache)
            .SingleOrDefaultAsync(o => o.Id == occurrenceId);
        if (occurrence is null)
        {
            return new ResultatCompletion(StatutCompletion.Introuvable, null);
        }
        if (occurrence.Statut == StatutOccurrence.Completee)
        {
            return new ResultatCompletion(StatutCompletion.DejaCompletee, null);
        }

        var entree = occurrence.Completer(utilisateurId, maintenant, notes);
        db.Journal.Add(entree);

        Occurrence? prochaine = null;
        var tache = occurrence.Tache!;
        if (tache.Recurrence.Mode != ModeRecurrence.Ponctuelle)
        {
            var assigne = await ChoisirProchainAssigne(db, tache, utilisateurId, maintenant);
            prochaine = tache.GenererProchaineOccurrence(
                DateOnly.FromDateTime(maintenant.LocalDateTime), occurrence.Echeance, assigne);
            if (prochaine is not null)
            {
                db.Occurrences.Add(prochaine);
            }
        }

        await db.SaveChangesAsync();
        return new ResultatCompletion(StatutCompletion.Ok, prochaine);
    }

    /// <summary>Liste les occurrences selon le filtre (mêmes règles pour REST et MCP).</summary>
    public static async Task<List<OccurrenceDto>> ListerOccurrencesAsync(
        HouseOsDbContext db,
        string? filtre,
        DateOnly aujourdhui,
        DateTimeOffset? de,
        DateTimeOffset? a)
    {
        var requete = db.Occurrences.AsNoTracking()
            .Include(o => o.Tache)
            .Include(o => o.AssigneA)
            .Include(o => o.CompleteePar)
            .AsQueryable();

        requete = filtre switch
        {
            "aujourdhui" => requete.Where(o =>
                o.Statut == StatutOccurrence.EnAttente && o.Echeance != null && o.Echeance <= aujourdhui),
            "avenir" => requete.Where(o =>
                o.Statut == StatutOccurrence.EnAttente && (o.Echeance == null || o.Echeance > aujourdhui)),
            "en-attente" => requete.Where(o => o.Statut == StatutOccurrence.EnAttente),
            "completees" => requete.Where(o => o.Statut == StatutOccurrence.Completee),
            // Complétées dans une fenêtre d'instants [de, a) — le client fournit les bornes
            // de sa journée locale (le serveur ne connaît pas le fuseau du client).
            "faites" => requete.Where(o =>
                o.Statut == StatutOccurrence.Completee && o.CompleteeLe >= de && o.CompleteeLe < a),
            _ => requete,
        };

        return await requete
            .OrderBy(o => o.Echeance == null)
            .ThenBy(o => o.Echeance)
            .ThenByDescending(o => o.CompleteeLe)
            .Take(200)
            .Select(o => new OccurrenceDto(
                o.Id,
                o.TacheId,
                o.Tache!.Titre,
                o.Tache.Description,
                o.Echeance,
                o.Statut.ToString(),
                o.AssigneA == null
                    ? null
                    : new UtilisateurDto(o.AssigneA.Id, o.AssigneA.NomUtilisateur, o.AssigneA.NomAffichage),
                o.CompleteePar == null
                    ? null
                    : new UtilisateurDto(o.CompleteePar.Id, o.CompleteePar.NomUtilisateur, o.CompleteePar.NomAffichage),
                o.CompleteeLe,
                o.Tache.ZoneId,
                o.Tache.EquipementId,
                o.Tache.Recurrence.Mode.ToString()))
            .ToListAsync();
    }

    /// <summary>Applique la stratégie d'assignation pour la prochaine occurrence.</summary>
    internal static async Task<Guid?> ChoisirProchainAssigne(
        HouseOsDbContext db,
        Tache tache,
        Guid dernierCompleteurId,
        DateTimeOffset maintenant)
    {
        if (tache.Strategie == StrategieAssignation.Fixe)
        {
            return tache.AssigneAId;
        }

        var utilisateurs = await db.Utilisateurs.Select(u => u.Id).ToListAsync();
        var depuis = maintenant.AddDays(-90);
        var completions = await db.Journal
            .Where(j => j.TacheId == tache.Id && j.CompleteeLe >= depuis)
            .GroupBy(j => j.UtilisateurId)
            .Select(g => new { UtilisateurId = g.Key, Nombre = g.Count() })
            .ToDictionaryAsync(x => x.UtilisateurId, x => x.Nombre);

        return Assignation.ChoisirAssigne(
            tache.Strategie, tache.AssigneAId, dernierCompleteurId, utilisateurs, completions);
    }

    /// <summary>Échéance recalculée après édition d'une récurrence (sans complétion).</summary>
    internal static async Task<DateOnly?> RecalculerEcheance(
        HouseOsDbContext db,
        Tache tache,
        SpecRecurrence spec,
        DateOnly aujourdhui)
    {
        if (spec.Mode == ModeRecurrence.Intervalle)
        {
            var derniereCompletion = await db.Journal
                .Where(j => j.TacheId == tache.Id)
                .OrderByDescending(j => j.CompleteeLe)
                .Select(j => (DateTimeOffset?)j.CompleteeLe)
                .FirstOrDefaultAsync();
            var reference = derniereCompletion is { } d ? DateOnly.FromDateTime(d.LocalDateTime) : aujourdhui;
            return MoteurRecurrence.ProchaineEcheance(spec, reference);
        }
        return MoteurRecurrence.PremiereEcheance(spec, aujourdhui);
    }

    public static (SpecRecurrence Spec, string? Erreur) ConvertirRecurrence(RecurrenceDto? dto)
    {
        if (dto is null || dto.Mode == nameof(ModeRecurrence.Ponctuelle))
        {
            return (SpecRecurrence.Ponctuelle(), null);
        }
        if (Enum.TryParse<ModeRecurrence>(dto.Mode, out var mode) == false)
        {
            return (SpecRecurrence.Ponctuelle(), $"Mode inconnu : {dto.Mode}.");
        }

        var spec = new SpecRecurrence
        {
            Mode = mode,
            Rollover = dto.Rollover ?? true,
        };

        // Fenêtre : complète ou absente.
        var fenetre = new[] { dto.FenetreDebutMois, dto.FenetreDebutJour, dto.FenetreFinMois, dto.FenetreFinJour };
        if (fenetre.Any(v => v is not null))
        {
            if (fenetre.Any(v => v is null)
                || dto.FenetreDebutMois is < 1 or > 12 || dto.FenetreFinMois is < 1 or > 12
                || dto.FenetreDebutJour is < 1 or > 31 || dto.FenetreFinJour is < 1 or > 31)
            {
                return (spec, "Fenêtre saisonnière invalide (mois 1-12, jour 1-31, les 4 bornes requises).");
            }
            spec.FenetreDebutMois = dto.FenetreDebutMois;
            spec.FenetreDebutJour = dto.FenetreDebutJour;
            spec.FenetreFinMois = dto.FenetreFinMois;
            spec.FenetreFinJour = dto.FenetreFinJour;
        }

        if (mode == ModeRecurrence.Intervalle)
        {
            if (dto.IntervalleJours is null or < 1)
            {
                return (spec, "IntervalleJours requis (≥ 1) en mode intervalle.");
            }
            spec.IntervalleJours = dto.IntervalleJours;
            return (spec, null);
        }

        // Mode fixe
        if (Enum.TryParse<TypeFixe>(dto.FixeType, out var fixeType) == false)
        {
            return (spec, "FixeType requis en mode fixe (JoursSemaine, JourDuMois ou Annuelle).");
        }
        spec.FixeType = fixeType;
        switch (fixeType)
        {
            case TypeFixe.JoursSemaine:
                if (dto.JoursSemaine is null || dto.JoursSemaine.Length == 0
                    || dto.JoursSemaine.Any(j => j is < 0 or > 6))
                {
                    return (spec, "Au moins un jour de semaine (0=dimanche … 6=samedi) est requis.");
                }
                spec.JoursSemaineMasque = SpecRecurrence.MasqueDe(dto.JoursSemaine.Select(j => (DayOfWeek)j).ToArray());
                break;
            case TypeFixe.JourDuMois:
                if (dto.JourDuMois is null or < 1 or > 31)
                {
                    return (spec, "JourDuMois (1-31) requis.");
                }
                spec.JourDuMois = dto.JourDuMois;
                break;
            case TypeFixe.Annuelle:
                if (dto.MoisAnnuel is null or < 1 or > 12 || dto.JourAnnuel is null or < 1 or > 31)
                {
                    return (spec, "MoisAnnuel (1-12) et JourAnnuel (1-31) requis.");
                }
                spec.MoisAnnuel = dto.MoisAnnuel;
                spec.JourAnnuel = dto.JourAnnuel;
                break;
        }
        return (spec, null);
    }

    public static (StrategieAssignation Strategie, string? Erreur) ConvertirStrategie(string? strategie)
    {
        if (strategie is null)
        {
            return (StrategieAssignation.Fixe, null);
        }
        return Enum.TryParse<StrategieAssignation>(strategie, out var valeur)
            ? (valeur, null)
            : (StrategieAssignation.Fixe, $"Stratégie inconnue : {strategie}.");
    }

    public static TacheDto VersTacheDto(Tache tache)
    {
        var r = tache.Recurrence;
        var jours = r.JoursSemaineMasque is { } masque
            ? Enumerable.Range(0, 7).Where(j => (masque >> j & 1) == 1).ToArray()
            : null;
        return new TacheDto(
            tache.Id,
            tache.Titre,
            tache.Description,
            tache.AssigneAId,
            tache.ZoneId,
            tache.EquipementId,
            tache.Strategie.ToString(),
            new RecurrenceDto(
                r.Mode.ToString(),
                r.FixeType?.ToString(),
                jours,
                r.JourDuMois,
                r.MoisAnnuel,
                r.JourAnnuel,
                r.IntervalleJours,
                r.FenetreDebutMois,
                r.FenetreDebutJour,
                r.FenetreFinMois,
                r.FenetreFinJour,
                r.Rollover));
    }
}
