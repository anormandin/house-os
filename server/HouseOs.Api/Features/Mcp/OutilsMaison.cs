using System.ComponentModel;
using HouseOs.Api.Domaine;
using HouseOs.Api.Features.ComptesARebours;
using HouseOs.Api.Features.Equipements;
using HouseOs.Api.Features.Zones;
using HouseOs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace HouseOs.Api.Features.Mcp;

/// <summary>Champs d'un équipement pour créer/modifier via MCP (dates en chaînes YYYY-MM-DD).</summary>
public record EquipementDonnees(
    [property: Description("Nom de l'équipement (requis).")] string Nom,
    [property: Description("Id d'une zone existante (via lister_zones), ou null.")] Guid? ZoneId,
    string? Marque,
    string? Modele,
    string? NumeroSerie,
    [property: Description("Date d'achat YYYY-MM-DD, ou null.")] string? DateAchat,
    [property: Description("Fin de garantie YYYY-MM-DD, ou null.")] string? FinGarantie,
    string? Notes,
    [property: Description("Caractéristiques libres clé→valeur (ex. {\"HP\": \"12000 BTU\"}).")] Dictionary<string, string>? Specs);

[McpServerToolType]
public static class OutilsMaison
{
    [McpServerTool(Name = "lister_zones")]
    [Description("Liste les zones de la maison (pièces et extérieur) : id, nom, type " +
        "(Interieur|Exterieur), ordre d'affichage.")]
    public static async Task<List<ZoneDto>> ListerZones(HouseOsDbContext db) =>
        await db.Zones
            .OrderBy(z => z.Ordre).ThenBy(z => z.Nom)
            .Select(z => new ZoneDto(z.Id, z.Nom, z.Type.ToString(), z.Ordre))
            .ToListAsync();

    [McpServerTool(Name = "gerer_zone")]
    [Description("Créer, modifier ou supprimer une zone. Supprimer ne détruit rien d'autre : " +
        "les tâches et équipements de la zone sont simplement détachés.")]
    public static async Task<object> GererZone(
        HouseOsDbContext db,
        [Description("creer, modifier ou supprimer.")] string action,
        [Description("Id de la zone (requis pour modifier et supprimer).")] Guid? id = null,
        [Description("Nom (requis pour creer).")] string? nom = null,
        [Description("Interieur (défaut) ou Exterieur.")] string? type = null,
        [Description("Ordre d'affichage (petit = en premier).")] int? ordre = null)
    {
        switch (action)
        {
            case "creer":
            {
                var zone = new Zone { Id = Guid.NewGuid(), Nom = string.Empty };
                AppliquerZone(zone, nom, type, ordre);
                db.Zones.Add(zone);
                await db.SaveChangesAsync();
                return new ZoneDto(zone.Id, zone.Nom, zone.Type.ToString(), zone.Ordre);
            }
            case "modifier":
            {
                var zone = await TrouverZone(db, id);
                AppliquerZone(zone, nom, type, ordre);
                await db.SaveChangesAsync();
                return new ZoneDto(zone.Id, zone.Nom, zone.Type.ToString(), zone.Ordre);
            }
            case "supprimer":
            {
                var zone = await TrouverZone(db, id);
                db.Zones.Remove(zone);
                await db.SaveChangesAsync();
                return new { supprime = true, id = zone.Id };
            }
            default:
                throw new McpException($"Action inconnue : '{action}' (creer, modifier ou supprimer).");
        }
    }

    [McpServerTool(Name = "lister_equipements")]
    [Description("Liste les équipements de la maison (résumé : id, nom, zoneId, marque, modèle, " +
        "fin de garantie, nombre de pièces jointes).")]
    public static async Task<List<EquipementResumeDto>> ListerEquipements(HouseOsDbContext db) =>
        await db.Equipements
            .OrderBy(e => e.Nom)
            .Select(e => new EquipementResumeDto(
                e.Id, e.Nom, e.ZoneId, e.Marque, e.Modele, e.FinGarantie, e.PiecesJointes.Count))
            .ToListAsync();

    [McpServerTool(Name = "obtenir_equipement")]
    [Description("Détail d'un équipement : specs, historique d'entretien (20 dernières " +
        "complétions de tâches liées) et métadonnées des pièces jointes. Le téléversement et le " +
        "téléchargement de fichiers passent par l'interface web, pas par MCP.")]
    public static async Task<EquipementDetailDto> ObtenirEquipement(
        HouseOsDbContext db,
        [Description("Id de l'équipement.")] Guid id) =>
        await EquipementsEndpoints.ChargerDetailAsync(db, id)
            ?? throw new McpException($"Équipement introuvable : {id}.");

    [McpServerTool(Name = "gerer_equipement")]
    [Description("Créer, modifier ou supprimer un équipement, ou supprimer une de ses pièces " +
        "jointes (action supprimer_piece_jointe + pieceJointeId). Modifier remplace la fiche " +
        "complète. L'ajout de pièces jointes n'est pas possible via MCP (interface web).")]
    public static async Task<object> GererEquipement(
        HouseOsDbContext db,
        IConfiguration config,
        IWebHostEnvironment env,
        [Description("creer, modifier, supprimer ou supprimer_piece_jointe.")] string action,
        [Description("Id de l'équipement (requis sauf pour creer).")] Guid? id = null,
        [Description("Id de la pièce jointe (requis pour supprimer_piece_jointe).")] Guid? pieceJointeId = null,
        [Description("Fiche complète (requise pour creer et modifier).")] EquipementDonnees? donnees = null)
    {
        switch (action)
        {
            case "creer":
            {
                var equipement = new Equipement
                {
                    Id = Guid.NewGuid(),
                    Nom = string.Empty,
                    CreeLe = DateTimeOffset.UtcNow,
                };
                EquipementsEndpoints.Appliquer(await ConvertirDonnees(db, donnees), equipement);
                db.Equipements.Add(equipement);
                await db.SaveChangesAsync();
                return new { id = equipement.Id };
            }
            case "modifier":
            {
                var equipement = await db.Equipements.FindAsync(RequisId(id))
                    ?? throw new McpException($"Équipement introuvable : {id}.");
                EquipementsEndpoints.Appliquer(await ConvertirDonnees(db, donnees), equipement);
                await db.SaveChangesAsync();
                return new { modifie = true, id = equipement.Id };
            }
            case "supprimer":
            {
                var equipement = await db.Equipements
                    .Include(e => e.PiecesJointes)
                    .SingleOrDefaultAsync(e => e.Id == RequisId(id))
                    ?? throw new McpException($"Équipement introuvable : {id}.");
                var dossier = EquipementsEndpoints.DossierFichiers(config, env);
                var fichiers = equipement.PiecesJointes.Select(p => Path.Combine(dossier, p.CheminDisque)).ToList();
                db.Equipements.Remove(equipement);
                await db.SaveChangesAsync();
                foreach (var fichier in fichiers.Where(File.Exists))
                {
                    File.Delete(fichier);
                }
                return new { supprime = true, id = equipement.Id };
            }
            case "supprimer_piece_jointe":
            {
                if (pieceJointeId is null)
                {
                    throw new McpException("pieceJointeId est requis pour supprimer_piece_jointe.");
                }
                var pieceJointe = await db.PiecesJointes.FindAsync(pieceJointeId.Value)
                    ?? throw new McpException($"Pièce jointe introuvable : {pieceJointeId}.");
                var chemin = Path.Combine(EquipementsEndpoints.DossierFichiers(config, env), pieceJointe.CheminDisque);
                db.PiecesJointes.Remove(pieceJointe);
                await db.SaveChangesAsync();
                if (File.Exists(chemin))
                {
                    File.Delete(chemin);
                }
                return new { supprime = true, pieceJointeId };
            }
            default:
                throw new McpException(
                    $"Action inconnue : '{action}' (creer, modifier, supprimer ou supprimer_piece_jointe).");
        }
    }

    [McpServerTool(Name = "gerer_comptes_a_rebours")]
    [Description("Comptes à rebours affichés dans l'app (ex. le déménagement). Actions : lister " +
        "(inclut les passés), creer, modifier, supprimer. Icônes : Camion, Sapin, Avion, Valise, " +
        "Gateau, Cadeau, Coeur, Soleil (défaut).")]
    public static async Task<object> GererComptesARebours(
        HouseOsDbContext db,
        [Description("lister, creer, modifier ou supprimer.")] string action,
        [Description("Id du compte (requis pour modifier et supprimer).")] Guid? id = null,
        [Description("Titre (requis pour creer).")] string? titre = null,
        [Description("Date cible YYYY-MM-DD (requise pour creer).")] string? dateCible = null,
        [Description("Icône (voir la liste).")] string? icone = null)
    {
        switch (action)
        {
            case "lister":
                return await db.ComptesARebours
                    .OrderBy(c => c.DateCible)
                    .Select(c => new CompteARebourDto(c.Id, c.Titre, c.DateCible, c.Icone.ToString()))
                    .ToListAsync();
            case "creer":
            {
                var compte = new CompteARebours { Id = Guid.NewGuid(), Titre = string.Empty };
                AppliquerCompte(compte, titre, dateCible, icone, estCreation: true);
                db.ComptesARebours.Add(compte);
                await db.SaveChangesAsync();
                return new CompteARebourDto(compte.Id, compte.Titre, compte.DateCible, compte.Icone.ToString());
            }
            case "modifier":
            {
                var compte = await db.ComptesARebours.FindAsync(RequisId(id))
                    ?? throw new McpException($"Compte à rebours introuvable : {id}.");
                AppliquerCompte(compte, titre ?? compte.Titre, dateCible, icone, estCreation: false);
                await db.SaveChangesAsync();
                return new CompteARebourDto(compte.Id, compte.Titre, compte.DateCible, compte.Icone.ToString());
            }
            case "supprimer":
            {
                var compte = await db.ComptesARebours.FindAsync(RequisId(id))
                    ?? throw new McpException($"Compte à rebours introuvable : {id}.");
                db.ComptesARebours.Remove(compte);
                await db.SaveChangesAsync();
                return new { supprime = true, id = compte.Id };
            }
            default:
                throw new McpException($"Action inconnue : '{action}' (lister, creer, modifier ou supprimer).");
        }
    }

    private static Guid RequisId(Guid? id) =>
        id ?? throw new McpException("Le paramètre id est requis pour cette action.");

    private static async Task<Zone> TrouverZone(HouseOsDbContext db, Guid? id) =>
        await db.Zones.FindAsync(RequisId(id))
            ?? throw new McpException($"Zone introuvable : {id}.");

    private static void AppliquerZone(Zone zone, string? nom, string? type, int? ordre)
    {
        var nomFinal = nom ?? zone.Nom;
        if (string.IsNullOrWhiteSpace(nomFinal))
        {
            throw new McpException("Le nom est requis.");
        }
        var typeFinal = zone.Type;
        if (type is not null && Enum.TryParse(type, out typeFinal) == false)
        {
            throw new McpException($"Type inconnu : '{type}' (Interieur ou Exterieur).");
        }
        zone.Nom = nomFinal.Trim();
        zone.Type = typeFinal;
        zone.Ordre = ordre ?? zone.Ordre;
    }

    private static async Task<EquipementRequete> ConvertirDonnees(HouseOsDbContext db, EquipementDonnees? donnees)
    {
        if (donnees is null)
        {
            throw new McpException("Le paramètre donnees est requis pour creer et modifier.");
        }
        if (string.IsNullOrWhiteSpace(donnees.Nom))
        {
            throw new McpException("Le nom est requis.");
        }
        if (donnees.ZoneId is { } zoneId && await db.Zones.AnyAsync(z => z.Id == zoneId) == false)
        {
            throw new McpException($"zoneId inconnu : {zoneId} (voir lister_zones).");
        }
        return new EquipementRequete(
            donnees.Nom, donnees.ZoneId, donnees.Marque, donnees.Modele, donnees.NumeroSerie,
            Conversions.ParserDate(donnees.DateAchat, "dateAchat"),
            Conversions.ParserDate(donnees.FinGarantie, "finGarantie"),
            donnees.Notes, donnees.Specs);
    }

    private static void AppliquerCompte(
        CompteARebours compte, string? titre, string? dateCible, string? icone, bool estCreation)
    {
        if (string.IsNullOrWhiteSpace(titre))
        {
            throw new McpException("Le titre est requis.");
        }
        var date = Conversions.ParserDate(dateCible, "dateCible");
        if (date is null && estCreation)
        {
            throw new McpException("dateCible est requise pour creer (YYYY-MM-DD).");
        }
        var iconeFinale = compte.Icone;
        if (icone is not null && Enum.TryParse(icone, out iconeFinale) == false)
        {
            throw new McpException(
                $"Icône inconnue : '{icone}' (Camion, Sapin, Avion, Valise, Gateau, Cadeau, Coeur, Soleil).");
        }
        compte.Titre = titre.Trim();
        compte.DateCible = date ?? compte.DateCible;
        compte.Icone = iconeFinale;
    }
}
