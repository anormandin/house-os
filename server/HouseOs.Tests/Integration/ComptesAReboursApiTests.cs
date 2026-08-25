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
}
