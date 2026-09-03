using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HouseOs.Api.Features.Affichage;
using HouseOs.Tests.Features.Affichage;
using Microsoft.Extensions.DependencyInjection;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;

namespace HouseOs.Tests.Integration;

/// <summary>
/// Le protocole TRMNL vu de l'appareil (enrôlement, réveil, image, journal) et le
/// registre vu des humains. Le rendu est factice (HouseOsFactory) : ce qu'on vérifie,
/// c'est le contrat, pas Chromium.
/// </summary>
[Collection("integration")]
public class AffichageApiTests(HouseOsFactory factory)
{
    private static string MacAleatoire() =>
        string.Join(':', Enumerable.Range(0, 6).Select(_ => Random.Shared.Next(256).ToString("X2")));

    private static async Task<(string Mac, JsonElement Setup)> Enroler(HttpClient client, string? mac = null)
    {
        mac ??= MacAleatoire();
        var requete = new HttpRequestMessage(HttpMethod.Get, "/api/setup");
        requete.Headers.Add("ID", mac);
        var reponse = await client.SendAsync(requete);
        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        return (mac, await reponse.Content.ReadFromJsonAsync<JsonElement>());
    }

    private static HttpRequestMessage RequeteAppareil(HttpMethod methode, string chemin, string mac, string? cle,
        params (string Nom, string Valeur)[] entetes)
    {
        var requete = new HttpRequestMessage(methode, chemin);
        requete.Headers.Add("ID", mac);
        if (cle is not null)
        {
            requete.Headers.Add("Access-Token", cle);
        }
        foreach (var (nom, valeur) in entetes)
        {
            requete.Headers.Add(nom, valeur);
        }
        return requete;
    }

    [Fact]
    public async Task Setup_EnroleUnAppareilInconnu_EtRedonneLaMemeCleEnsuite()
    {
        var client = factory.CreateClient();

        var (mac, premier) = await Enroler(client);
        var (_, second) = await Enroler(client, mac);

        Assert.Equal(200, premier.GetProperty("status").GetInt32());
        var cle = premier.GetProperty("api_key").GetString();
        Assert.False(string.IsNullOrWhiteSpace(cle));
        Assert.Equal(OperationsAppareils.LongueurIdentifiant, premier.GetProperty("friendly_id").GetString()!.Length);
        Assert.Equal(cle, second.GetProperty("api_key").GetString());
        Assert.Equal("accueil", premier.GetProperty("filename").GetString());
        Assert.Contains("/api/affichage/", premier.GetProperty("image_url").GetString());
    }

    [Fact]
    public async Task Setup_SansId_Repond400()
    {
        var client = factory.CreateClient();

        var reponse = await client.GetAsync("/api/setup");

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
    }

    [Fact]
    public async Task Display_SansCleValide_Repond401()
    {
        var client = factory.CreateClient();
        var (mac, _) = await Enroler(client);

        var sansCle = await client.SendAsync(RequeteAppareil(HttpMethod.Get, "/api/display", mac, null));
        var mauvaiseCle = await client.SendAsync(RequeteAppareil(HttpMethod.Get, "/api/display", mac, "pas-la-bonne"));

        Assert.Equal(HttpStatusCode.Unauthorized, sansCle.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, mauvaiseCle.StatusCode);
    }

    [Fact]
    public async Task Display_RendALaTailleAnnoncee_EtLImageSigneeSeTelecharge()
    {
        var client = factory.CreateClient();
        var (mac, setup) = await Enroler(client);
        var cle = setup.GetProperty("api_key").GetString()!;
        var rendu = factory.Services.GetRequiredService<RenduEcranFictif>();

        var reponse = await client.SendAsync(RequeteAppareil(HttpMethod.Get, "/api/display", mac, cle,
            ("Width", "800"), ("Height", "480"), ("Battery-Voltage", "3.75"), ("RSSI", "-55"), ("FW-Version", "1.8.7")));

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        var corps = await reponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, corps.GetProperty("status").GetInt32());
        Assert.InRange(corps.GetProperty("refresh_rate").GetInt32(), 60, 3600);
        Assert.False(corps.GetProperty("update_firmware").GetBoolean());
        var fichier = corps.GetProperty("filename").GetString()!;
        Assert.Matches("^[0-9a-f]{16}$", fichier);
        // La taille vient des en-têtes, la pile est passée à la page.
        Assert.Equal((800, 480, 50), (rendu.DerniereDemande!.Largeur, rendu.DerniereDemande.Hauteur, rendu.DerniereDemande.Pile));

        var image = await client.GetAsync(new Uri(corps.GetProperty("image_url").GetString()!).PathAndQuery);
        Assert.Equal(HttpStatusCode.OK, image.StatusCode);
        Assert.Equal("image/png", image.Content.Headers.ContentType!.MediaType);
        var info = Image.Identify(await image.Content.ReadAsByteArrayAsync());
        Assert.Equal((800, 480), (info.Width, info.Height));
        Assert.Equal(PngBitDepth.Bit1, info.Metadata.GetPngMetadata().BitDepth);
    }

    [Fact]
    public async Task Image_AvecUnJetonFaux_Repond404()
    {
        var client = factory.CreateClient();
        var (mac, setup) = await Enroler(client);
        var cle = setup.GetProperty("api_key").GetString()!;
        var display = await client.SendAsync(RequeteAppareil(HttpMethod.Get, "/api/display", mac, cle));
        var corps = await display.Content.ReadFromJsonAsync<JsonElement>();
        var identifiant = setup.GetProperty("friendly_id").GetString();
        var fichier = corps.GetProperty("filename").GetString();

        var reponse = await client.GetAsync($"/api/affichage/{identifiant}/{new string('0', 32)}/{fichier}.png");

        Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
    }

    [Fact]
    public async Task Log_Authentifie_Repond204()
    {
        var client = factory.CreateClient();
        var (mac, setup) = await Enroler(client);
        var cle = setup.GetProperty("api_key").GetString()!;
        var requete = RequeteAppareil(HttpMethod.Post, "/api/log", mac, cle);
        requete.Content = JsonContent.Create(new
        {
            logs = new[] { new { level = "error", message = "Un test.", battery_voltage = 3.9, wake_reason = "timer" } },
        });

        var reponse = await client.SendAsync(requete);

        Assert.Equal(HttpStatusCode.NoContent, reponse.StatusCode);
    }

    [Fact]
    public async Task LesHumains_Voient_Renomment_EtRevoquent()
    {
        var anonyme = factory.CreateClient();
        var (mac, setup) = await Enroler(anonyme);
        var cle = setup.GetProperty("api_key").GetString()!;
        var identifiant = setup.GetProperty("friendly_id").GetString();
        var connecte = await factory.ClientConnecte();

        var liste = await connecte.GetFromJsonAsync<List<AppareilAffichageDto>>("/api/affichage/appareils");
        var appareil = Assert.Single(liste!, a => a.Identifiant == identifiant);
        Assert.Equal(mac, appareil.AdresseMac);

        var renomme = await connecte.PutAsJsonAsync($"/api/affichage/appareils/{appareil.Id}", new { nom = "Cuisine" });
        Assert.Equal(HttpStatusCode.OK, renomme.StatusCode);
        Assert.Equal("Cuisine", (await renomme.Content.ReadFromJsonAsync<AppareilAffichageDto>())!.Nom);

        var revoque = await connecte.DeleteAsync($"/api/affichage/appareils/{appareil.Id}");
        Assert.Equal(HttpStatusCode.NoContent, revoque.StatusCode);
        var apres = await anonyme.SendAsync(RequeteAppareil(HttpMethod.Get, "/api/display", mac, cle));
        Assert.Equal(HttpStatusCode.Unauthorized, apres.StatusCode);
    }

    [Fact]
    public async Task LeRegistre_ExigeLaSession()
    {
        var reponse = await factory.CreateClient().GetAsync("/api/affichage/appareils");

        Assert.Equal(HttpStatusCode.Unauthorized, reponse.StatusCode);
    }
}
