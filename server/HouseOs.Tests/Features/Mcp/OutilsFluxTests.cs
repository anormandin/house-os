using HouseOs.Api.Domaine;
using HouseOs.Api.Features.FluxExternes;
using HouseOs.Api.Features.Mcp;
using HouseOs.Tests.Features.Taches;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol;

namespace HouseOs.Tests.Features.Mcp;

/// <summary>
/// Outils MCP des calendriers externes — la parité avec REST, dans la même tranche
/// (vault : Serveur MCP). Les flux poussés ne téléchargent rien : la fabrique HTTP de
/// ces tests lève si on l'appelle, ce qui est en soi une assertion.
/// </summary>
public class OutilsFluxTests : TestAvecSqlite
{
    private sealed class FabriqueInterdite : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) =>
            throw new InvalidOperationException("Un flux poussé ne télécharge rien.");
    }

    private static readonly FabriqueInterdite SansReseau = new();

    private async Task<Guid> CreerFluxPousse(string nom = "Ville")
    {
        var dto = (FluxExterneDto)await OutilsFlux.GererFluxExterne(
            Db, SansReseau, "creer", nom: nom, type: "Municipal", source: "Poussee");
        Assert.Null(dto.Url);
        Assert.Equal("Poussee", dto.Source);
        return dto.Id;
    }

    [Fact]
    public async Task Creer_un_flux_pousse_ne_telecharge_rien_et_le_laisse_vide()
    {
        var id = await CreerFluxPousse();

        var flux = Db.FluxExternes.Single(f => f.Id == id);
        Assert.Equal(SourceFluxExterne.Poussee, flux.Source);
        Assert.Equal(TypeFluxExterne.Municipal, flux.Type);
        Assert.Null(flux.DernierRafraichissementLe);
        Assert.Empty(Db.EvenementsExternes);
    }

    [Fact]
    public async Task La_source_est_toleree_en_minuscules_et_refusee_si_inconnue()
    {
        var dto = (FluxExterneDto)await OutilsFlux.GererFluxExterne(
            Db, SansReseau, "creer", nom: "Ville", type: "municipal", source: "poussee");
        Assert.Equal("Poussee", dto.Source);

        var erreur = await Assert.ThrowsAsync<McpException>(() => OutilsFlux.GererFluxExterne(
            Db, SansReseau, "creer", nom: "Ville", source: "tiree"));
        Assert.Contains("Ics ou Poussee", erreur.Message);
    }

    [Fact]
    public async Task Une_url_sur_un_flux_pousse_est_refusee()
    {
        var erreur = await Assert.ThrowsAsync<McpException>(() => OutilsFlux.GererFluxExterne(
            Db, SansReseau, "creer", nom: "Ville", url: "https://exemple.test/c.ics", source: "Poussee"));

        Assert.Contains("pas d'URL", erreur.Message);
    }

    [Fact]
    public async Task Pousser_deux_fois_remplace_les_evenements()
    {
        var id = await CreerFluxPousse();
        var demain = DateOnly.FromDateTime(DateTime.Now.AddDays(1));

        var premiere = (ResultatPoussee)await OutilsFlux.PousserEvenementsFlux(Db, id, [
            new EvenementPousseDonnees("Séance du conseil", $"{demain:yyyy-MM-dd}", "19:30"),
            new EvenementPousseDonnees("Marché public", $"{demain.AddDays(2):yyyy-MM-dd}"),
        ]);
        Assert.Equal(new ResultatPoussee(2, 2, 0), premiere);

        await OutilsFlux.PousserEvenementsFlux(Db, id, [
            new EvenementPousseDonnees("Marché public", $"{demain.AddDays(2):yyyy-MM-dd}"),
        ]);

        Db.ChangeTracker.Clear();
        var evenement = Assert.Single(Db.EvenementsExternes);
        Assert.Equal("Marché public", evenement.Titre);
        Assert.NotNull(Db.FluxExternes.Single(f => f.Id == id).DernierRafraichissementLe);
    }

    [Fact]
    public async Task Une_poussee_vide_fait_taire_le_flux_sans_le_perimer()
    {
        var id = await CreerFluxPousse();
        await OutilsFlux.PousserEvenementsFlux(Db, id, [
            new EvenementPousseDonnees("Marché", $"{DateOnly.FromDateTime(DateTime.Now.AddDays(1)):yyyy-MM-dd}"),
        ]);

        // « La ville n'annonce rien cette semaine » est une réponse, pas une panne :
        // les événements disparaissent et la réception reste fraîche.
        var resultat = (ResultatPoussee)await OutilsFlux.PousserEvenementsFlux(Db, id, []);

        Assert.Equal(new ResultatPoussee(0, 0, 0), resultat);
        Db.ChangeTracker.Clear();
        Assert.Empty(Db.EvenementsExternes);
        Assert.NotNull(Db.FluxExternes.Single(f => f.Id == id).DernierRafraichissementLe);
    }

    [Fact]
    public async Task Pousser_dans_un_abonnement_ics_est_refuse()
    {
        var flux = new FluxExterne
        {
            Id = Guid.NewGuid(), Nom = "Collectes", Url = "https://exemple.test/c.ics",
        };
        Db.FluxExternes.Add(flux);
        await Db.SaveChangesAsync();

        var erreur = await Assert.ThrowsAsync<McpException>(() => OutilsFlux.PousserEvenementsFlux(
            Db, flux.Id, [new EvenementPousseDonnees("Faux", "2026-09-25")]));

        Assert.Contains("abonnement iCal", erreur.Message);
    }

    [Fact]
    public async Task Une_date_illisible_le_dit_plutot_que_de_tout_remplacer()
    {
        var id = await CreerFluxPousse();

        var erreur = await Assert.ThrowsAsync<McpException>(() => OutilsFlux.PousserEvenementsFlux(
            Db, id, [new EvenementPousseDonnees("Marché", "le 25 septembre")]));

        Assert.Contains("YYYY-MM-DD", erreur.Message);
        Assert.Empty(Db.EvenementsExternes);
    }

    [Fact]
    public async Task Modifier_un_flux_pousse_avec_un_nom_vide_conserve_le_nom()
    {
        var id = await CreerFluxPousse("Ville de test");

        var dto = (FluxExterneDto)await OutilsFlux.GererFluxExterne(
            Db, SansReseau, "modifier", id, nom: "  ", type: "Autre");

        Assert.Equal("Ville de test", dto.Nom);
        Assert.Equal("Autre", dto.Type);
    }

    /// <summary>Parité avec REST : accepter l'URL en la jetant en silence laisserait
    /// l'agent croire qu'elle sert à quelque chose (revue de code, étape 6).</summary>
    [Fact]
    public async Task Poser_une_url_sur_un_flux_pousse_deja_cree_est_refuse()
    {
        var id = await CreerFluxPousse();

        var erreur = await Assert.ThrowsAsync<McpException>(() => OutilsFlux.GererFluxExterne(
            Db, SansReseau, "modifier", id, url: "https://exemple.test/c.ics"));

        Assert.Contains("pas d'URL", erreur.Message);
    }

    [Fact]
    public async Task La_source_ne_se_change_pas_apres_coup()
    {
        var id = await CreerFluxPousse();

        var erreur = await Assert.ThrowsAsync<McpException>(() => OutilsFlux.GererFluxExterne(
            Db, SansReseau, "modifier", id, source: "Ics"));

        Assert.Contains("ne se change pas", erreur.Message);
    }

    [Fact]
    public async Task Lister_montre_la_source_et_la_derniere_reception()
    {
        var id = await CreerFluxPousse();
        await OutilsFlux.PousserEvenementsFlux(Db, id, [
            new EvenementPousseDonnees("Marché", $"{DateOnly.FromDateTime(DateTime.Now.AddDays(1)):yyyy-MM-dd}"),
        ]);

        var liste = await OutilsFlux.ListerFluxExternes(Db);

        var flux = Assert.Single(liste);
        Assert.Equal("Poussee", flux.Source);
        Assert.Equal(1, flux.NbEvenements);
        Assert.NotNull(flux.DernierRafraichissementLe);
    }

    [Fact]
    public async Task Supprimer_un_flux_emporte_ses_evenements()
    {
        var id = await CreerFluxPousse();
        await OutilsFlux.PousserEvenementsFlux(Db, id, [
            new EvenementPousseDonnees("Marché", $"{DateOnly.FromDateTime(DateTime.Now.AddDays(1)):yyyy-MM-dd}"),
        ]);

        await OutilsFlux.GererFluxExterne(Db, SansReseau, "supprimer", id);

        Db.ChangeTracker.Clear();
        Assert.Empty(Db.FluxExternes);
        Assert.Empty(Db.EvenementsExternes);
    }
}
