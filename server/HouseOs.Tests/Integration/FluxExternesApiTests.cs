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

    // ---- Flux poussés (vault : D-2026-09-20 Flux Externe Poussé) ----

    private static HttpClient ClientDePoussee(WebApplicationFactory<Program> hote, string? cle)
    {
        var client = hote.CreateClient();
        if (cle is not null)
        {
            client.DefaultRequestHeaders.Authorization = new("Bearer", cle);
        }
        return client;
    }

    private static async Task<FluxExterneDto> CreerFluxPousse(HttpClient client, string nom)
    {
        var creation = await client.PostAsJsonAsync(
            "/api/flux-externes", new FluxExterneRequete(nom, null, "Municipal", "Poussee"));
        Assert.Equal(HttpStatusCode.Created, creation.StatusCode);
        var cree = await creation.Content.ReadFromJsonAsync<FluxExterneDto>();
        // Rien n'est téléchargé, rien n'est amorcé : il attend le dehors.
        Assert.Null(cree!.Url);
        Assert.Equal("Poussee", cree.Source);
        Assert.Equal(0, cree.NbEvenements);
        Assert.Null(cree.DernierRafraichissementLe);
        return cree;
    }

    private static PousseeRequete Poussee(params (string Titre, DateOnly Date)[] evenements) =>
        new([.. evenements.Select(e => new EvenementPousseRequete(e.Titre, e.Date, null, null))]);

    [Fact]
    public async Task UneUrlSurUnFluxPousse_EstRefusee()
    {
        await using var hote = HoteAvecIcsFactice();
        var client = await Connecter(hote);

        var reponse = await client.PostAsJsonAsync("/api/flux-externes",
            new FluxExterneRequete("Ville", UrlSaine, "Municipal", "Poussee"));

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
        Assert.Contains("pas d'URL", await reponse.Content.ReadAsStringAsync());
    }

    /// <summary>
    /// `Enum.TryParse` accepte les chaînes numériques et les valeurs indéfinies : un
    /// `source: "5"` créait un flux <b>mort-vivant</b> — la passe de rafraîchissement
    /// l'ignore (ce n'est pas Ics), la poussée le refuse (ce n'est pas Poussee), et
    /// l'UI le montre comme un calendrier ordinaire. Trouvé en revue de code.
    /// </summary>
    [Theory]
    [InlineData("5")]
    [InlineData("42")]
    [InlineData("Tiree")]
    public async Task UneSourceQuiNExistePas_EstRefusee(string source)
    {
        await using var hote = HoteAvecIcsFactice();
        var client = await Connecter(hote);

        var reponse = await client.PostAsJsonAsync("/api/flux-externes",
            new FluxExterneRequete($"Zombie {Guid.NewGuid():N}", null, "Municipal", source));

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
        Assert.Contains("Source inconnue", await reponse.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task UnTypeNumerique_EstRefuse()
    {
        await using var hote = HoteAvecIcsFactice();
        var client = await Connecter(hote);

        var reponse = await client.PostAsJsonAsync("/api/flux-externes",
            new FluxExterneRequete("Zombie", UrlSaine, "3"));

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
        Assert.Contains("Type inconnu", await reponse.Content.ReadAsStringAsync());
    }

    /// <summary>La casse se tolère des deux côtés : le MCP l'acceptait, REST non.</summary>
    [Fact]
    public async Task LaSourceEtLeType_SeLisentEnMinuscules()
    {
        await using var hote = HoteAvecIcsFactice();
        var client = await Connecter(hote);

        var reponse = await client.PostAsJsonAsync("/api/flux-externes",
            new FluxExterneRequete($"Ville {Guid.NewGuid():N}", null, "municipal", "poussee"));

        Assert.Equal(HttpStatusCode.Created, reponse.StatusCode);
        var cree = await reponse.Content.ReadFromJsonAsync<FluxExterneDto>();
        Assert.Equal("Poussee", cree!.Source);
        Assert.Equal("Municipal", cree.Type);
    }

    /// <summary>
    /// Un client qui relit puis réécrit une fiche renvoie l'URL courante : s'il change
    /// aussi la source, c'est de ça qu'il faut lui parler, pas de l'URL.
    /// </summary>
    [Fact]
    public async Task ChangerLaSource_LeDitPlutotQueDeParlerDeLUrl()
    {
        await using var hote = HoteAvecIcsFactice();
        var client = await Connecter(hote);
        var creation = await client.PostAsJsonAsync("/api/flux-externes",
            new FluxExterneRequete($"Collectes {Guid.NewGuid():N}", UrlSaine, "Collecte"));
        var flux = await creation.Content.ReadFromJsonAsync<FluxExterneDto>();

        var reponse = await client.PutAsJsonAsync($"/api/flux-externes/{flux!.Id}",
            new FluxExterneRequete(flux.Nom, flux.Url, "Collecte", "Poussee"));

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
        Assert.Contains("ne se change pas", await reponse.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ModifierUnFluxPousse_SansRepeterSaSource_Fonctionne()
    {
        await using var hote = HoteAvecIcsFactice();
        var client = await Connecter(hote);
        var flux = await CreerFluxPousse(client, $"Ville {Guid.NewGuid():N}");

        var reponse = await client.PutAsJsonAsync($"/api/flux-externes/{flux.Id}",
            new FluxExterneRequete("Ville (conseil)", null, "Municipal"));

        Assert.Equal(HttpStatusCode.NoContent, reponse.StatusCode);
        var liste = await client.GetFromJsonAsync<List<FluxExterneDto>>("/api/flux-externes");
        Assert.Equal("Ville (conseil)", liste!.Single(f => f.Id == flux.Id).Nom);
    }

    [Fact]
    public async Task SansCle_LaPousseeEstRefusee()
    {
        await using var hote = HoteAvecIcsFactice();
        var session = await Connecter(hote);
        var flux = await CreerFluxPousse(session, $"Ville {Guid.NewGuid():N}");

        var reponse = await ClientDePoussee(hote, null).PostAsJsonAsync(
            $"/api/flux-externes/{flux.Id}/evenements",
            Poussee(("Séance du conseil", DateOnly.FromDateTime(DateTime.Now.AddDays(3)))));

        Assert.Equal(HttpStatusCode.Unauthorized, reponse.StatusCode);
    }

    /// <summary>
    /// La clé du MCP n'ouvre pas la poussée, et c'est le point de la décision : le
    /// programme qui pousse vit dehors et ne doit pas porter la clé qui ouvre la maison.
    /// </summary>
    [Fact]
    public async Task LaCleDuMcp_NOuvrePasLaPoussee()
    {
        await using var hote = HoteAvecIcsFactice();
        var session = await Connecter(hote);
        var flux = await CreerFluxPousse(session, $"Ville {Guid.NewGuid():N}");

        var reponse = await ClientDePoussee(hote, HouseOsFactory.CleMcp).PostAsJsonAsync(
            $"/api/flux-externes/{flux.Id}/evenements",
            Poussee(("Séance du conseil", DateOnly.FromDateTime(DateTime.Now.AddDays(3)))));

        Assert.Equal(HttpStatusCode.Unauthorized, reponse.StatusCode);
    }

    /// <summary>Le contrat de la poussée est celui du rafraîchissement ICS : un
    /// remplacement complet, pas un ajout.</summary>
    [Fact]
    public async Task PousserDeuxFois_RemplaceLesEvenements()
    {
        await using var hote = HoteAvecIcsFactice();
        var session = await Connecter(hote);
        var flux = await CreerFluxPousse(session, $"Ville {Guid.NewGuid():N}");
        var pousseur = ClientDePoussee(hote, HouseOsFactory.ClePoussee);
        var demain = DateOnly.FromDateTime(DateTime.Now.AddDays(1));

        var premiere = await pousseur.PostAsJsonAsync(
            $"/api/flux-externes/{flux.Id}/evenements",
            Poussee(("Séance du conseil", demain), ("Marché public", demain.AddDays(2))));
        Assert.Equal(HttpStatusCode.OK, premiere.StatusCode);
        Assert.Equal(new ResultatPoussee(2, 2, 0), await premiere.Content.ReadFromJsonAsync<ResultatPoussee>());

        // La seconde poussée n'annonce plus qu'un événement : l'autre doit disparaître.
        var seconde = await pousseur.PostAsJsonAsync(
            $"/api/flux-externes/{flux.Id}/evenements", Poussee(("Marché public", demain.AddDays(2))));
        Assert.Equal(HttpStatusCode.OK, seconde.StatusCode);

        var evenements = await session.GetFromJsonAsync<List<EvenementExterneDto>>(
            "/api/evenements-externes?jours=7");
        Assert.DoesNotContain(evenements!, e => e.Titre == "Séance du conseil");
        Assert.Contains(evenements!, e => e.Titre == "Marché public");

        var liste = await session.GetFromJsonAsync<List<FluxExterneDto>>("/api/flux-externes");
        var relu = liste!.Single(f => f.Id == flux.Id);
        Assert.Equal(1, relu.NbEvenements);
        // L'horodatage de réception est ce que l'UI montre, et ce sur quoi le fonds de
        // tiroir juge qu'un flux est périmé.
        Assert.NotNull(relu.DernierRafraichissementLe);
    }

    [Fact]
    public async Task PousserDansUnAbonnementIcs_EstRefuse()
    {
        await using var hote = HoteAvecIcsFactice();
        var session = await Connecter(hote);
        var creation = await session.PostAsJsonAsync("/api/flux-externes",
            new FluxExterneRequete($"Collectes {Guid.NewGuid():N}", UrlSaine, "Collecte"));
        var abonnement = await creation.Content.ReadFromJsonAsync<FluxExterneDto>();

        var reponse = await ClientDePoussee(hote, HouseOsFactory.ClePoussee).PostAsJsonAsync(
            $"/api/flux-externes/{abonnement!.Id}/evenements",
            Poussee(("Faux", DateOnly.FromDateTime(DateTime.Now.AddDays(1)))));

        // Accepter ici viderait le flux à la prochaine passe de six heures.
        Assert.Equal(HttpStatusCode.UnprocessableEntity, reponse.StatusCode);
        Assert.Contains("abonnement iCal", await reponse.Content.ReadAsStringAsync());
        var liste = await session.GetFromJsonAsync<List<FluxExterneDto>>("/api/flux-externes");
        Assert.Equal(1, liste!.Single(f => f.Id == abonnement.Id).NbEvenements);
    }

    [Fact]
    public async Task LesEvenementsHorsFenetre_SontEcartesSansFaireEchouerLaPoussee()
    {
        await using var hote = HoteAvecIcsFactice();
        var session = await Connecter(hote);
        var flux = await CreerFluxPousse(session, $"Ville {Guid.NewGuid():N}");
        var aujourdhui = DateOnly.FromDateTime(DateTime.Now);

        var reponse = await ClientDePoussee(hote, HouseOsFactory.ClePoussee).PostAsJsonAsync(
            $"/api/flux-externes/{flux.Id}/evenements",
            Poussee(
                ("Dans la fenêtre", aujourdhui.AddDays(3)),
                ("L'an prochain", aujourdhui.AddDays(400)),
                ("L'an dernier", aujourdhui.AddDays(-400))));

        // Une ville qui publie son année entière ne doit pas voir sa poussée rejetée.
        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.Equal(new ResultatPoussee(3, 1, 2), await reponse.Content.ReadFromJsonAsync<ResultatPoussee>());
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
