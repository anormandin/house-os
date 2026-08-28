using HouseOs.Api.Domaine;
using HouseOs.Api.Features.Mcp;
using HouseOs.Api.Features.Zones;
using HouseOs.Tests.Features.Taches;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol;

namespace HouseOs.Tests.Features.Mcp;

/// <summary>
/// Outils MCP maison (zones, comptes à rebours) : fiches partielles non destructives,
/// enums tolérants à la casse mais fermés aux valeurs numériques, messages d'erreur
/// qui énumèrent les choix réels.
/// </summary>
public class OutilsMaisonTests : TestAvecSqlite
{
    private async Task<Guid> CreerZone(string nom = "Cuisine")
    {
        var dto = (ZoneDto)await OutilsMaison.GererZone(Db, "creer", nom: nom);
        return dto.Id;
    }

    [Fact]
    public async Task Modifier_une_zone_avec_un_nom_vide_conserve_le_nom()
    {
        var id = await CreerZone("Garage");

        // Fiche partielle d'un client LLM : « change juste l'ordre » — le nom vide
        // signifie « ne pas toucher », jamais un effacement ni un refus.
        var dto = (ZoneDto)await OutilsMaison.GererZone(Db, "modifier", id, nom: "  ", ordre: 4);

        Assert.Equal("Garage", dto.Nom);
        Assert.Equal(4, dto.Ordre);
    }

    [Fact]
    public async Task Le_type_de_zone_est_accepte_en_minuscules()
    {
        var dto = (ZoneDto)await OutilsMaison.GererZone(Db, "creer", nom: "Cour", type: "exterieur");

        Assert.Equal(nameof(TypeZone.Exterieur), dto.Type);
    }

    [Fact]
    public async Task Un_type_de_zone_numerique_est_refuse()
    {
        // Enum.TryParse accepte « 999 » : sans garde, la chaîne « 999 » serait
        // persistée comme type et renvoyée telle quelle au client.
        var exception = await Assert.ThrowsAsync<McpException>(() =>
            OutilsMaison.GererZone(Db, "creer", nom: "Bizarre", type: "999"));

        Assert.Contains("Type inconnu", exception.Message);
    }

    [Fact]
    public async Task L_action_de_zone_est_acceptee_quelle_qu_en_soit_la_casse()
    {
        var dto = (ZoneDto)await OutilsMaison.GererZone(Db, " CREER ", nom: "Salon");

        Assert.Equal("Salon", dto.Nom);
    }

    [Fact]
    public async Task L_erreur_d_icone_enumere_toutes_les_icones_acceptees()
    {
        var exception = await Assert.ThrowsAsync<McpException>(() =>
            OutilsMaison.GererComptesARebours(
                Db, "creer", titre: "Noël", dateCible: "2026-12-25", icone: "Traîneau"));

        // Liste dérivée de l'enum : les 8 icônes ajoutées le 2026-08-25 y sont aussi.
        foreach (var nom in Enum.GetNames<IconeCompteARebours>())
        {
            Assert.Contains(nom, exception.Message);
        }
    }

    [Fact]
    public async Task Une_icone_en_minuscules_est_acceptee_une_numerique_refusee()
    {
        var compte = await OutilsMaison.GererComptesARebours(
            Db, "creer", titre: "Hiver", dateCible: "2026-12-01", icone: "flocon");
        Assert.Equal(IconeCompteARebours.Flocon, Db.ComptesARebours.Single().Icone);

        await Assert.ThrowsAsync<McpException>(() =>
            OutilsMaison.GererComptesARebours(
                Db, "creer", titre: "Bizarre", dateCible: "2026-12-01", icone: "42"));
    }

    [Fact]
    public async Task Modifier_un_compte_sans_titre_conserve_le_titre()
    {
        await OutilsMaison.GererComptesARebours(
            Db, "creer", titre: "Déménagement", dateCible: "2026-10-06");
        var id = Db.ComptesARebours.Single().Id;

        await OutilsMaison.GererComptesARebours(Db, "modifier", id, dateCible: "2026-10-07");

        Assert.Equal("Déménagement", Db.ComptesARebours.Single().Titre);
        Assert.Equal(new DateOnly(2026, 10, 7), Db.ComptesARebours.Single().DateCible);
    }

    [Fact]
    public async Task Gerer_comptes_a_rebours_refuse_un_titre_trop_long()
    {
        // Même borne que le REST (colonne varchar 200) : parité MCP.
        var exception = await Assert.ThrowsAsync<McpException>(() =>
            OutilsMaison.GererComptesARebours(
                Db, "creer", titre: new string('t', 201), dateCible: "2026-12-25"));

        Assert.Contains("200 caractères", exception.Message);
    }

    private static EquipementDonnees Equipement(
        string nom = "Fournaise",
        string? dateAchat = null,
        string? finGarantie = null,
        Dictionary<string, string>? specs = null) =>
        new(nom, null, null, null, null, dateAchat, finGarantie, null, specs);

    [Fact]
    public async Task Gerer_equipement_cree_une_fiche_valide()
    {
        await OutilsMaison.GererEquipement(Db, "creer", donnees: Equipement("Thermopompe"));

        Assert.Equal("Thermopompe", Db.Equipements.Single().Nom);
    }

    [Fact]
    public async Task Gerer_equipement_refuse_un_nom_trop_long()
    {
        // Sans les validations partagées avec le REST, l'erreur Postgres remonterait brute.
        var exception = await Assert.ThrowsAsync<McpException>(() =>
            OutilsMaison.GererEquipement(Db, "creer", donnees: Equipement(new string('e', 201))));

        Assert.Contains("200 caractères", exception.Message);
    }

    [Fact]
    public async Task Gerer_equipement_refuse_une_spec_avec_caractere_nul()
    {
        var exception = await Assert.ThrowsAsync<McpException>(() =>
            OutilsMaison.GererEquipement(Db, "creer", donnees: Equipement(
                specs: new Dictionary<string, string> { ["filtre"] = "16\0x25" })));

        Assert.Contains("caractère nul", exception.Message);
    }

    [Fact]
    public async Task Gerer_equipement_refuse_une_fin_de_garantie_avant_l_achat()
    {
        var exception = await Assert.ThrowsAsync<McpException>(() =>
            OutilsMaison.GererEquipement(Db, "creer", donnees: Equipement(
                dateAchat: "2026-05-01", finGarantie: "2025-05-01")));

        Assert.Contains("précéder", exception.Message);
    }

    private Guid CreerDocument()
    {
        var document = new Document
        {
            Id = Guid.NewGuid(),
            Titre = "Manuel",
            Categorie = CategorieDocument.Manuel,
            NomFichier = "manuel.pdf",
            CheminDisque = "manuel.pdf",
            TypeMime = "application/pdf",
            CreeLe = DateTimeOffset.UtcNow,
        };
        Db.Documents.Add(document);
        Db.SaveChanges();
        return document.Id;
    }

    private static DocumentDonnees Donnees(
        string titre = "Manuel", string categorie = "Manuel", string? notes = null) =>
        new(titre, categorie, null, null, null, notes, null, null);

    [Fact]
    public async Task Gerer_document_refuse_titre_et_notes_trop_longs()
    {
        var id = CreerDocument();

        var titre = await Assert.ThrowsAsync<McpException>(() => OutilsMaison.GererDocument(
            Db, null!, null!, "modifier", id, Donnees(titre: new string('t', 201))));
        Assert.Contains("200 caractères", titre.Message);

        var notes = await Assert.ThrowsAsync<McpException>(() => OutilsMaison.GererDocument(
            Db, null!, null!, "modifier", id, Donnees(notes: new string('n', 2001))));
        Assert.Contains("2000 caractères", notes.Message);
    }

    [Fact]
    public async Task La_categorie_de_document_tolere_la_casse_et_refuse_les_numeriques()
    {
        var id = CreerDocument();

        await OutilsMaison.GererDocument(Db, null!, null!, "modifier", id, Donnees(categorie: "photo"));
        Assert.Equal(CategorieDocument.Photo, Db.Documents.Single().Categorie);

        // « 999 » passerait Enum.TryParse et serait persisté puis affiché tel quel.
        await Assert.ThrowsAsync<McpException>(() => OutilsMaison.GererDocument(
            Db, null!, null!, "modifier", id, Donnees(categorie: "999")));
        await Assert.ThrowsAsync<McpException>(() =>
            OutilsMaison.ListerDocuments(Db, categorie: "999"));
    }

    private (Guid TransactionId, Guid EnveloppeId) CreerTransactionBudget(
        decimal montant, StatutTransaction statut = StatutTransaction.Nouvelle)
    {
        var compte = new CompteBudget
        {
            Id = Guid.NewGuid(),
            Nom = "Fonds",
            DateAncrage = new DateOnly(2026, 1, 1),
            CreeLe = DateTimeOffset.UtcNow,
        };
        var enveloppe = new Enveloppe
        {
            Id = Guid.NewGuid(),
            Nom = "Réserve",
            Type = TypeEnveloppe.Reserve,
            CreeLe = DateTimeOffset.UtcNow,
        };
        var transaction = new TransactionBancaire
        {
            Id = Guid.NewGuid(),
            CompteBudgetId = compte.Id,
            Date = new DateOnly(2026, 8, 20),
            Montant = montant,
            Description = "QUINCAILLERIE",
            CleDedup = $"hash:{Guid.NewGuid():N}",
            Statut = statut,
            ImporteeLe = DateTimeOffset.UtcNow,
        };
        Db.AddRange(compte, enveloppe, transaction);
        Db.SaveChanges();
        return (transaction.Id, enveloppe.Id);
    }

    [Fact]
    public async Task Gerer_budget_restaure_une_ignoree_et_refuse_les_autres_statuts()
    {
        var (transactionId, _) = CreerTransactionBudget(-25m, StatutTransaction.Ignoree);

        await OutilsMaison.GererBudget(Db, "restaurer_transaction", transactionId);
        Assert.Equal(StatutTransaction.Nouvelle, Db.TransactionsBancaires.Single().Statut);

        // Redevenue Nouvelle : rien à restaurer — seul le statut Ignoree se restaure.
        var exception = await Assert.ThrowsAsync<McpException>(() =>
            OutilsMaison.GererBudget(Db, "restaurer_transaction", transactionId));
        Assert.Contains("ignorée", exception.Message);
    }

    [Fact]
    public async Task Lier_transaction_via_mcp_cree_le_mouvement_et_marque_liee()
    {
        var (transactionId, enveloppeId) = CreerTransactionBudget(-40m);

        await OutilsMaison.GererBudget(Db, "lier_transaction", transactionId,
            ventilation: [new VentilationDonnees(enveloppeId, 40m)]);

        Assert.Equal(StatutTransaction.Liee, Db.TransactionsBancaires.Single().Statut);
        var mouvement = Assert.Single(Db.MouvementsEnveloppe.ToList());
        Assert.Equal(-40m, mouvement.Montant);
        Assert.Equal(transactionId, mouvement.TransactionBancaireId);
    }

    [Fact]
    public async Task Lier_transaction_rejouee_apres_une_liaison_concurrente_est_refusee_sans_doubler()
    {
        var (transactionId, enveloppeId) = CreerTransactionBudget(-40m);
        // Le rejeu type (timeout MCP, deux navigateurs) : ce client a lu « Nouvelle »
        // (entité suivie), mais un client concurrent a déjà réclamé la transaction en base.
        await Db.TransactionsBancaires
            .Where(t => t.Id == transactionId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.Statut, StatutTransaction.Liee));

        var exception = await Assert.ThrowsAsync<McpException>(() =>
            OutilsMaison.GererBudget(Db, "lier_transaction", transactionId,
                ventilation: [new VentilationDonnees(enveloppeId, 40m)]));

        // La réclamation conditionnelle a tranché : aucun mouvement doublé.
        Assert.Contains("déjà traitée", exception.Message);
        Assert.Empty(Db.MouvementsEnveloppe.ToList());
    }

    [Fact]
    public async Task Supprimer_une_zone_detache_ses_taches_et_equipements()
    {
        var id = await CreerZone("Éphémère");
        var alain = new Utilisateur
        {
            Id = Guid.NewGuid(), NomUtilisateur = "alain", NomAffichage = "Alain", MotDePasseHash = "x",
        };
        Db.Utilisateurs.Add(alain);
        var tache = Tache.CreerPonctuelle("Dans la zone", null, null, null, alain.Id, DateTimeOffset.UtcNow);
        tache.ZoneId = id;
        Db.Taches.Add(tache);
        Db.SaveChanges();

        await OutilsMaison.GererZone(Db, "supprimer", id);

        // Le SetNull réel est éprouvé côté Postgres (intégration) ; ici on fige au
        // moins le contrat côté modèle EF.
        Db.ChangeTracker.Clear();
        Assert.Null(Db.Taches.Single().ZoneId);
    }
}
