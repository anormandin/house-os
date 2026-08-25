using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using HouseOs.Api.Features.Documents;
using HouseOs.Api.Features.Equipements;
using HouseOs.Api.Features.Taches;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace HouseOs.Tests.Integration;

[Collection("integration")]
public class EquipementsApiTests(HouseOsFactory factory)
{
    private sealed record CorpsId(Guid Id);

    private static async Task<Guid> CreerEquipement(HttpClient client, object corps)
    {
        var reponse = await client.PostAsJsonAsync("/api/equipements", corps);
        Assert.Equal(HttpStatusCode.Created, reponse.StatusCode);
        return (await reponse.Content.ReadFromJsonAsync<CorpsId>())!.Id;
    }

    [Fact]
    public async Task SupprimerUnEquipement_DelieSesDocuments_SansEffacerLesFichiers()
    {
        var client = await factory.ClientConnecte();
        var equipementId = await CreerEquipement(client, new { nom = $"Fournaise {Guid.NewGuid():N}" });

        using var image = new Image<Rgba32>(4, 4);
        using var memoire = new MemoryStream();
        image.SaveAsPng(memoire);
        var fichier = new ByteArrayContent(memoire.ToArray());
        fichier.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
        var creation = await client.PostAsync("/api/documents", new MultipartFormDataContent
        {
            { fichier, "fichier", "manuel.png" },
            { new StringContent(equipementId.ToString()), "equipementId" },
        });
        var documentId = (await creation.Content.ReadFromJsonAsync<CorpsId>())!.Id;

        var suppression = await client.DeleteAsync($"/api/equipements/{equipementId}");
        Assert.Equal(HttpStatusCode.NoContent, suppression.StatusCode);

        // Le document survit (SET NULL réel côté Postgres), délié, fichier intact.
        var documents = await client.GetFromJsonAsync<List<DocumentDto>>("/api/documents");
        var document = Assert.Single(documents!, d => d.Id == documentId);
        Assert.Null(document.EquipementId);
        Assert.True(File.Exists(Path.Combine(factory.DossierFichiers, documentId.ToString("N") + ".png")));
    }

    [Fact]
    public async Task LHistoriqueDEntretien_PlafonneAuxVingtPlusRecentes()
    {
        var client = await factory.ClientConnecte();
        var equipementId = await CreerEquipement(client, new { nom = $"Tondeuse {Guid.NewGuid():N}" });

        for (var i = 0; i < 21; i++)
        {
            var tache = await client.PostAsJsonAsync("/api/taches", new CreerTacheRequete(
                $"Entretien {i}", null, DateOnly.FromDateTime(DateTime.Now),
                null, null, equipementId, null, null));
            var tacheId = (await tache.Content.ReadFromJsonAsync<CorpsId>())!.Id;
            var occurrences = await client.GetFromJsonAsync<List<OccurrenceDto>>(
                "/api/occurrences?filtre=en-attente");
            var occurrence = occurrences!.Single(o => o.TacheId == tacheId);
            (await client.PostAsync($"/api/occurrences/{occurrence.Id}/completer", null))
                .EnsureSuccessStatusCode();
        }

        var detail = await client.GetFromJsonAsync<EquipementDetailDto>($"/api/equipements/{equipementId}");

        Assert.Equal(20, detail!.Entretiens.Count);
        // Des plus récentes aux plus anciennes : la toute première (Entretien 0) tombe.
        Assert.DoesNotContain(detail.Entretiens, e => e.TitreTache == "Entretien 0");
        Assert.True(detail.Entretiens.Zip(detail.Entretiens.Skip(1))
            .All(paire => paire.First.CompleteeLe >= paire.Second.CompleteeLe));
    }

    [Fact]
    public async Task CreerAvecZoneInconnue_Repond400()
    {
        var client = await factory.ClientConnecte();

        var reponse = await client.PostAsJsonAsync("/api/equipements",
            new { nom = "Orphelin", zoneId = Guid.NewGuid() });

        // Sans validation, la violation de FK remonterait un 500.
        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
    }

    [Fact]
    public async Task SpecsAvecCaractereNul_SontRejeteesProprement()
    {
        var client = await factory.ClientConnecte();

        // Postgres refuse le caractère nul dans un jsonb (22P05) : 400, jamais 500. Ce cas est
        // intestable en Sqlite (specs remappées en TEXT) — d'où l'intégration.
        var reponse = await client.PostAsJsonAsync("/api/equipements",
            new { nom = "Piégé", specs = new Dictionary<string, string> { ["filtre"] = "16\0x25" } });

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
    }

    [Fact]
    public async Task CreerAvecNomTropLong_Repond400()
    {
        var client = await factory.ClientConnecte();

        var reponse = await client.PostAsJsonAsync("/api/equipements",
            new { nom = new string('e', 201) });

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
    }
}
