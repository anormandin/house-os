using System.ComponentModel;
using HouseOs.Api.Domaine;
using HouseOs.Api.Features.Auth;
using HouseOs.Api.Features.Taches;
using HouseOs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace HouseOs.Api.Features.Mcp;

/// <summary>Une tâche à créer via MCP : dates en chaînes YYYY-MM-DD, assigné par nom d'utilisateur.</summary>
public record TacheAPlanifier(
    [property: Description("Titre de la tâche (requis).")] string Titre,
    [property: Description("Détails optionnels.")] string? Description,
    [property: Description("Échéance au format YYYY-MM-DD (ex. 2026-10-06), ou null si sans date.")] string? Echeance,
    [property: Description("Nom d'utilisateur de la personne assignée ('alain' ou 'ariane'), ou null si non assignée.")] string? AssigneA,
    [property: Description("Id d'une zone existante (via lister_zones) — ne jamais inventer.")] Guid? ZoneId,
    [property: Description("Id d'un équipement existant (via lister_equipements) — ne jamais inventer.")] Guid? EquipementId,
    [property: Description("Stratégie d'assignation pour une tâche récurrente : Fixe (défaut), Alternance ou MoinsLAFait.")] string? Strategie,
    [property: Description("Récurrence ; omise = tâche ponctuelle. mode: Ponctuelle|Fixe|Intervalle ; en mode Fixe, fixeType: JoursSemaine (+ joursSemaine 0=dimanche…6=samedi) | JourDuMois (+ jourDuMois 1-31) | Annuelle (+ moisAnnuel 1-12, jourAnnuel 1-31) ; en mode Intervalle, intervalleJours ≥ 1 (depuis la dernière complétion). Fenêtre saisonnière optionnelle : les 4 bornes fenetreDebutMois/fenetreDebutJour/fenetreFinMois/fenetreFinJour ensemble.")] RecurrenceDto? Recurrence);

/// <summary>Une semaine du bilan : lundi de la semaine (YYYY-MM-DD) et total complété.</summary>
public record BilanSemaineDto(
    [property: Description("Lundi de la semaine, format YYYY-MM-DD.")] string SemaineDu,
    [property: Description("Nombre de complétions du foyer cette semaine-là.")] int Nombre);

[McpServerToolType]
public static class OutilsTaches
{
    [McpServerTool(Name = "lister_utilisateurs")]
    [Description("Liste les deux membres du foyer (id, nomUtilisateur, nomAffichage). " +
        "Les paramètres agirComme et assigneA des autres outils prennent le nomUtilisateur.")]
    public static async Task<List<UtilisateurDto>> ListerUtilisateurs(HouseOsDbContext db) =>
        await db.Utilisateurs
            .OrderBy(u => u.NomAffichage)
            .Select(u => new UtilisateurDto(u.Id, u.NomUtilisateur, u.NomAffichage))
            .ToListAsync();

    [McpServerTool(Name = "creer_taches")]
    [Description("Crée un lot de tâches en une seule transaction (tout-ou-rien : si une tâche est " +
        "invalide, rien n'est créé et les erreurs sont listées par index). C'est l'outil pour pousser " +
        "un plan complet (ex. déménagement). Toujours faire confirmer le plan par l'humain avant " +
        "d'appeler. Dates YYYY-MM-DD ; récurrence omise = tâche ponctuelle.")]
    public static async Task<object> CreerTaches(
        HouseOsDbContext db,
        [Description("Au nom de qui les tâches sont créées : 'alain' ou 'ariane'. Demander si ambigu.")]
        string agirComme,
        [Description("Les tâches à créer.")] TacheAPlanifier[] taches)
    {
        if (taches is null || taches.Length == 0)
        {
            throw new McpException("Au moins une tâche est requise.");
        }

        var createur = await AgirComme.ResoudreAsync(db, agirComme);
        var utilisateurs = await db.Utilisateurs.ToListAsync();
        var zonesValides = await db.Zones.Select(z => z.Id).ToListAsync();
        var equipementsValides = await db.Equipements.Select(e => e.Id).ToListAsync();

        var maintenant = DateTimeOffset.UtcNow;
        var aujourdhui = DateOnly.FromDateTime(DateTime.Now);
        var erreurs = new List<string>();
        var creees = new List<(int Index, Tache Tache)>();

        foreach (var (item, index) in taches.Select((t, i) => (t, i)))
        {
            DateOnly? echeance;
            try
            {
                echeance = Conversions.ParserDate(item.Echeance, "echeance");
            }
            catch (McpException e)
            {
                erreurs.Add($"[{index}] {e.Message}");
                continue;
            }

            Guid? assigneAId = null;
            if (string.IsNullOrWhiteSpace(item.AssigneA) == false)
            {
                assigneAId = AgirComme.Resoudre(item.AssigneA, utilisateurs.Select(u => (u.Id, u.NomUtilisateur)));
                if (assigneAId is null)
                {
                    erreurs.Add($"[{index}] assigneA inconnu : '{item.AssigneA}' ({AgirComme.ValeursValides(utilisateurs)}).");
                    continue;
                }
            }
            if (item.ZoneId is { } zoneId && zonesValides.Contains(zoneId) == false)
            {
                erreurs.Add($"[{index}] zoneId inconnu : {zoneId} (voir lister_zones).");
                continue;
            }
            if (item.EquipementId is { } equipementId && equipementsValides.Contains(equipementId) == false)
            {
                erreurs.Add($"[{index}] equipementId inconnu : {equipementId} (voir lister_equipements).");
                continue;
            }

            var requete = new CreerTacheRequete(
                item.Titre, item.Description, echeance, assigneAId,
                item.ZoneId, item.EquipementId, item.Strategie, item.Recurrence);
            var (tache, erreur) = OperationsTaches.PreparerTache(db, requete, createur.Id, maintenant, aujourdhui);
            if (erreur is not null)
            {
                erreurs.Add($"[{index}] {erreur.Champ} : {erreur.Message}");
                continue;
            }
            creees.Add((index, tache!));
        }

        if (erreurs.Count > 0)
        {
            throw new McpException(
                "Aucune tâche créée (lot tout-ou-rien). Erreurs :\n" + string.Join("\n", erreurs));
        }

        await db.SaveChangesAsync();
        return new
        {
            crees = creees.Select(c => new
            {
                index = c.Index,
                id = c.Tache.Id,
                titre = c.Tache.Titre,
                echeance = c.Tache.Occurrences.FirstOrDefault()?.Echeance,
            }).ToList(),
        };
    }

    [McpServerTool(Name = "gerer_tache")]
    [Description("Obtenir, modifier ou supprimer une définition de tâche (pas ses occurrences — " +
        "voir lister_occurrences). Modifier remplace la définition complète et réaligne " +
        "l'occurrence en attente ; supprimer efface aussi les occurrences (le journal survit).")]
    public static async Task<object> GererTache(
        HouseOsDbContext db,
        [Description("obtenir, modifier ou supprimer.")] string action,
        [Description("Id de la tâche.")] Guid id,
        [Description("Nouvelle définition complète (requise pour modifier).")] TacheAPlanifier? tache = null)
    {
        switch (action)
        {
            case "obtenir":
            {
                var existante = await db.Taches.AsNoTracking().SingleOrDefaultAsync(t => t.Id == id);
                return existante is null
                    ? throw new McpException($"Tâche introuvable : {id}.")
                    : OperationsTaches.VersTacheDto(existante);
            }
            case "modifier":
            {
                if (tache is null)
                {
                    throw new McpException("Le paramètre tache est requis pour modifier.");
                }
                var existante = await db.Taches
                    .Include(t => t.Occurrences.Where(o => o.Statut == StatutOccurrence.EnAttente))
                    .SingleOrDefaultAsync(t => t.Id == id);
                if (existante is null)
                {
                    throw new McpException($"Tâche introuvable : {id}.");
                }

                var echeance = Conversions.ParserDate(tache.Echeance, "echeance");
                Guid? assigneAId = null;
                if (string.IsNullOrWhiteSpace(tache.AssigneA) == false)
                {
                    var utilisateurs = await db.Utilisateurs.ToListAsync();
                    assigneAId = AgirComme.Resoudre(tache.AssigneA, utilisateurs.Select(u => (u.Id, u.NomUtilisateur)));
                    if (assigneAId is null)
                    {
                        throw new McpException($"assigneA inconnu : '{tache.AssigneA}' ({AgirComme.ValeursValides(utilisateurs)}).");
                    }
                }

                var requete = new ModifierTacheRequete(
                    tache.Titre, tache.Description, echeance, assigneAId,
                    tache.ZoneId, tache.EquipementId, tache.Strategie, tache.Recurrence);
                var erreur = await OperationsTaches.ModifierTacheAsync(
                    db, existante, requete, DateOnly.FromDateTime(DateTime.Now));
                if (erreur is not null)
                {
                    throw new McpException($"{erreur.Champ} : {erreur.Message}");
                }
                await db.SaveChangesAsync();
                return new { modifie = true, id };
            }
            case "supprimer":
            {
                var existante = await db.Taches.FindAsync(id);
                if (existante is null)
                {
                    throw new McpException($"Tâche introuvable : {id}.");
                }
                db.Taches.Remove(existante);
                await db.SaveChangesAsync();
                return new { supprime = true, id };
            }
            default:
                throw new McpException($"Action inconnue : '{action}' (obtenir, modifier ou supprimer).");
        }
    }

    [McpServerTool(Name = "lister_occurrences")]
    [Description("Liste les occurrences (instances planifiées des tâches), 200 max, triées par " +
        "échéance. Filtres : 'aujourdhui' (en attente, échues à la date de référence ou avant), " +
        "'avenir' (en attente, futures ou sans échéance), 'en-attente' (toutes les non-faites), " +
        "'completees' (les faites, plus récentes d'abord). Sans filtre : tout, y compris les " +
        "occurrences au statut Passee (sautées sans être faites).")]
    public static async Task<List<OccurrenceDto>> ListerOccurrences(
        HouseOsDbContext db,
        [Description("aujourdhui, avenir, en-attente ou completees.")] string? filtre = null,
        [Description("Date de référence YYYY-MM-DD (défaut : aujourd'hui, heure du serveur).")] string? date = null)
    {
        var aujourdhui = Conversions.ParserDate(date, "date") ?? DateOnly.FromDateTime(DateTime.Now);
        return await OperationsTaches.ListerOccurrencesAsync(db, filtre, aujourdhui, null, null);
    }

    [McpServerTool(Name = "bilan_taches")]
    [Description("Bilan du ménage : nombre de tâches complétées par semaine (lundi à dimanche, " +
        "heure du serveur), semaine courante en premier. Total du foyer entier — l'attribution " +
        "individuelle du journal est indicative (qui clique n'est pas toujours qui a fait).")]
    public static async Task<List<BilanSemaineDto>> BilanTaches(
        HouseOsDbContext db,
        [Description("Nombre de semaines à couvrir, semaine courante incluse (défaut 8, max 52).")] int? semaines = null)
    {
        var nb = Math.Clamp(semaines ?? 8, 1, 52);
        var aujourdhui = DateOnly.FromDateTime(DateTime.Now);
        var lundiCourant = aujourdhui.AddDays(-(((int)aujourdhui.DayOfWeek + 6) % 7));
        var debut = lundiCourant.AddDays(-7 * (nb - 1));
        var de = new DateTimeOffset(debut.ToDateTime(TimeOnly.MinValue)).ToUniversalTime();
        var a = new DateTimeOffset(lundiCourant.AddDays(7).ToDateTime(TimeOnly.MinValue)).ToUniversalTime();

        var instants = await OperationsTaches.BilanCompletionsAsync(db, de, a);
        var comptes = new int[nb];
        foreach (var instant in instants)
        {
            var indice = (DateOnly.FromDateTime(instant.ToLocalTime().Date).DayNumber - debut.DayNumber) / 7;
            if (indice >= 0 && indice < nb)
            {
                comptes[indice]++;
            }
        }

        return Enumerable.Range(0, nb)
            .Select(i => new BilanSemaineDto(
                debut.AddDays(7 * i).ToString("yyyy-MM-dd"), comptes[i]))
            .Reverse()
            .ToList();
    }

    [McpServerTool(Name = "completer_occurrence")]
    [Description("Marque une occurrence comme faite, au nom de la personne agirComme — cette " +
        "attribution alimente les statistiques d'équité (stratégie MoinsLAFait) : ne jamais la " +
        "deviner. Si la tâche est récurrente, la prochaine occurrence est créée et retournée.")]
    public static async Task<object> CompleterOccurrence(
        HouseOsDbContext db,
        [Description("Id de l'occurrence (via lister_occurrences).")] Guid occurrenceId,
        [Description("Qui l'a faite : 'alain' ou 'ariane'. Demander si ambigu.")] string agirComme,
        [Description("Notes optionnelles (coût, remarques…).")] string? notes = null)
    {
        var utilisateur = await AgirComme.ResoudreAsync(db, agirComme);
        var resultat = await OperationsTaches.CompleterAsync(
            db, occurrenceId, utilisateur.Id, notes, DateTimeOffset.UtcNow);
        switch (resultat.Statut)
        {
            case StatutCompletion.Introuvable:
                throw new McpException($"Occurrence introuvable : {occurrenceId}.");
            case StatutCompletion.DejaCompletee:
                throw new McpException("Occurrence déjà complétée.");
        }

        if (resultat.Prochaine is null)
        {
            return new { complete = true };
        }
        var assigne = resultat.Prochaine.AssigneAId is { } assigneId
            ? await db.Utilisateurs.Where(u => u.Id == assigneId).Select(u => u.NomUtilisateur).SingleOrDefaultAsync()
            : null;
        return new
        {
            complete = true,
            prochaine = new
            {
                id = resultat.Prochaine.Id,
                echeance = resultat.Prochaine.Echeance,
                assigneA = assigne,
            },
        };
    }

    [McpServerTool(Name = "gerer_occurrence")]
    [Description("Agit sur une occurrence : 'annuler-completion' défait la complétion la plus " +
        "récente d'une tâche (le journal est effacé, l'occurrence suivante matérialisée est " +
        "supprimée — refusé si une complétion plus récente existe ou si la suivante a déjà été " +
        "traitée) ; 'passer' saute une occurrence récurrente sans la marquer faite (aucun journal, " +
        "la suivante est créée comme après une complétion aujourd'hui) ; 'reporter' glisse " +
        "l'échéance de l'occurrence en attente sans toucher la définition de la tâche.")]
    public static async Task<object> GererOccurrence(
        HouseOsDbContext db,
        [Description("annuler-completion, passer ou reporter.")] string action,
        [Description("Id de l'occurrence (via lister_occurrences).")] Guid occurrenceId,
        [Description("Requis pour passer : au nom de qui ('alain' ou 'ariane') — alimente la " +
            "stratégie d'assignation de la suivante. Demander si ambigu.")] string? agirComme = null,
        [Description("Requis pour reporter : nouvelle échéance YYYY-MM-DD (aujourd'hui ou plus tard).")]
        string? echeance = null)
    {
        switch (action)
        {
            case "annuler-completion":
            {
                var statut = await OperationsTaches.AnnulerCompletionAsync(db, occurrenceId);
                return statut switch
                {
                    StatutAnnulation.Introuvable =>
                        throw new McpException($"Occurrence introuvable : {occurrenceId}."),
                    StatutAnnulation.PasCompletee =>
                        throw new McpException("L'occurrence n'est pas complétée."),
                    StatutAnnulation.PasLaDerniere =>
                        throw new McpException("Cette complétion n'est pas la plus récente."),
                    StatutAnnulation.ProchaineDejaTraitee =>
                        throw new McpException("La prochaine occurrence a déjà été traitée."),
                    _ => new { annulee = true, occurrenceId },
                };
            }
            case "passer":
            {
                if (string.IsNullOrWhiteSpace(agirComme))
                {
                    throw new McpException("Le paramètre agirComme est requis pour passer.");
                }
                var utilisateur = await AgirComme.ResoudreAsync(db, agirComme);
                var resultat = await OperationsTaches.PasserAsync(
                    db, occurrenceId, utilisateur.Id, DateTimeOffset.UtcNow);
                switch (resultat.Statut)
                {
                    case StatutPasse.Introuvable:
                        throw new McpException($"Occurrence introuvable : {occurrenceId}.");
                    case StatutPasse.DejaTraitee:
                        throw new McpException("Occurrence déjà traitée.");
                    case StatutPasse.TachePonctuelle:
                        throw new McpException("Une tâche ponctuelle ne se passe pas (la supprimer via gerer_tache).");
                }

                var assigne = resultat.Prochaine?.AssigneAId is { } assigneId
                    ? await db.Utilisateurs.Where(u => u.Id == assigneId)
                        .Select(u => u.NomUtilisateur).SingleOrDefaultAsync()
                    : null;
                return new
                {
                    passee = true,
                    prochaine = resultat.Prochaine is null
                        ? null
                        : new
                        {
                            id = resultat.Prochaine.Id,
                            echeance = resultat.Prochaine.Echeance,
                            assigneA = assigne,
                        },
                };
            }
            case "reporter":
            {
                var nouvelleEcheance = Conversions.ParserDate(echeance, "echeance")
                    ?? throw new McpException("Le paramètre echeance est requis pour reporter.");
                var statut = await OperationsTaches.ReporterAsync(
                    db, occurrenceId, nouvelleEcheance, DateOnly.FromDateTime(DateTime.Now));
                return statut switch
                {
                    StatutReport.Introuvable =>
                        throw new McpException($"Occurrence introuvable : {occurrenceId}."),
                    StatutReport.DejaTraitee =>
                        throw new McpException("Occurrence déjà traitée."),
                    StatutReport.DateInvalide =>
                        throw new McpException("L'échéance reportée ne peut pas être dans le passé."),
                    _ => new { reportee = true, echeance = nouvelleEcheance },
                };
            }
            default:
                throw new McpException($"Action inconnue : '{action}' (annuler-completion, passer ou reporter).");
        }
    }
}
