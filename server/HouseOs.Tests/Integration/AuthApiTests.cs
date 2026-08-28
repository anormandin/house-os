using System.Net;
using System.Net.Http.Json;
using HouseOs.Api.Features.Auth;
using Microsoft.AspNetCore.Hosting;

namespace HouseOs.Tests.Integration;

[Collection("integration")]
public class AuthApiTests(HouseOsFactory factory)
{
    [Fact]
    public async Task Connexion_AvecCorpsIncomplet_Repond400()
    {
        var client = factory.CreateClient();

        // {} : le binding laisse les membres à null malgré les types non-nullables —
        // sans garde, c'était une NullReferenceException sur un endpoint anonyme.
        var vide = await client.PostAsJsonAsync("/api/auth/connexion", new { });
        var sansMotDePasse = await client.PostAsJsonAsync(
            "/api/auth/connexion", new { nomUtilisateur = "alain" });

        Assert.Equal(HttpStatusCode.BadRequest, vide.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, sansMotDePasse.StatusCode);
    }

    [Fact]
    public async Task Connexion_MauvaisMotDePasse_Repond401_SansPoserDeCookie()
    {
        var client = factory.CreateClient();

        var reponse = await client.PostAsJsonAsync(
            "/api/auth/connexion", new ConnexionRequete("alain", "pas-le-bon"));

        Assert.Equal(HttpStatusCode.Unauthorized, reponse.StatusCode);
        Assert.False(reponse.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task Connexion_CompteInconnu_Repond401()
    {
        var client = factory.CreateClient();

        var reponse = await client.PostAsJsonAsync(
            "/api/auth/connexion", new ConnexionRequete("gertrude", "test-gertrude"));

        Assert.Equal(HttpStatusCode.Unauthorized, reponse.StatusCode);
    }

    [Fact]
    public async Task Connexion_NormaliseCasseEtEspaces()
    {
        var client = factory.CreateClient();

        var reponse = await client.PostAsJsonAsync(
            "/api/auth/connexion", new ConnexionRequete("  ALAIN  ", "test-alain"));

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        var moi = await client.GetFromJsonAsync<UtilisateurDto>("/api/auth/moi");
        Assert.Equal("alain", moi!.NomUtilisateur);
    }

    [Fact]
    public async Task Connexion_PoseUnCookieHttpOnlySameSiteLax()
    {
        var client = factory.CreateClient();

        var reponse = await client.PostAsJsonAsync(
            "/api/auth/connexion", new ConnexionRequete("alain", "test-alain"));

        // Les mutations JSON ne sont protégées que par SameSite : à figer.
        var cookie = Assert.Single(reponse.Headers.GetValues("Set-Cookie"),
            c => c.StartsWith("houseos_session="));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Connexion_SixiemeTentativeDansLaMinute_Repond429()
    {
        // Hôte dérivé avec la vraie limite (5/min) : la factory partagée la monte
        // très haut pour que le reste de la suite se connecte librement.
        await using var hote = factory.WithWebHostBuilder(builder =>
            builder.UseSetting("Auth:LimiteConnexion:Tentatives", "5"));
        var client = hote.CreateClient();

        for (var tentative = 1; tentative <= 5; tentative++)
        {
            var refus = await client.PostAsJsonAsync(
                "/api/auth/connexion", new ConnexionRequete("alain", "pas-le-bon"));
            Assert.Equal(HttpStatusCode.Unauthorized, refus.StatusCode);
        }

        var bloquee = await client.PostAsJsonAsync(
            "/api/auth/connexion", new ConnexionRequete("alain", "pas-le-bon"));

        Assert.Equal(HttpStatusCode.TooManyRequests, bloquee.StatusCode);
        Assert.Contains("Trop de tentatives", await bloquee.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Moi_AvecCookieAltere_Repond401()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", "houseos_session=falsifie-par-le-chat");

        var reponse = await client.GetAsync("/api/auth/moi");

        Assert.Equal(HttpStatusCode.Unauthorized, reponse.StatusCode);
    }

    [Fact]
    public async Task Deconnexion_InvalideLaSession()
    {
        var client = await factory.ClientConnecte();

        (await client.PostAsync("/api/auth/deconnexion", null)).EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/moi")).StatusCode);
    }
}
