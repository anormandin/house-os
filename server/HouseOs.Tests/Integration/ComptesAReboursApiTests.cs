using System.Net;
using System.Net.Http.Json;
using HouseOs.Api.Features.ComptesARebours;

namespace HouseOs.Tests.Integration;

[Collection("integration")]
public class ComptesAReboursApiTests(HouseOsFactory factory)
{
    private static readonly DateOnly Aujourdhui = DateOnly.FromDateTime(DateTime.Now);

    [Fact]
    public async Task LaListe_InclutLesComptesPasses()
    {
        var client = await factory.ClientConnecte();
        var marqueur = Guid.NewGuid().ToString("N");
        var creation = await client.PostAsJsonAsync("/api/comptes-a-rebours",
            new { titre = $"Déjà passé {marqueur}", dateCible = Aujourdhui.AddDays(-10).ToString("yyyy-MM-dd") });
        Assert.Equal(HttpStatusCode.Created, creation.StatusCode);

        // Contrat serveur↔client : l'API retourne aussi les passés — c'est la carte
        // Aujourd'hui qui filtre côté client (célébration puis masquage).
        var comptes = await client.GetFromJsonAsync<List<CompteARebourDto>>("/api/comptes-a-rebours");

        Assert.Contains(comptes!, c => c.Titre == $"Déjà passé {marqueur}");
    }

    [Fact]
    public async Task CreerUnCompte_AvecTitreTropLong_Repond400()
    {
        var client = await factory.ClientConnecte();

        var reponse = await client.PostAsJsonAsync("/api/comptes-a-rebours",
            new { titre = new string('t', 201), dateCible = Aujourdhui.ToString("yyyy-MM-dd") });

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
    }

    [Fact]
    public async Task CreerUnCompte_AvecIconeNumerique_Repond400()
    {
        var client = await factory.ClientConnecte();

        var reponse = await client.PostAsJsonAsync("/api/comptes-a-rebours",
            new { titre = "Icône bizarre", dateCible = Aujourdhui.ToString("yyyy-MM-dd"), icone = "999" });

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
    }

    private static async Task<CompteARebourDto> CreerCompte(HttpClient client, object corps)
    {
        var reponse = await client.PostAsJsonAsync("/api/comptes-a-rebours", corps);
        Assert.Equal(HttpStatusCode.Created, reponse.StatusCode);
        return (await reponse.Content.ReadFromJsonAsync<CompteARebourDto>())!;
    }

    [Fact]
    public async Task ModifierUnCompte_RemplaceLaFiche()
    {
        var client = await factory.ClientConnecte();
        var compte = await CreerCompte(client, new
        {
            titre = $"Vacances {Guid.NewGuid():N}",
            dateCible = Aujourdhui.AddDays(30).ToString("yyyy-MM-dd"),
            icone = "Avion",
        });
        var nouveauTitre = $"Camping {Guid.NewGuid():N}";

        var put = await client.PutAsJsonAsync($"/api/comptes-a-rebours/{compte.Id}", new
        {
            titre = nouveauTitre,
            dateCible = Aujourdhui.AddDays(45).ToString("yyyy-MM-dd"),
            icone = "Tente",
        });
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);

        var comptes = await client.GetFromJsonAsync<List<CompteARebourDto>>("/api/comptes-a-rebours");
        var relu = comptes!.Single(c => c.Id == compte.Id);
        Assert.Equal(nouveauTitre, relu.Titre);
        Assert.Equal(Aujourdhui.AddDays(45), relu.DateCible);
        Assert.Equal("Tente", relu.Icone);
    }

    [Fact]
    public async Task ModifierUnCompte_SansIcone_ConserveLIcone()
    {
        var client = await factory.ClientConnecte();
        var compte = await CreerCompte(client, new
        {
            titre = $"Noël {Guid.NewGuid():N}",
            dateCible = "2026-12-25",
            icone = "Sapin",
        });

        // Corriger seulement la date ne doit pas repasser « Sapin » à Soleil (le
        // MCP conserve déjà — même sémantique des deux côtés).
        var put = await client.PutAsJsonAsync($"/api/comptes-a-rebours/{compte.Id}",
            new { titre = compte.Titre, dateCible = "2026-12-24" });
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);

        var comptes = await client.GetFromJsonAsync<List<CompteARebourDto>>("/api/comptes-a-rebours");
        Assert.Equal("Sapin", comptes!.Single(c => c.Id == compte.Id).Icone);
    }

    [Fact]
    public async Task ModifierUnCompte_AvecTitreTropLong_Repond400()
    {
        var client = await factory.ClientConnecte();
        var compte = await CreerCompte(client, new
        {
            titre = $"Limite {Guid.NewGuid():N}",
            dateCible = Aujourdhui.ToString("yyyy-MM-dd"),
        });

        var put = await client.PutAsJsonAsync($"/api/comptes-a-rebours/{compte.Id}",
            new { titre = new string('t', 201), dateCible = Aujourdhui.ToString("yyyy-MM-dd") });

        Assert.Equal(HttpStatusCode.BadRequest, put.StatusCode);
    }

    [Fact]
    public async Task SupprimerUnCompte_LeRetire_PuisRepond404()
    {
        var client = await factory.ClientConnecte();
        var compte = await CreerCompte(client, new
        {
            titre = $"Éphémère {Guid.NewGuid():N}",
            dateCible = Aujourdhui.ToString("yyyy-MM-dd"),
        });

        Assert.Equal(HttpStatusCode.NoContent,
            (await client.DeleteAsync($"/api/comptes-a-rebours/{compte.Id}")).StatusCode);

        var comptes = await client.GetFromJsonAsync<List<CompteARebourDto>>("/api/comptes-a-rebours");
        Assert.DoesNotContain(comptes!, c => c.Id == compte.Id);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.DeleteAsync($"/api/comptes-a-rebours/{compte.Id}")).StatusCode);
    }
}
