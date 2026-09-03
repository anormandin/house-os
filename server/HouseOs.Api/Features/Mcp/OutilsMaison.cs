using System.ComponentModel;
using HouseOs.Api.Domaine;
using HouseOs.Api.Features.Budget;
using HouseOs.Api.Features.ComptesARebours;
using HouseOs.Api.Features.Courriel;
using HouseOs.Api.Features.Documents;
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

/// <summary>Métadonnées d'un document pour modifier via MCP (remplace la fiche complète).</summary>
public record DocumentDonnees(
    [property: Description("Titre (requis).")] string Titre,
    [property: Description("Catégorie : Manuel, Photo, Assurance, Facture, Garantie, Contrat, " +
        "PlanPermis, ImpotsTaxes ou Autre (requise).")] string Categorie,
    [property: Description("Id d'un équipement existant (via lister_equipements), ou null pour délier.")] Guid? EquipementId,
    [property: Description("Id d'une zone existante (via lister_zones), ou null pour délier.")] Guid? ZoneId,
    [property: Description("Dossier libre de classement (« Maison », « Impôts 2026 »…), ou null.")] string? Dossier,
    string? Notes,
    [property: Description("Date portée par le document YYYY-MM-DD (facture, contrat…), ou null.")] string? DateDocument,
    [property: Description("Échéance YYYY-MM-DD (rappel visuel dans l'app), ou null.")] string? Echeance,
    [property: Description("Sortir de la boîte À classer (false) ou y remettre (true) ; null = inchangé.")]
    bool? AClasser = null);

/// <summary>Fiche du compte fonds de prévoyance (dates en chaînes YYYY-MM-DD).</summary>
public record CompteBudgetDonnees(
    [property: Description("Nom du compte (requis).")] string Nom,
    string? Institution,
    [property: Description("Solde au moment de l'ancrage.")] decimal SoldeInitial,
    [property: Description("Date d'ancrage YYYY-MM-DD (requise).")] string? DateAncrage,
    [property: Description("Id de la tâche récurrente de virement mensuel (via lister_taches), " +
        "ou null.")] Guid? TacheVirementId);

/// <summary>Versement daté d'un échéancier de taxes.</summary>
public record VersementDonnees(
    [property: Description("Date YYYY-MM-DD.")] string Date,
    decimal Montant);

/// <summary>Fiche d'une enveloppe budgétaire (remplace la fiche complète).</summary>
public record EnveloppeDonnees(
    [property: Description("Nom (requis).")] string Nom,
    [property: Description("Equipement, Taxes, Projet ou Reserve (requis).")] string Type,
    [property: Description("Montant cible en dollars, ou null (Reserve).")] decimal? MontantCible,
    [property: Description("Date cible YYYY-MM-DD — ignorée si une tâche est liée " +
        "(l'échéance dérive de sa prochaine occurrence).")] string? DateCible,
    [property: Description("Id d'une tâche liée (exclusif avec equipementId).")] Guid? TacheId,
    [property: Description("Id d'un équipement lié (exclusif avec tacheId).")] Guid? EquipementId,
    [property: Description("Échéancier de versements (type Taxes seulement).")]
    List<VersementDonnees>? Echeancier);

/// <summary>Part d'une ventilation de dépôt (ou l'unique enveloppe d'un retrait).</summary>
public record VentilationDonnees(
    Guid EnveloppeId,
    [property: Description("Part positive en dollars. Pour un retrait : la valeur absolue " +
        "du montant de la transaction.")] decimal Montant);

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
        switch (Conversions.NormaliserAction(action))
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
        "fin de garantie, nombre de documents liés).")]
    public static async Task<List<EquipementResumeDto>> ListerEquipements(HouseOsDbContext db) =>
        await db.Equipements
            .OrderBy(e => e.Nom)
            .Select(e => new EquipementResumeDto(
                e.Id, e.Nom, e.ZoneId, e.Marque, e.Modele, e.FinGarantie,
                db.Documents.Count(d => d.EquipementId == e.Id)))
            .ToListAsync();

    [McpServerTool(Name = "obtenir_equipement")]
    [Description("Détail d'un équipement : specs, historique d'entretien (20 dernières " +
        "complétions de tâches liées) et métadonnées des documents liés. Le téléversement et le " +
        "téléchargement de fichiers passent par l'interface web, pas par MCP.")]
    public static async Task<EquipementDetailDto> ObtenirEquipement(
        HouseOsDbContext db,
        [Description("Id de l'équipement.")] Guid id) =>
        await EquipementsEndpoints.ChargerDetailAsync(db, id)
            ?? throw new McpException($"Équipement introuvable : {id}.");

    [McpServerTool(Name = "gerer_equipement")]
    [Description("Créer, modifier ou supprimer un équipement. Modifier remplace la fiche " +
        "complète. Supprimer un équipement délie ses documents sans les effacer " +
        "(gestion des documents : lister_documents / gerer_document).")]
    public static async Task<object> GererEquipement(
        HouseOsDbContext db,
        [Description("creer, modifier ou supprimer.")] string action,
        [Description("Id de l'équipement (requis sauf pour creer).")] Guid? id = null,
        [Description("Fiche complète (requise pour creer et modifier).")] EquipementDonnees? donnees = null)
    {
        switch (Conversions.NormaliserAction(action))
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
                var equipement = await db.Equipements.FindAsync(RequisId(id))
                    ?? throw new McpException($"Équipement introuvable : {id}.");
                // Les documents liés survivent (FK en SET NULL) — aucun fichier effacé.
                db.Equipements.Remove(equipement);
                await db.SaveChangesAsync();
                return new { supprime = true, id = equipement.Id };
            }
            default:
                throw new McpException($"Action inconnue : '{action}' (creer, modifier ou supprimer).");
        }
    }

    [McpServerTool(Name = "lister_documents")]
    [Description("Liste les documents de la maison (classeur : actes, assurances, factures, " +
        "manuels, photos…) : métadonnées, liens équipement/zone, échéances. Le téléversement " +
        "et le téléchargement de fichiers passent par l'interface web, pas par MCP.")]
    public static async Task<List<DocumentDto>> ListerDocuments(
        HouseOsDbContext db,
        [Description("Filtrer par catégorie : Manuel, Photo, Assurance, Facture, Garantie, " +
            "Contrat, PlanPermis, ImpotsTaxes ou Autre.")] string? categorie = null,
        [Description("Filtrer par équipement lié.")] Guid? equipementId = null,
        [Description("Filtrer par dossier de classement (valeur exacte).")] string? dossier = null,
        [Description("true = seulement la boîte À classer (documents arrivés par courriel, " +
            "pas encore confirmés) ; false = seulement les classés.")] bool? aClasser = null)
    {
        var documents = db.Documents.AsNoTracking();
        if (aClasser is { } aTrier)
        {
            documents = documents.Where(d => d.AClasser == aTrier);
        }
        if (string.IsNullOrWhiteSpace(categorie) == false)
        {
            if (Conversions.ParserEnum(categorie, out CategorieDocument cat) == false)
            {
                throw new McpException($"Catégorie inconnue : '{categorie}'.");
            }
            documents = documents.Where(d => d.Categorie == cat);
        }
        if (equipementId is not null)
        {
            documents = documents.Where(d => d.EquipementId == equipementId);
        }
        if (string.IsNullOrWhiteSpace(dossier) == false)
        {
            documents = documents.Where(d => d.Dossier == dossier);
        }
        return await documents
            .OrderByDescending(d => d.CreeLe)
            .Select(d => new DocumentDto(
                d.Id, d.Titre, d.Categorie.ToString(),
                d.EquipementId,
                db.Equipements.Where(e => e.Id == d.EquipementId).Select(e => e.Nom).FirstOrDefault(),
                d.ZoneId,
                db.Zones.Where(z => z.Id == d.ZoneId).Select(z => z.Nom).FirstOrDefault(),
                d.Dossier, d.Notes, d.DateDocument, d.Echeance,
                d.NomFichier, d.TypeMime, d.Taille, d.CreeLe, d.AClasser, d.ImportCourrielId))
            .ToListAsync();
    }

    [McpServerTool(Name = "gerer_document")]
    [Description("Modifier les métadonnées d'un document (remplace la fiche : titre, catégorie, " +
        "liens équipement/zone, dossier, dates, notes), le classer (sortir de la boîte À classer " +
        "sans toucher à la fiche) ou le supprimer (efface aussi le fichier disque). " +
        "L'ajout d'un document passe par l'interface web ou par courriel (relever_courriels).")]
    public static async Task<object> GererDocument(
        HouseOsDbContext db,
        IConfiguration config,
        IWebHostEnvironment env,
        [Description("modifier, classer ou supprimer.")] string action,
        [Description("Id du document (via lister_documents).")] Guid? id = null,
        [Description("Métadonnées complètes (requises pour modifier).")] DocumentDonnees? donnees = null)
    {
        switch (Conversions.NormaliserAction(action))
        {
            case "modifier":
            {
                var document = await db.Documents.FindAsync(RequisId(id))
                    ?? throw new McpException($"Document introuvable : {id}.");
                if (donnees is null)
                {
                    throw new McpException("Le paramètre donnees est requis pour modifier.");
                }
                if (string.IsNullOrWhiteSpace(donnees.Titre))
                {
                    throw new McpException("Le titre est requis.");
                }
                if (donnees.Titre.Trim().Length > 200)
                {
                    throw new McpException("Le titre ne peut pas dépasser 200 caractères.");
                }
                if (Conversions.ParserEnum(donnees.Categorie, out CategorieDocument categorie) == false)
                {
                    throw new McpException($"Catégorie inconnue : '{donnees.Categorie}'.");
                }
                if (donnees.EquipementId is { } equipementId
                    && await db.Equipements.AnyAsync(e => e.Id == equipementId) == false)
                {
                    throw new McpException($"equipementId inconnu : {equipementId} (voir lister_equipements).");
                }
                if (donnees.ZoneId is { } zoneId && await db.Zones.AnyAsync(z => z.Id == zoneId) == false)
                {
                    throw new McpException($"zoneId inconnu : {zoneId} (voir lister_zones).");
                }

                var dossier = string.IsNullOrWhiteSpace(donnees.Dossier) ? null : donnees.Dossier.Trim();
                if (dossier?.Length > 100)
                {
                    throw new McpException("Le dossier ne peut pas dépasser 100 caractères.");
                }
                var notes = string.IsNullOrWhiteSpace(donnees.Notes) ? null : donnees.Notes.Trim();
                if (notes?.Length > 2000)
                {
                    throw new McpException("Les notes ne peuvent pas dépasser 2000 caractères.");
                }

                document.Titre = donnees.Titre.Trim();
                document.Categorie = categorie;
                document.EquipementId = donnees.EquipementId;
                document.ZoneId = donnees.ZoneId;
                document.Dossier = dossier;
                document.Notes = notes;
                document.DateDocument = Conversions.ParserDate(donnees.DateDocument, "dateDocument");
                document.Echeance = Conversions.ParserDate(donnees.Echeance, "echeance");
                if (donnees.AClasser is { } aClasser)
                {
                    document.AClasser = aClasser;
                }
                await db.SaveChangesAsync();
                return new { modifie = true, id = document.Id };
            }
            case "classer":
            {
                var document = await db.Documents.FindAsync(RequisId(id))
                    ?? throw new McpException($"Document introuvable : {id}.");
                document.AClasser = false;
                await db.SaveChangesAsync();
                return new { classe = true, id = document.Id };
            }
            case "supprimer":
            {
                if (await DocumentsEndpoints.SupprimerAsync(db, RequisId(id), config, env) == false)
                {
                    throw new McpException($"Document introuvable : {id}.");
                }
                return new { supprime = true, id };
            }
            default:
                throw new McpException($"Action inconnue : '{action}' (modifier, classer ou supprimer).");
        }
    }

    [McpServerTool(Name = "relever_courriels")]
    [Description("Relève maintenant la boîte documents@ (dépôt Cloudflare R2) sans attendre le " +
        "passage automatique : chaque courriel transféré devient un ou des documents dans la boîte " +
        "À classer (voir lister_documents aClasser=true). Retourne le rapport du passage.")]
    public static async Task<object> ReleverCourriels(CourrielEntrantService service, CancellationToken ct)
    {
        var rapport = await service.ReleverAsync(ct);
        if (rapport.Actif == false)
        {
            throw new McpException("Relevé du courriel non configuré sur ce serveur (section Courriel:R2).");
        }
        if (rapport.DejaEnCours)
        {
            throw new McpException("Un relevé est déjà en cours — réessayer dans une minute.");
        }
        return new
        {
            nbCourriels = rapport.NbCourriels,
            nbDocuments = rapport.NbDocuments,
            nbIgnores = rapport.NbIgnores,
            erreurs = rapport.Erreurs,
        };
    }

    [McpServerTool(Name = "gerer_comptes_a_rebours")]
    [Description("Comptes à rebours affichés dans l'app (ex. le déménagement). Actions : lister " +
        "(inclut les passés), creer, modifier, supprimer. Icônes : Camion, Sapin, Avion, Valise, " +
        "Gateau, Cadeau, Coeur, Soleil (défaut), Flocon, Citrouille, Feuille, Fleur, Tente, " +
        "Velo, Ballon, Etoile.")]
    public static async Task<object> GererComptesARebours(
        HouseOsDbContext db,
        [Description("lister, creer, modifier ou supprimer.")] string action,
        [Description("Id du compte (requis pour modifier et supprimer).")] Guid? id = null,
        [Description("Titre (requis pour creer).")] string? titre = null,
        [Description("Date cible YYYY-MM-DD (requise pour creer).")] string? dateCible = null,
        [Description("Icône (voir la liste).")] string? icone = null)
    {
        switch (Conversions.NormaliserAction(action))
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
                AppliquerCompte(compte,
                    string.IsNullOrWhiteSpace(titre) ? compte.Titre : titre,
                    dateCible, icone, estCreation: false);
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

    [McpServerTool(Name = "bilan_budget")]
    [Description("Bilan du fonds de prévoyance : compte (solde courant, ancrage, tâche de " +
        "virement), enveloppes (solde = Σ mouvements, provision mensuelle calculée, échéance " +
        "dérivée de la tâche liée), invariant solde = enveloppes + non affecté, virement " +
        "mensuel suggéré, sorties prévues et transactions à rapprocher. Avec enveloppeId : " +
        "détail d'une enveloppe (échéancier + mouvements avec solde après chacun).")]
    public static async Task<object> BilanBudget(
        HouseOsDbContext db,
        [Description("Id d'une enveloppe pour son détail, ou null pour le bilan complet.")]
        Guid? enveloppeId = null,
        [Description("Date du jour YYYY-MM-DD (défaut : aujourd'hui, heure du serveur).")]
        string? date = null)
    {
        var aujourdhui = Conversions.ParserDate(date, "date") ?? BudgetEndpoints.Aujourdhui();
        if (enveloppeId is { } id)
        {
            return await BudgetEndpoints.ChargerEnveloppeDetailAsync(db, id, aujourdhui)
                ?? throw new McpException($"Enveloppe introuvable : {id}.");
        }
        return new
        {
            Resume = await BudgetEndpoints.ChargerResumeAsync(db, aujourdhui),
            TransactionsARapprocher = await BudgetEndpoints.ChargerTransactionsAsync(
                db, StatutTransaction.Nouvelle, aujourdhui),
        };
    }

    [McpServerTool(Name = "gerer_budget")]
    [Description("Gérer le fonds de prévoyance. Actions : ancrer_compte, modifier_compte " +
        "(fiche compte), creer_enveloppe, modifier_enveloppe (fiche enveloppe), " +
        "fermer_enveloppe (exige un solde à zéro), ajouter_mouvement (typeMouvement " +
        "Provision|Retrait|Ajustement, montant signé), transferer (deEnveloppeId → " +
        "versEnveloppeId, montant positif), lier_transaction (retrait : une seule part ; " +
        "dépôt : ventilation multi-enveloppes, le reste demeure non affecté), " +
        "ignorer_transaction, restaurer_transaction (ramène une ignorée dans l'inbox). " +
        "L'import de fichiers CSV/OFX passe par l'interface web, pas par MCP.")]
    public static async Task<object> GererBudget(
        HouseOsDbContext db,
        [Description("ancrer_compte, modifier_compte, creer_enveloppe, modifier_enveloppe, " +
            "fermer_enveloppe, ajouter_mouvement, transferer, lier_transaction, " +
            "ignorer_transaction ou restaurer_transaction.")] string action,
        [Description("Id de l'enveloppe (modifier/fermer/ajouter_mouvement) ou de la " +
            "transaction (lier/ignorer/restaurer).")] Guid? id = null,
        [Description("Fiche du compte (requise pour ancrer_compte et modifier_compte).")]
        CompteBudgetDonnees? compte = null,
        [Description("Fiche de l'enveloppe (requise pour creer_enveloppe et modifier_enveloppe).")]
        EnveloppeDonnees? enveloppe = null,
        [Description("Provision, Retrait ou Ajustement (ajouter_mouvement).")]
        string? typeMouvement = null,
        [Description("Montant signé (ajouter_mouvement : retrait négatif) ou positif (transferer).")]
        decimal? montant = null,
        [Description("Date du mouvement YYYY-MM-DD (défaut : aujourd'hui).")] string? date = null,
        [Description("Note libre du mouvement ou de la liaison.")] string? note = null,
        [Description("Enveloppe source (transferer).")] Guid? deEnveloppeId = null,
        [Description("Enveloppe destination (transferer).")] Guid? versEnveloppeId = null,
        [Description("Parts de la liaison (lier_transaction) : une seule pour un retrait, " +
            "plusieurs pour ventiler un dépôt.")] List<VentilationDonnees>? ventilation = null,
        [Description("Entrée du journal de complétion à lier (retrait seulement).")]
        Guid? entreeJournalId = null)
    {
        switch (Conversions.NormaliserAction(action))
        {
            case "ancrer_compte":
            {
                if (await db.ComptesBudget.AnyAsync())
                {
                    throw new McpException("Le compte est déjà ancré (action modifier_compte).");
                }
                var nouveau = new CompteBudget
                {
                    Id = Guid.NewGuid(),
                    Nom = string.Empty,
                    CreeLe = DateTimeOffset.UtcNow,
                };
                await AppliquerCompteBudget(db, nouveau, compte);
                db.ComptesBudget.Add(nouveau);
                await db.SaveChangesAsync();
                return new { id = nouveau.Id };
            }
            case "modifier_compte":
            {
                var existant = await db.ComptesBudget.FirstOrDefaultAsync()
                    ?? throw new McpException("Aucun compte ancré (action ancrer_compte).");
                await AppliquerCompteBudget(db, existant, compte);
                await db.SaveChangesAsync();
                return new { modifie = true, id = existant.Id };
            }
            case "creer_enveloppe":
            {
                var nouvelle = new Enveloppe
                {
                    Id = Guid.NewGuid(),
                    Nom = string.Empty,
                    CreeLe = DateTimeOffset.UtcNow,
                };
                await AppliquerEnveloppeBudget(db, nouvelle, enveloppe);
                db.Enveloppes.Add(nouvelle);
                await db.SaveChangesAsync();
                return new { id = nouvelle.Id };
            }
            case "modifier_enveloppe":
            {
                var existante = await TrouverEnveloppe(db, id);
                await AppliquerEnveloppeBudget(db, existante, enveloppe);
                await db.SaveChangesAsync();
                return new { modifie = true, id = existante.Id };
            }
            case "fermer_enveloppe":
            {
                var existante = await TrouverEnveloppe(db, id);
                var solde = await BudgetEndpoints.SoldeEnveloppeAsync(db, existante.Id);
                try
                {
                    existante.Fermer(solde);
                }
                catch (InvalidOperationException e)
                {
                    throw new McpException(e.Message);
                }
                await db.SaveChangesAsync();
                return new { fermee = true, id = existante.Id };
            }
            case "ajouter_mouvement":
            {
                var existante = await TrouverEnveloppe(db, id);
                var requete = new MouvementRequete(
                    typeMouvement ?? "",
                    montant ?? throw new McpException("Le paramètre montant est requis."),
                    Conversions.ParserDate(date, "date"),
                    note);
                if (BudgetEndpoints.ValiderMouvement(requete, existante, out var type) is { } erreur)
                {
                    throw new McpException(erreur.Message);
                }
                db.MouvementsEnveloppe.Add(new MouvementEnveloppe
                {
                    Id = Guid.NewGuid(),
                    EnveloppeId = existante.Id,
                    Date = requete.Date ?? BudgetEndpoints.Aujourdhui(),
                    Montant = requete.Montant,
                    Type = type,
                    Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
                    CreeLe = DateTimeOffset.UtcNow,
                });
                await db.SaveChangesAsync();
                return new { ajoute = true, enveloppeId = existante.Id };
            }
            case "transferer":
            {
                var requete = new TransfertRequete(
                    deEnveloppeId ?? throw new McpException("deEnveloppeId est requis."),
                    versEnveloppeId ?? throw new McpException("versEnveloppeId est requis."),
                    montant ?? throw new McpException("Le paramètre montant est requis."),
                    note);
                var erreur = await BudgetEndpoints.TransfererAsync(
                    db, requete, Conversions.ParserDate(date, "date") ?? BudgetEndpoints.Aujourdhui());
                if (erreur is not null)
                {
                    throw new McpException(erreur);
                }
                await db.SaveChangesAsync();
                return new { transfere = true };
            }
            case "lier_transaction":
            {
                var transaction = await db.TransactionsBancaires.FindAsync(RequisId(id))
                    ?? throw new McpException($"Transaction introuvable : {id}.");
                if (transaction.Statut != StatutTransaction.Nouvelle)
                {
                    throw new McpException("Transaction déjà traitée.");
                }
                var lignes = ventilation?.Select(v => new VentilationLigne(v.EnveloppeId, v.Montant)).ToList()
                    ?? throw new McpException("Le paramètre ventilation est requis.");
                // Réclamation atomique du statut, comme LierAsync côté REST : un rejeu
                // concurrent (timeout du client, deux sessions) obtient « déjà traitée »
                // au lieu de doubler les mouvements.
                await using var portee = await db.Database.BeginTransactionAsync();
                var reclamees = await db.TransactionsBancaires
                    .Where(t => t.Id == transaction.Id && t.Statut == StatutTransaction.Nouvelle)
                    .ExecuteUpdateAsync(s => s.SetProperty(t => t.Statut, StatutTransaction.Liee));
                if (reclamees == 0)
                {
                    throw new McpException("Transaction déjà traitée.");
                }
                var erreur = await BudgetEndpoints.CreerLiaisonAsync(
                    db, transaction, new LierRequete(lignes, entreeJournalId, note));
                if (erreur is { } e)
                {
                    throw new McpException(e.Message); // rollback à la sortie de la portée
                }
                await db.SaveChangesAsync();
                await portee.CommitAsync();
                return new { liee = true, id = transaction.Id };
            }
            case "ignorer_transaction":
            {
                var transaction = await db.TransactionsBancaires.FindAsync(RequisId(id))
                    ?? throw new McpException($"Transaction introuvable : {id}.");
                if (transaction.Statut != StatutTransaction.Nouvelle)
                {
                    throw new McpException("Transaction déjà traitée.");
                }
                transaction.Statut = StatutTransaction.Ignoree;
                await db.SaveChangesAsync();
                return new { ignoree = true, id = transaction.Id };
            }
            case "restaurer_transaction":
            {
                var idTransaction = RequisId(id);
                return await BudgetEndpoints.RestaurerAsync(db, idTransaction) switch
                {
                    StatutRestauration.Introuvable =>
                        throw new McpException($"Transaction introuvable : {idTransaction}."),
                    StatutRestauration.PasIgnoree =>
                        throw new McpException("Seule une transaction ignorée se restaure."),
                    _ => new { restauree = true, id = idTransaction },
                };
            }
            default:
                throw new McpException($"Action inconnue : '{action}' (ancrer_compte, " +
                    "modifier_compte, creer_enveloppe, modifier_enveloppe, fermer_enveloppe, " +
                    "ajouter_mouvement, transferer, lier_transaction, ignorer_transaction " +
                    "ou restaurer_transaction).");
        }
    }

    private static async Task AppliquerCompteBudget(
        HouseOsDbContext db, CompteBudget cible, CompteBudgetDonnees? donnees)
    {
        if (donnees is null)
        {
            throw new McpException("Le paramètre compte est requis pour cette action.");
        }
        var requete = new CompteBudgetRequete(
            donnees.Nom, donnees.Institution, donnees.SoldeInitial,
            Conversions.ParserDate(donnees.DateAncrage, "dateAncrage"),
            donnees.TacheVirementId);
        if (await BudgetEndpoints.AppliquerCompte(requete, cible, db) is { } erreur)
        {
            throw new McpException(erreur.Message);
        }
    }

    private static async Task AppliquerEnveloppeBudget(
        HouseOsDbContext db, Enveloppe cible, EnveloppeDonnees? donnees)
    {
        if (donnees is null)
        {
            throw new McpException("Le paramètre enveloppe est requis pour cette action.");
        }
        var echeancier = donnees.Echeancier?
            .Select(v => new Versement(
                Conversions.ParserDate(v.Date, "echeancier.date")
                    ?? throw new McpException("Chaque versement exige une date YYYY-MM-DD."),
                v.Montant))
            .ToList();
        var requete = new EnveloppeRequete(
            donnees.Nom, donnees.Type, donnees.MontantCible,
            Conversions.ParserDate(donnees.DateCible, "dateCible"),
            donnees.TacheId, donnees.EquipementId, echeancier);
        if (await BudgetEndpoints.AppliquerEnveloppe(requete, cible, db) is { } erreur)
        {
            throw new McpException(erreur.Message);
        }
    }

    private static async Task<Enveloppe> TrouverEnveloppe(HouseOsDbContext db, Guid? id) =>
        await db.Enveloppes.FindAsync(RequisId(id))
            ?? throw new McpException($"Enveloppe introuvable : {id}.");

    private static Guid RequisId(Guid? id) =>
        id ?? throw new McpException("Le paramètre id est requis pour cette action.");

    private static async Task<Zone> TrouverZone(HouseOsDbContext db, Guid? id) =>
        await db.Zones.FindAsync(RequisId(id))
            ?? throw new McpException($"Zone introuvable : {id}.");

    private static void AppliquerZone(Zone zone, string? nom, string? type, int? ordre)
    {
        // Nom vide ou blanc = « ne pas toucher » (fiche partielle d'un client LLM),
        // jamais un effacement ; à la création le nom reste donc requis.
        var nomFinal = string.IsNullOrWhiteSpace(nom) ? zone.Nom : nom;
        if (string.IsNullOrWhiteSpace(nomFinal))
        {
            throw new McpException("Le nom est requis.");
        }
        var typeFinal = zone.Type;
        if (type is not null && Conversions.ParserEnum(type, out typeFinal) == false)
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
        var requete = new EquipementRequete(
            donnees.Nom, donnees.ZoneId, donnees.Marque, donnees.Modele, donnees.NumeroSerie,
            Conversions.ParserDate(donnees.DateAchat, "dateAchat"),
            Conversions.ParserDate(donnees.FinGarantie, "finGarantie"),
            donnees.Notes, donnees.Specs);
        // Mêmes règles que le POST/PUT REST (longueurs, zone, dates, bornes des
        // specs) : sans elles, Postgres répondrait par une erreur brute.
        if (await EquipementsEndpoints.ValiderAsync(requete, db) is { } erreur)
        {
            throw new McpException(erreur.Message);
        }
        return requete;
    }

    private static void AppliquerCompte(
        CompteARebours compte, string? titre, string? dateCible, string? icone, bool estCreation)
    {
        if (string.IsNullOrWhiteSpace(titre))
        {
            throw new McpException("Le titre est requis.");
        }
        if (titre.Trim().Length > 200)
        {
            throw new McpException("Le titre ne peut pas dépasser 200 caractères.");
        }
        var date = Conversions.ParserDate(dateCible, "dateCible");
        if (date is null && estCreation)
        {
            throw new McpException("dateCible est requise pour creer (YYYY-MM-DD).");
        }
        var iconeFinale = compte.Icone;
        if (icone is not null && Conversions.ParserEnum(icone, out iconeFinale) == false)
        {
            // Liste dérivée de l'enum : elle ne peut plus se désynchroniser du set réel.
            throw new McpException(
                $"Icône inconnue : '{icone}' ({string.Join(", ", Enum.GetNames<IconeCompteARebours>())}).");
        }
        compte.Titre = titre.Trim();
        compte.DateCible = date ?? compte.DateCible;
        compte.Icone = iconeFinale;
    }
}
