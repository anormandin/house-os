using System.Net;
using System.Net.Http.Json;
using HouseOs.Api.Features.Auth;
using HouseOs.Api.Features.FluxExternes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HouseOs.Tests.Integration;

/// <summary>
/// La gestion des flux externes de bout en bout — avec un HTTP factice derrière la
/// vraie garde SSRF (hôtes en IP littérale publique : aucun DNS, aucun réseau) :
/// CRUD, validation d'URL, flux mort refusé à la création, adresse interne refusée.
/// </summary>
[Collection("integration")]
public class FluxExternesApiTests(HouseOsFactory factory)
{
    private const string UrlSaine = "http://203.0.113.10/collectes.ics";
    private const string UrlMorte = "http://203.0.113.10/mort.ics";

    private static readonly string IcsValide = $"""
        BEGIN:VCALENDAR
        VERSION:2.0
        PRODID:-//test//FR
        BEGIN:VEVENT
        UID:frais@test
        DTSTART;VALUE=DATE:{DateTime.Now.AddDays(1):yyyyMMdd}
        SUMMARY:Collecte
        END:VEVENT
        END:VCALENDAR
        """;

    private sealed class RouteurIcs : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage requete, CancellationToken ct) =>
            Task.FromResult(requete.RequestUri!.AbsolutePath == "/mort.ics"
                ? new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("") }
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(IcsValide) });
    }

    private sealed class FabriqueFactice : IHttpClientFactory
    {
        // La garde SSRF réelle, un fil HTTP factice : le test exerce le vrai chemin.
        public HttpClient CreateClient(string name) =>
            new(new GardeSsrfHandler { InnerHandler = new RouteurIcs() });
    }

    /// <summary>Hôte dérivé où tout téléchargement ICS passe par le routeur factice.</summary>
    private WebApplicationFactory<Program> HoteAvecIcsFactice() =>
        factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddSingleton<IHttpClientFactory>(new FabriqueFactice())));

    private static async Task<HttpClient> Connecter(WebApplicationFactory<Program> hote)
    {
        var client = hote.CreateClient();
        var connexion = await client.PostAsJsonAsync(
            "/api/auth/connexion", new ConnexionRequete("alain", "test-alain"));
        connexion.EnsureSuccessStatusCode();
        return client;
    }

    [Fact]
    public async Task SansSession_LaGestionDesFluxEstFermee()
    {
        var reponse = await factory.CreateClient().PostAsJsonAsync(
            "/api/flux-externes", new FluxExterneRequete("Collectes", UrlSaine, null));

        Assert.Equal(HttpStatusCode.Unauthorized, reponse.StatusCode);
    }

    [Fact]
    public async Task CreerListerModifierSupprimer_FontLAllerRetour()
    {
        await using var hote = HoteAvecIcsFactice();
        var client = await Connecter(hote);
        var nom = $"Collectes {Guid.NewGuid():N}";

        var creation = await client.PostAsJsonAsync(
            "/api/flux-externes", new FluxExterneRequete(nom, UrlSaine, "Collecte"));
        Assert.Equal(HttpStatusCode.Created, creation.StatusCode);
        var cree = await creation.Content.ReadFromJsonAsync<FluxExterneDto>();
        Assert.Equal(1, cree!.NbEvenements); // la création amorce les événements
        Assert.Null(cree.DerniereErreur);

        var liste = await client.GetFromJsonAsync<List<FluxExterneDto>>("/api/flux-externes");
        Assert.Contains(liste!, f => f.Id == cree.Id && f.Nom == nom);

        var modification = await client.PutAsJsonAsync($"/api/flux-externes/{cree.Id}",
            new FluxExterneRequete($"{nom} (ville)", UrlSaine, "Collecte"));
        Assert.Equal(HttpStatusCode.NoContent, modification.StatusCode);

        var suppression = await client.DeleteAsync($"/api/flux-externes/{cree.Id}");
        Assert.Equal(HttpStatusCode.NoContent, suppression.StatusCode);
        var relu = await client.GetFromJsonAsync<List<FluxExterneDto>>("/api/flux-externes");
        Assert.DoesNotContain(relu!, f => f.Id == cree.Id);
    }

    [Fact]
    public async Task UrlInvalide_Repond400()
    {
        await using var hote = HoteAvecIcsFactice();
        var client = await Connecter(hote);

        var reponse = await client.PostAsJsonAsync(
            "/api/flux-externes", new FluxExterneRequete("Cassé", "pas-une-url", null));

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
        Assert.Contains("http", await reponse.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task FluxIllisible_Repond422_SansGarderLAbonnementMort()
    {
        await using var hote = HoteAvecIcsFactice();
        var client = await Connecter(hote);
        var nom = $"Mort {Guid.NewGuid():N}";

        var reponse = await client.PostAsJsonAsync(
            "/api/flux-externes", new FluxExterneRequete(nom, UrlMorte, null));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, reponse.StatusCode);
        Assert.Contains("Impossible de lire", await reponse.Content.ReadAsStringAsync());
        var liste = await client.GetFromJsonAsync<List<FluxExterneDto>>("/api/flux-externes");
        Assert.DoesNotContain(liste!, f => f.Nom == nom);
    }

    [Fact]
    public async Task AdresseInterne_RefuseeAvecUnMessageClair()
    {
        await using var hote = HoteAvecIcsFactice();
        var client = await Connecter(hote);

        var reponse = await client.PostAsJsonAsync("/api/flux-externes",
            new FluxExterneRequete("Sonde LAN", "http://192.168.4.1/admin.ics", null));

        // La garde SSRF court-circuite avant tout appel : 422, message explicite.
        Assert.Equal(HttpStatusCode.UnprocessableEntity, reponse.StatusCode);
        Assert.Contains("réseau interne", await reponse.Content.ReadAsStringAsync());
    }
}
