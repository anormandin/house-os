using HouseOs.Api.Domaine;
using HouseOs.Api.Features.Mcp;
using HouseOs.Api.Features.Zones;
using HouseOs.Tests.Features.Taches;
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
