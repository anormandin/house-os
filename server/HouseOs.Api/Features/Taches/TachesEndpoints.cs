using System.Security.Claims;
using HouseOs.Api.Domaine;
using HouseOs.Api.Features.Auth;
using HouseOs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HouseOs.Api.Features.Taches;

public record RecurrenceDto(
    string Mode,
    string? FixeType,
    int[]? JoursSemaine,
    int? JourDuMois,
    int? MoisAnnuel,
    int? JourAnnuel,
    int? IntervalleJours,
    int? FenetreDebutMois,
    int? FenetreDebutJour,
    int? FenetreFinMois,
    int? FenetreFinJour,
    bool? Rollover);

public record CreerTacheRequete(
    string Titre,
    string? Description,
    DateOnly? Echeance,
    Guid? AssigneAId,
    Guid? ZoneId,
    Guid? EquipementId,
    string? Strategie,
    RecurrenceDto? Recurrence);

public record ModifierTacheRequete(
    string Titre,
    string? Description,
    DateOnly? Echeance,
    Guid? AssigneAId,
    Guid? ZoneId,
    Guid? EquipementId,
    string? Strategie,
    RecurrenceDto? Recurrence);

public record CompleterRequete(string? Notes);

public record OccurrenceDto(
    Guid Id,
    Guid TacheId,
    string Titre,
    string? Description,
    DateOnly? Echeance,
    string Statut,
    UtilisateurDto? AssigneA,
    UtilisateurDto? CompleteePar,
    DateTimeOffset? CompleteeLe,
    Guid? ZoneId,
    Guid? EquipementId,
    string ModeRecurrence);

public record TacheDto(
    Guid Id,
    string Titre,
    string? Description,
    Guid? AssigneAId,
    Guid? ZoneId,
    Guid? EquipementId,
    string Strategie,
    RecurrenceDto Recurrence);

public static class TachesEndpoints
{
    public static IEndpointRouteBuilder MapTaches(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/utilisateurs", async (HouseOsDbContext db) =>
            await db.Utilisateurs
                .OrderBy(u => u.NomAffichage)
                .Select(u => new UtilisateurDto(u.Id, u.NomUtilisateur, u.NomAffichage))
                .ToListAsync());

        app.MapPost("/api/taches", async (
            CreerTacheRequete requete,
            ClaimsPrincipal principal,
            HouseOsDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(requete.Titre))
            {
                return Erreur("titre", "Le titre est requis.");
            }

            var (spec, erreur) = ConvertirRecurrence(requete.Recurrence);
            if (erreur is not null)
            {
                return erreur;
            }
            var (strategie, erreurStrategie) = ConvertirStrategie(requete.Strategie);
            if (erreurStrategie is not null)
            {
                return erreurStrategie;
            }

            var maintenant = DateTimeOffset.UtcNow;
            var aujourdhui = DateOnly.FromDateTime(DateTime.Now);
            var titre = requete.Titre.Trim();
            var description = string.IsNullOrWhiteSpace(requete.Description) ? null : requete.Description.Trim();

            var tache = spec.Mode == ModeRecurrence.Ponctuelle
                ? Tache.CreerPonctuelle(titre, description, requete.Echeance, requete.AssigneAId,
                    principal.IdUtilisateur(), maintenant)
                : Tache.CreerRecurrente(titre, description, spec, strategie, requete.AssigneAId,
                    requete.Echeance, principal.IdUtilisateur(), maintenant, aujourdhui);

            tache.ZoneId = requete.ZoneId;
            tache.EquipementId = requete.EquipementId;

            db.Taches.Add(tache);
            await db.SaveChangesAsync();
            return Results.Created($"/api/taches/{tache.Id}", new { tache.Id });
        });

        app.MapGet("/api/taches/{id:guid}", async (Guid id, HouseOsDbContext db) =>
        {
            var tache = await db.Taches.AsNoTracking().SingleOrDefaultAsync(t => t.Id == id);
            return tache is null ? Results.NotFound() : Results.Ok(VersTacheDto(tache));
        });

        app.MapPut("/api/taches/{id:guid}", async (
            Guid id,
            ModifierTacheRequete requete,
            HouseOsDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(requete.Titre))
            {
                return Erreur("titre", "Le titre est requis.");
            }

            var tache = await db.Taches
                .Include(t => t.Occurrences.Where(o => o.Statut == StatutOccurrence.EnAttente))
                .SingleOrDefaultAsync(t => t.Id == id);
            if (tache is null)
            {
                return Results.NotFound();
            }

            var (spec, erreur) = ConvertirRecurrence(requete.Recurrence);
            if (erreur is not null)
            {
                return erreur;
            }
            var (strategie, erreurStrategie) = ConvertirStrategie(requete.Strategie);
            if (erreurStrategie is not null)
            {
                return erreurStrategie;
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
                var aujourdhui = DateOnly.FromDateTime(DateTime.Now);
                enAttente.Echeance = spec.Mode switch
                {
                    ModeRecurrence.Ponctuelle => requete.Echeance,
                    _ => requete.Echeance ?? await RecalculerEcheance(db, tache, spec, aujourdhui),
                };
                enAttente.AssigneAId = requete.AssigneAId ?? enAttente.AssigneAId;
            }

            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        app.MapGet("/api/occurrences", async (
            string? filtre,
            DateOnly? date,
            DateTimeOffset? de,
            DateTimeOffset? a,
            HouseOsDbContext db) =>
        {
            var aujourdhui = date ?? DateOnly.FromDateTime(DateTime.Now);
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

            var occurrences = await requete
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

            return Results.Ok(occurrences);
        });

        app.MapPost("/api/occurrences/{id:guid}/completer", async (
            Guid id,
            CompleterRequete? requete,
            ClaimsPrincipal principal,
            HouseOsDbContext db) =>
        {
            var occurrence = await db.Occurrences
                .Include(o => o.Tache)
                .SingleOrDefaultAsync(o => o.Id == id);
            if (occurrence is null)
            {
                return Results.NotFound();
            }
            if (occurrence.Statut == StatutOccurrence.Completee)
            {
                return Results.Conflict(new { message = "Occurrence déjà complétée." });
            }

            var maintenant = DateTimeOffset.UtcNow;
            var utilisateurId = principal.IdUtilisateur();
            var entree = occurrence.Completer(utilisateurId, maintenant, requete?.Notes);
            db.Journal.Add(entree);

            var tache = occurrence.Tache!;
            if (tache.Recurrence.Mode != ModeRecurrence.Ponctuelle)
            {
                var assigne = await ChoisirProchainAssigne(db, tache, utilisateurId, maintenant);
                var prochaine = tache.GenererProchaineOccurrence(
                    DateOnly.FromDateTime(maintenant.LocalDateTime), occurrence.Echeance, assigne);
                if (prochaine is not null)
                {
                    db.Occurrences.Add(prochaine);
                }
            }

            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        app.MapDelete("/api/taches/{id:guid}", async (Guid id, HouseOsDbContext db) =>
        {
            var tache = await db.Taches.FindAsync(id);
            if (tache is null)
            {
                return Results.NotFound();
            }

            db.Taches.Remove(tache);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        return app;
    }

    /// <summary>Applique la stratégie d'assignation pour la prochaine occurrence.</summary>
    private static async Task<Guid?> ChoisirProchainAssigne(
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
    private static async Task<DateOnly?> RecalculerEcheance(
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

    private static IResult Erreur(string champ, string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { [champ] = [message] });

    private static (SpecRecurrence Spec, IResult? Erreur) ConvertirRecurrence(RecurrenceDto? dto)
    {
        if (dto is null || dto.Mode == nameof(ModeRecurrence.Ponctuelle))
        {
            return (SpecRecurrence.Ponctuelle(), null);
        }
        if (Enum.TryParse<ModeRecurrence>(dto.Mode, out var mode) == false)
        {
            return (SpecRecurrence.Ponctuelle(), Erreur("recurrence", $"Mode inconnu : {dto.Mode}."));
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
                return (spec, Erreur("recurrence", "Fenêtre saisonnière invalide (mois 1-12, jour 1-31, les 4 bornes requises)."));
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
                return (spec, Erreur("recurrence", "IntervalleJours requis (≥ 1) en mode intervalle."));
            }
            spec.IntervalleJours = dto.IntervalleJours;
            return (spec, null);
        }

        // Mode fixe
        if (Enum.TryParse<TypeFixe>(dto.FixeType, out var fixeType) == false)
        {
            return (spec, Erreur("recurrence", "FixeType requis en mode fixe (JoursSemaine, JourDuMois ou Annuelle)."));
        }
        spec.FixeType = fixeType;
        switch (fixeType)
        {
            case TypeFixe.JoursSemaine:
                if (dto.JoursSemaine is null || dto.JoursSemaine.Length == 0
                    || dto.JoursSemaine.Any(j => j is < 0 or > 6))
                {
                    return (spec, Erreur("recurrence", "Au moins un jour de semaine (0=dimanche … 6=samedi) est requis."));
                }
                spec.JoursSemaineMasque = SpecRecurrence.MasqueDe(dto.JoursSemaine.Select(j => (DayOfWeek)j).ToArray());
                break;
            case TypeFixe.JourDuMois:
                if (dto.JourDuMois is null or < 1 or > 31)
                {
                    return (spec, Erreur("recurrence", "JourDuMois (1-31) requis."));
                }
                spec.JourDuMois = dto.JourDuMois;
                break;
            case TypeFixe.Annuelle:
                if (dto.MoisAnnuel is null or < 1 or > 12 || dto.JourAnnuel is null or < 1 or > 31)
                {
                    return (spec, Erreur("recurrence", "MoisAnnuel (1-12) et JourAnnuel (1-31) requis."));
                }
                spec.MoisAnnuel = dto.MoisAnnuel;
                spec.JourAnnuel = dto.JourAnnuel;
                break;
        }
        return (spec, null);
    }

    private static (StrategieAssignation Strategie, IResult? Erreur) ConvertirStrategie(string? strategie)
    {
        if (strategie is null)
        {
            return (StrategieAssignation.Fixe, null);
        }
        return Enum.TryParse<StrategieAssignation>(strategie, out var valeur)
            ? (valeur, null)
            : (StrategieAssignation.Fixe, Erreur("strategie", $"Stratégie inconnue : {strategie}."));
    }

    private static TacheDto VersTacheDto(Tache tache)
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
