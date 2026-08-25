using System.Net;
using System.Net.Http.Json;
using HouseOs.Api.Features.Taches;
using HouseOs.Api.Features.Zones;

namespace HouseOs.Tests.Integration;

[Collection("integration")]
public class ZonesApiTests(HouseOsFactory factory)
{
    private sealed record CorpsId(Guid Id);

    private static async Task<ZoneDto> CreerZone(HttpClient client, string nom, string? type = null, int? ordre = null)
    {
        var reponse = await client.PostAsJsonAsync("/api/zones", new { nom, type, ordre });
        Assert.Equal(HttpStatusCode.Created, reponse.StatusCode);
        return (await reponse.Content.ReadFromJsonAsync<ZoneDto>())!;
    }

    [Fact]
    public async Task SupprimerUneZone_LaisseLesTachesEtEquipements_AvecZoneIdNull()
    {
        var client = await factory.ClientConnecte();
        var zone = await CreerZone(client, $"Zone éphémère {Guid.NewGuid():N}");

        var tache = await client.PostAsJsonAsync("/api/taches", new CreerTacheRequete(
            "Tâche de la zone", null, null, null, zone.Id, null, null, null));
        var tacheId = (await tache.Content.ReadFromJsonAsync<CorpsId>())!.Id;
        var equipement = await client.PostAsJsonAsync("/api/equipements",
            new { nom = "Fournaise de la zone", zoneId = zone.Id });
        var equipementId = (await equipement.Content.ReadFromJsonAsync<CorpsId>())!.Id;

        // Le SetNull est déclaré au modèle et aux migrations, mais tout repose sur
        // Postgres : un Cascade accidentel détruirait tâches et archives.
        var suppression = await client.DeleteAsync($"/api/zones/{zone.Id}");
        Assert.Equal(HttpStatusCode.NoContent, suppression.StatusCode);

        var detailTache = await client.GetFromJsonAsync<TacheDto>($"/api/taches/{tacheId}");
        Assert.Null(detailTache!.ZoneId);
        var reponseEquipement = await client.GetAsync($"/api/equipements/{equipementId}");
        reponseEquipement.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task ModifierUneZone_SansOrdre_ConserveLOrdreExistant()
    {
        var client = await factory.ClientConnecte();
        var zone = await CreerZone(client, $"Zone ordonnée {Guid.NewGuid():N}", ordre: 7);

        // Même motif que le bug « édition sans perte » des tâches : un PUT partiel
        // ne doit pas remettre l'ordre à zéro.
        var put = await client.PutAsJsonAsync($"/api/zones/{zone.Id}",
            new { nom = zone.Nom, type = zone.Type });
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);

        var zones = await client.GetFromJsonAsync<List<ZoneDto>>("/api/zones");
        Assert.Equal(7, zones!.Single(z => z.Id == zone.Id).Ordre);
    }

    [Fact]
    public async Task LesZones_SortentTrieesParOrdrePuisNom()
    {
        var client = await factory.ClientConnecte();
        var marqueur = Guid.NewGuid().ToString("N")[..8];
        await CreerZone(client, $"B-{marqueur}", ordre: 2);
        await CreerZone(client, $"A-{marqueur}", ordre: 2);
        await CreerZone(client, $"C-{marqueur}", ordre: 1);

        var zones = await client.GetFromJsonAsync<List<ZoneDto>>("/api/zones");

        var notres = zones!.Where(z => z.Nom.EndsWith(marqueur)).Select(z => z.Nom[..1]).ToList();
        Assert.Equal(["C", "A", "B"], notres);
    }

    [Fact]
    public async Task CreerUneZone_AvecTypeNumerique_Repond400()
    {
        var client = await factory.ClientConnecte();

        // Enum.TryParse accepte « 999 » : sans garde, la chaîne « 999 » serait
        // persistée comme type et renvoyée au client.
        var reponse = await client.PostAsJsonAsync("/api/zones",
            new { nom = "Zone bizarre", type = "999" });

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
    }

    [Fact]
    public async Task CreerUneZone_AvecTypeEnMinuscules_EstAcceptee()
    {
        var client = await factory.ClientConnecte();

        var zone = await CreerZone(client, $"Cour {Guid.NewGuid():N}", type: "exterieur");

        Assert.Equal("Exterieur", zone.Type);
    }

    [Fact]
    public async Task CreerUneZone_AvecNomTropLong_Repond400()
    {
        var client = await factory.ClientConnecte();

        var reponse = await client.PostAsJsonAsync("/api/zones",
            new { nom = new string('z', 101) });

        // Sans validation, la colonne varchar(100) remonterait un 500.
        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
    }
}
