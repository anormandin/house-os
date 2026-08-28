using HouseOs.Api.Domaine;
using HouseOs.Api.Features.Auth;
using HouseOs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HouseOs.Api.Features.Taches;

public record ErreurValidation(string Champ, string Message);

public enum StatutCompletion { Introuvable, DejaCompletee, Ok }

public record ResultatCompletion(StatutCompletion Statut, Occurrence? Prochaine);

public enum StatutAnnulation { Introuvable, PasCompletee, PasLaDerniere, ProchaineDejaTraitee, JournalManquant, Ok }

public enum StatutPasse { Introuvable, DejaTraitee, TachePonctuelle, Ok }

public record ResultatPasse(StatutPasse Statut, Occurrence? Prochaine);

public enum StatutReport { Introuvable, DejaTraitee, DateInvalide, Ok }

public enum StatutNotes { Introuvable, PasCompletee, Ok }

/// <summary>
/// Logique de la tranche Taches partagée entre les endpoints REST et les outils MCP.
/// Les erreurs sont des valeurs (jamais des IResult) : chaque consommateur les traduit
/// dans son protocole (ValidationProblem côté REST, McpException côté MCP).
/// </summary>
public static class OperationsTaches
{
    /// <summary>Filtres acceptés par <see cref="ListerOccurrencesAsync"/> (REST et MCP).</summary>
    public static readonly string[] FiltresConnus = ["aujourdhui", "avenir", "en-attente", "completees", "faites"];

    /// <summary>
    /// Valide le filtre de liste : filtre inconnu refusé (sinon il retournerait tout,
    /// silencieusement), « faites » exige ses deux bornes, dans l'ordre.
    /// </summary>
    public static ErreurValidation? ValiderFiltre(string? filtre, DateTimeOffset? de, DateTimeOffset? a)
    {
        if (filtre is not null && FiltresConnus.Contains(filtre) == false)
        {
            return new ErreurValidation("filtre",
                $"Filtre inconnu : {filtre}. Valides : {string.Join(", ", FiltresConnus)} (ou aucun).");
        }
        if (filtre == "faites")
        {
            if (de is null || a is null)
            {
                return new ErreurValidation("de", "Le filtre « faites » exige les bornes de et a.");
            }
            // Bornes inversées ou fenêtre vide : un 200 [] silencieux cacherait l'erreur du client.
            if (de >= a)
            {
                return new ErreurValidation("de", "Le filtre « faites » exige de avant a (fenêtre [de, a)).");
            }
        }
        return null;
    }

    /// <summary>Les références optionnelles doivent exister — sinon la FK ferait un 500.</summary>
    private static async Task<ErreurValidation?> ValiderReferencesAsync(
        HouseOsDbContext db, Guid? zoneId, Guid? equipementId, Guid? assigneAId)
    {
        if (zoneId is { } z && await db.Zones.AnyAsync(x => x.Id == z) == false)
        {
            return new ErreurValidation("zoneId", "Cette pièce n'existe pas (ou plus).");
        }
        if (equipementId is { } e && await db.Equipements.AnyAsync(x => x.Id == e) == false)
        {
            return new ErreurValidation("equipementId", "Cet équipement n'existe pas (ou plus).");
        }
        if (assigneAId is { } u && await db.Utilisateurs.AnyAsync(x => x.Id == u) == false)
        {
            return new ErreurValidation("assigneAId", "Cet utilisateur n'existe pas.");
        }
        return null;
    }

    /// <summary>
    /// Remplace les documents liés d'une tâche (chargée avec sa collection Documents) :
    /// null = liens conservés, [] = tout délier, liste = remplacement complet.
    /// </summary>
    private static async Task<ErreurValidation?> AppliquerDocumentsAsync(
        HouseOsDbContext db, Tache tache, Guid[]? documentIds)
    {
        if (documentIds is null)
        {
            return null;
        }
        var ids = documentIds.Distinct().ToArray();
        var documents = await db.Documents.Where(d => ids.Contains(d.Id)).ToListAsync();
        if (documents.Count != ids.Length)
        {
            return new ErreurValidation("documentIds", "Un des documents n'existe pas (ou plus).");
        }
        tache.Documents.Clear();
        tache.Documents.AddRange(documents);
        return null;
    }

    private static ErreurValidation? ValiderTitre(string titre) =>
        string.IsNullOrWhiteSpace(titre)
            ? new ErreurValidation("titre", "Le titre est requis.")
            : titre.Trim().Length > 200
                ? new ErreurValidation("titre", "Le titre ne peut pas dépasser 200 caractères.")
                : null;

    /// <summary>Valide la requête et ajoute la tâche au contexte, sans SaveChanges.</summary>
    public static async Task<(Tache? Tache, ErreurValidation? Erreur)> PreparerTacheAsync(
        HouseOsDbContext db,
        CreerTacheRequete requete,
        Guid creeParId,
        DateTimeOffset maintenant,
        DateOnly aujourdhui)
    {
        if (ValiderTitre(requete.Titre ?? "") is { } erreurTitre)
        {
            return (null, erreurTitre);
        }

        var (spec, erreur) = ConvertirRecurrence(requete.Recurrence, aujourdhui);
        if (erreur is not null)
        {
            return (null, new ErreurValidation("recurrence", erreur));
        }
        var (strategie, erreurStrategie) = ConvertirStrategie(requete.Strategie);
        if (erreurStrategie is not null)
        {
            return (null, new ErreurValidation("strategie", erreurStrategie));
        }
        if (await ValiderReferencesAsync(db, requete.ZoneId, requete.EquipementId, requete.AssigneAId)
            is { } erreurReference)
        {
            return (null, erreurReference);
        }

        var titre = requete.Titre!.Trim();
        var description = string.IsNullOrWhiteSpace(requete.Description) ? null : requete.Description.Trim();

        var tache = spec.Mode == ModeRecurrence.Ponctuelle
            ? Tache.CreerPonctuelle(titre, description, requete.Echeance, requete.AssigneAId,
                creeParId, maintenant)
            : Tache.CreerRecurrente(titre, description, spec, strategie, requete.AssigneAId,
                requete.Echeance, creeParId, maintenant, aujourdhui);

        tache.ZoneId = requete.ZoneId;
        tache.EquipementId = requete.EquipementId;
        if (await AppliquerDocumentsAsync(db, tache, requete.DocumentIds) is { } erreurDocuments)
        {
            return (null, erreurDocuments);
        }

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
        if (ValiderTitre(requete.Titre ?? "") is { } erreurTitre)
        {
            return erreurTitre;
        }

        var (spec, erreur) = ConvertirRecurrence(requete.Recurrence, aujourdhui);
        if (erreur is not null)
        {
            return new ErreurValidation("recurrence", erreur);
        }
        var (strategie, erreurStrategie) = ConvertirStrategie(requete.Strategie);
        if (erreurStrategie is not null)
        {
            return new ErreurValidation("strategie", erreurStrategie);
        }
        if (await ValiderReferencesAsync(db, requete.ZoneId, requete.EquipementId, requete.AssigneAId)
            is { } erreurReference)
        {
            return erreurReference;
        }
        if (await AppliquerDocumentsAsync(db, tache, requete.DocumentIds) is { } erreurDocuments)
        {
            return erreurDocuments;
        }

        tache.Titre = requete.Titre!.Trim();
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
            // Stratégie Fixe : l'occurrence suit la tâche (y compris la désassignation).
            // Stratégies tournantes : l'assigné choisi par la stratégie reste en place
            // tant que la requête n'en impose pas un autre.
            enAttente.AssigneAId = strategie == StrategieAssignation.Fixe
                ? requete.AssigneAId
                : requete.AssigneAId ?? enAttente.AssigneAId;
        }
        else if (spec.Mode != ModeRecurrence.Ponctuelle)
        {
            // Convertir une tâche sans occurrence en attente (ex. ponctuelle complétée)
            // en récurrente doit matérialiser une occurrence — sinon la tâche devient
            // invisible de tous les filtres, définitivement.
            db.Occurrences.Add(new Occurrence
            {
                Id = Guid.NewGuid(),
                TacheId = tache.Id,
                Echeance = requete.Echeance ?? await RecalculerEcheance(db, tache, spec, aujourdhui),
                AssigneAId = requete.AssigneAId,
            });
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
        if (occurrence.Statut != StatutOccurrence.EnAttente)
        {
            return new ResultatCompletion(StatutCompletion.DejaCompletee, null);
        }

        var entree = occurrence.Completer(utilisateurId, maintenant, notes);
        db.Journal.Add(entree);

        Occurrence? prochaine = null;
        var tache = occurrence.Tache!;
        if (tache.Recurrence.Mode != ModeRecurrence.Ponctuelle)
        {
            var assigne = await ChoisirProchainAssigne(
                db, tache, utilisateurId, maintenant, completionEnVol: true);
            prochaine = tache.GenererProchaineOccurrence(
                DateOnly.FromDateTime(maintenant.LocalDateTime), occurrence.Echeance, assigne);
            if (prochaine is not null)
            {
                db.Occurrences.Add(prochaine);
            }
        }

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (EstViolationUnicite(ex))
        {
            // Course entre deux complétions : l'index unique « une seule occurrence en
            // attente par tâche » a refusé la seconde matérialisation — l'autre a gagné.
            db.ChangeTracker.Clear();
            return new ResultatCompletion(StatutCompletion.DejaCompletee, null);
        }
        return new ResultatCompletion(StatutCompletion.Ok, prochaine);
    }

    /// <summary>
    /// Défait la complétion la plus récente d'une tâche : supprime l'entrée de journal,
    /// remet l'occurrence en attente et supprime l'occurrence suivante matérialisée.
    /// Refusé si une complétion plus récente existe ou si la suivante a déjà été traitée.
    /// L'occurrence annulée reprend son ancienne échéance et redevient éligible au
    /// rollover — voulu. Fait SaveChanges.
    /// </summary>
    public static async Task<StatutAnnulation> AnnulerCompletionAsync(
        HouseOsDbContext db,
        Guid occurrenceId)
    {
        var occurrence = await db.Occurrences
            .Include(o => o.Tache)
            .SingleOrDefaultAsync(o => o.Id == occurrenceId);
        if (occurrence is null)
        {
            return StatutAnnulation.Introuvable;
        }
        if (occurrence.Statut != StatutOccurrence.Completee)
        {
            return StatutAnnulation.PasCompletee;
        }

        var tache = occurrence.Tache!;
        var entree = await db.Journal.SingleOrDefaultAsync(j => j.OccurrenceId == occurrence.Id);
        if (entree is null)
        {
            // Complétée mais sans entrée de journal (données importées, incident) :
            // un message « pas la plus récente » serait faux et sans issue.
            return StatutAnnulation.JournalManquant;
        }
        // Garde « dernière complétion » évaluée côté client : le journal d'une tâche
        // reste minuscule (deux personnes) et Sqlite (tests) ne traduit pas les
        // comparaisons de DateTimeOffset en SQL.
        var horodatages = await db.Journal
            .Where(j => j.TacheId == tache.Id)
            .Select(j => j.CompleteeLe)
            .ToListAsync();
        if (horodatages.Any(h => h > entree.CompleteeLe))
        {
            return StatutAnnulation.PasLaDerniere;
        }
        // Un « passer » postérieur a aussi fait avancer la chaîne (compléter → passer →
        // suivante) : annuler supprimerait la suivante du passage — pas celle de cette
        // complétion — et laisserait l'occurrence passée orpheline.
        var passeesLe = await db.Occurrences
            .Where(o => o.TacheId == tache.Id && o.Statut == StatutOccurrence.Passee)
            .Select(o => o.PasseeLe)
            .ToListAsync();
        if (passeesLe.Any(p => p > entree.CompleteeLe))
        {
            return StatutAnnulation.ProchaineDejaTraitee;
        }

        if (tache.Recurrence.Mode != ModeRecurrence.Ponctuelle)
        {
            var prochaine = await db.Occurrences.SingleOrDefaultAsync(o =>
                o.TacheId == tache.Id && o.Statut == StatutOccurrence.EnAttente);
            if (prochaine is null)
            {
                return StatutAnnulation.ProchaineDejaTraitee;
            }
            db.Occurrences.Remove(prochaine);
        }

        occurrence.AnnulerCompletion();
        db.Journal.Remove(entree);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (EstViolationUnicite(ex))
        {
            // Course avec une complétion/un passage concurrent : la suivante a avancé
            // pendant l'annulation et l'index unique « une seule occurrence en attente
            // par tâche » a refusé la réactivation — même contrat d'erreur que
            // Completer/Passer (409, jamais un 500 brut).
            db.ChangeTracker.Clear();
            return StatutAnnulation.ProchaineDejaTraitee;
        }
        return StatutAnnulation.Ok;
    }

    /// <summary>
    /// Saute une occurrence récurrente sans la marquer faite : statut Passee (trace
    /// datée), aucune entrée de journal, prochaine occurrence matérialisée avec les
    /// échéances d'une complétion aujourd'hui. Passer ne prend pas le tour (décision
    /// T8, 2026-08-28) : les stratégies tournantes conservent l'assigné de l'occurrence
    /// passée — utilisateurId (qui a cliqué) n'influence pas l'assignation. Fait
    /// SaveChanges.
    /// </summary>
    public static async Task<ResultatPasse> PasserAsync(
        HouseOsDbContext db,
        Guid occurrenceId,
        Guid utilisateurId,
        DateTimeOffset maintenant)
    {
        var occurrence = await db.Occurrences
            .Include(o => o.Tache)
            .SingleOrDefaultAsync(o => o.Id == occurrenceId);
        if (occurrence is null)
        {
            return new ResultatPasse(StatutPasse.Introuvable, null);
        }
        if (occurrence.Statut != StatutOccurrence.EnAttente)
        {
            return new ResultatPasse(StatutPasse.DejaTraitee, null);
        }

        var tache = occurrence.Tache!;
        if (tache.Recurrence.Mode == ModeRecurrence.Ponctuelle)
        {
            return new ResultatPasse(StatutPasse.TachePonctuelle, null);
        }

        occurrence.Passer(maintenant);

        // Le tour n'a pas été pris : les stratégies tournantes gardent l'assigné de
        // l'occurrence passée (décision T8) ; la stratégie fixe suit la tâche.
        var assigne = tache.Strategie == StrategieAssignation.Fixe
            ? tache.AssigneAId
            : occurrence.AssigneAId;
        var prochaine = tache.GenererProchaineOccurrence(
            DateOnly.FromDateTime(maintenant.LocalDateTime), occurrence.Echeance, assigne);
        if (prochaine is not null)
        {
            db.Occurrences.Add(prochaine);
        }

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (EstViolationUnicite(ex))
        {
            // Même course que la complétion : l'index unique a tranché.
            db.ChangeTracker.Clear();
            return new ResultatPasse(StatutPasse.DejaTraitee, null);
        }
        return new ResultatPasse(StatutPasse.Ok, prochaine);
    }

    /// <summary>
    /// Glisse l'échéance de l'occurrence en attente sans toucher la définition de la
    /// tâche. Dates passées refusées. Fait SaveChanges.
    /// </summary>
    public static async Task<StatutReport> ReporterAsync(
        HouseOsDbContext db,
        Guid occurrenceId,
        DateOnly nouvelleEcheance,
        DateOnly aujourdhui)
    {
        var occurrence = await db.Occurrences.SingleOrDefaultAsync(o => o.Id == occurrenceId);
        if (occurrence is null)
        {
            return StatutReport.Introuvable;
        }
        if (occurrence.Statut != StatutOccurrence.EnAttente)
        {
            return StatutReport.DejaTraitee;
        }
        if (nouvelleEcheance < aujourdhui)
        {
            return StatutReport.DateInvalide;
        }

        occurrence.Reporter(nouvelleEcheance);
        await db.SaveChangesAsync();
        return StatutReport.Ok;
    }

    /// <summary>
    /// Met à jour les notes de l'entrée de journal d'une occurrence complétée
    /// (ajout post-hoc depuis la rangée verte). Fait SaveChanges.
    /// </summary>
    public static async Task<StatutNotes> AjouterNotesAsync(
        HouseOsDbContext db,
        Guid occurrenceId,
        string? notes)
    {
        var entree = await db.Journal.SingleOrDefaultAsync(j => j.OccurrenceId == occurrenceId);
        if (entree is null)
        {
            var existe = await db.Occurrences.AnyAsync(o => o.Id == occurrenceId);
            return existe ? StatutNotes.PasCompletee : StatutNotes.Introuvable;
        }

        entree.Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        await db.SaveChangesAsync();
        return StatutNotes.Ok;
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

        // Les listes de complétions se lisent des plus récentes ; les listes d'attente
        // par échéance. Le tri précède le plafond : sans ça, au-delà de 200 complétions
        // les récentes disparaîtraient.
        var ordonnee = filtre is "completees" or "faites"
            ? requete.OrderByDescending(o => o.CompleteeLe).ThenBy(o => o.Id)
            : requete
                .OrderBy(o => o.Echeance == null)
                .ThenBy(o => o.Echeance)
                .ThenByDescending(o => o.CompleteeLe);

        return await ordonnee
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
                db.Journal.Where(j => j.OccurrenceId == o.Id).Select(j => j.Notes).FirstOrDefault(),
                o.Tache.ZoneId,
                o.Tache.EquipementId,
                o.Tache.Recurrence.Mode.ToString()))
            .ToListAsync();
    }

    /// <summary>
    /// Instants de complétion du journal dans [de, a) — bilan du ménage entier, sans égard
    /// à qui a cliqué. Le client fournit les bornes et agrège par semaine locale (le serveur
    /// ne connaît pas le fuseau du client).
    /// </summary>
    public static Task<List<DateTimeOffset>> BilanCompletionsAsync(
        HouseOsDbContext db,
        DateTimeOffset de,
        DateTimeOffset a) =>
        db.Journal.AsNoTracking()
            .Where(j => j.CompleteeLe >= de && j.CompleteeLe < a)
            .Select(j => j.CompleteeLe)
            .ToListAsync();

    /// <summary>
    /// Vraie violation d'unicité (l'index « une seule occurrence en attente par
    /// tâche ») — toute autre erreur de sauvegarde doit remonter au lieu d'être
    /// maquillée en conflit de course.
    /// </summary>
    private static bool EstViolationUnicite(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    /// <summary>Applique la stratégie d'assignation pour la prochaine occurrence.</summary>
    internal static async Task<Guid?> ChoisirProchainAssigne(
        HouseOsDbContext db,
        Tache tache,
        Guid dernierCompleteurId,
        DateTimeOffset maintenant,
        bool completionEnVol = false)
    {
        if (tache.Strategie == StrategieAssignation.Fixe)
        {
            return tache.AssigneAId;
        }

        // Ordre stable requis : les stratégies prennent « le premier » en cas d'égalité.
        var utilisateurs = await db.Utilisateurs
            .OrderBy(u => u.NomUtilisateur)
            .Select(u => u.Id)
            .ToListAsync();
        var depuis = maintenant.AddDays(-90);
        var completions = await db.Journal
            .Where(j => j.TacheId == tache.Id && j.CompleteeLe >= depuis)
            .GroupBy(j => j.UtilisateurId)
            .Select(g => new { UtilisateurId = g.Key, Nombre = g.Count() })
            .ToDictionaryAsync(x => x.UtilisateurId, x => x.Nombre);
        if (completionEnVol)
        {
            // L'entrée de journal de la complétion en cours n'est pas encore sauvegardée,
            // donc invisible de la requête : sans ce complément, MoinsLAFait réassignerait
            // le compléteur une fois sur deux.
            completions[dernierCompleteurId] = completions.GetValueOrDefault(dernierCompleteurId) + 1;
        }

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

    public static (SpecRecurrence Spec, string? Erreur) ConvertirRecurrence(
        RecurrenceDto? dto, DateOnly aujourdhui)
    {
        if (dto is null || dto.Mode == nameof(ModeRecurrence.Ponctuelle))
        {
            if (dto?.Rollover == true)
            {
                return (SpecRecurrence.Ponctuelle(), "Le rollover ne s'applique qu'au mode fixe.");
            }
            return (SpecRecurrence.Ponctuelle(), null);
        }
        if (Enum.TryParse<ModeRecurrence>(dto.Mode, out var mode) == false)
        {
            return (SpecRecurrence.Ponctuelle(), $"Mode inconnu : {dto.Mode}.");
        }

        var spec = new SpecRecurrence
        {
            Mode = mode,
            // Le flag est réservé au mode fixe : ailleurs il est inopérant (accepté
            // silencieusement, il laisserait croire qu'une intervalle glisse) — refusé
            // plus bas s'il est demandé, et jamais exposé (VersRecurrenceDto).
            Rollover = mode == ModeRecurrence.Fixe && (dto.Rollover ?? true),
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
            if (dto.Rollover == true)
            {
                return (spec, "Le rollover ne s'applique qu'au mode fixe.");
            }
            if (dto.IntervalleJours is null or < 1)
            {
                return (spec, "IntervalleJours requis (≥ 1) en mode intervalle.");
            }
            spec.IntervalleJours = dto.IntervalleJours;
            return (spec, ValiderCoherence(spec, aujourdhui));
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
        return (spec, ValiderCoherence(spec, aujourdhui));
    }

    /// <summary>
    /// Validation croisée spec × fenêtre : chaque champ peut être valide isolément alors
    /// que la combinaison ne planifie jamais rien — sans cette garde, le moteur boucle
    /// 1500 jours puis lève, et la création répond 500 au lieu de 400.
    /// </summary>
    private static string? ValiderCoherence(SpecRecurrence spec, DateOnly aujourdhui)
    {
        if (spec.AFenetre)
        {
            // Bornes = vraies dates (année bissextile de référence : le 29 février passe).
            if (spec.FenetreDebutJour > DateTime.DaysInMonth(2024, spec.FenetreDebutMois!.Value)
                || spec.FenetreFinJour > DateTime.DaysInMonth(2024, spec.FenetreFinMois!.Value))
            {
                return "Fenêtre saisonnière invalide : ce jour n'existe pas dans ce mois.";
            }
        }
        if (spec.Mode == ModeRecurrence.Fixe)
        {
            try
            {
                // Sonde en deux temps, ancrée sur aujourd'hui (une date en dur vieillit
                // mal) : la première date planifiée doit exister, et une complétion ce
                // jour-là doit pouvoir calculer la suivante. « Lundi + fenêtre
                // 14/09–14/09 » trouve un lundi 14 septembre, mais le suivant est dans
                // plus de dix ans : accepté, ce serait un 500 à la complétion — et la
                // complétion perdue.
                var premiere = MoteurRecurrence.ProchainePlanifiee(spec, aujourdhui);
                MoteurRecurrence.ProchainePlanifiee(spec, premiere.AddDays(1));
            }
            catch (InvalidOperationException)
            {
                return "Cette récurrence ne tombe jamais, ou trop rarement, dans la fenêtre saisonnière.";
            }
        }
        return null;
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

    public static TacheDto VersTacheDto(Tache tache) =>
        new(
            tache.Id,
            tache.Titre,
            tache.Description,
            tache.Occurrences.FirstOrDefault(o => o.Statut == StatutOccurrence.EnAttente)?.Echeance,
            tache.AssigneAId,
            tache.ZoneId,
            tache.EquipementId,
            tache.Strategie.ToString(),
            VersRecurrenceDto(tache.Recurrence),
            tache.Documents.Select(d => d.Id).ToArray());

    private static RecurrenceDto VersRecurrenceDto(SpecRecurrence r)
    {
        var jours = r.JoursSemaineMasque is { } masque
            ? Enumerable.Range(0, 7).Where(j => (masque >> j & 1) == 1).ToArray()
            : null;
        return new RecurrenceDto(
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
            // Le flag n'existe que pour le mode fixe : l'exposer ailleurs laisserait
            // croire qu'il y agit.
            r.Mode == ModeRecurrence.Fixe ? r.Rollover : null);
    }

    /// <summary>
    /// Les définitions de tâches pour la console de gestion : récurrence, stratégie,
    /// échéance et assigné de l'occurrence en attente, nombre de documents liés.
    /// Une ponctuelle sans occurrence en attente est retournée `completee: true`.
    /// </summary>
    public static async Task<List<TacheResumeDto>> ListerTachesAsync(HouseOsDbContext db)
    {
        var taches = await db.Taches.AsNoTracking()
            .Include(t => t.Occurrences.Where(o => o.Statut == StatutOccurrence.EnAttente))
            .ThenInclude(o => o.AssigneA)
            .Include(t => t.Documents)
            .ToListAsync();

        return taches
            .Select(t =>
            {
                var enAttente = t.Occurrences.FirstOrDefault(o => o.Statut == StatutOccurrence.EnAttente);
                return new TacheResumeDto(
                    t.Id,
                    t.Titre,
                    t.Description,
                    enAttente?.Id,
                    enAttente?.Echeance,
                    enAttente?.AssigneA is { } assigne
                        ? new UtilisateurDto(assigne.Id, assigne.NomUtilisateur, assigne.NomAffichage)
                        : null,
                    t.ZoneId,
                    t.EquipementId,
                    t.Strategie.ToString(),
                    VersRecurrenceDto(t.Recurrence),
                    t.Documents.Count,
                    Completee: t.Recurrence.Mode == ModeRecurrence.Ponctuelle && enAttente is null);
            })
            .OrderBy(r => r.Echeance is null)
            .ThenBy(r => r.Echeance)
            .ThenBy(r => r.Titre)
            .ToList();
    }
}
