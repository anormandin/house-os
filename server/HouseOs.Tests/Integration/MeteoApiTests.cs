using HouseOs.Api.Features.Meteo;
using System.Net.Http.Json;

namespace HouseOs.Tests.Integration;

[Collection("integration")]
public class MeteoApiTests(HouseOsFactory factory)
{
    [Fact]
    public async Task TablesVides_RepondSansPlanter()
    {
        // Premier démarrage (aucune ingestion encore) : 200 avec un contenu creux —
        // le service d'ingestion est retiré du host de test, les tables sont vides.
        var client = await factory.ClientConnecte();

        var meteo = await client.GetFromJsonAsync<MeteoDto>("/api/meteo");

        Assert.NotNull(meteo);
        Assert.Null(meteo!.Maintenant);
        Assert.Empty(meteo.Jours);
        Assert.Empty(meteo.Verdicts);
    }
}
