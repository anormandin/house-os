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
        "'completees' (les faites, plus récentes d'abord). Sans filtre : tout.")]
    public static async Task<List<OccurrenceDto>> ListerOccurrences(
        HouseOsDbContext db,
        [Description("aujourdhui, avenir, en-attente ou completees.")] string? filtre = null,
        [Description("Date de référence YYYY-MM-DD (défaut : aujourd'hui, heure du serveur).")] string? date = null)
    {
        var aujourdhui = Conversions.ParserDate(date, "date") ?? DateOnly.FromDateTime(DateTime.Now);
        return await OperationsTaches.ListerOccurrencesAsync(db, filtre, aujourdhui, null, null);
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
}
