using System.Net.Http.Json;

namespace HouseOs.Tests.Integration;

/// <summary>
/// /api/sante sonde réellement la base (healthcheck compose) et le middleware
/// d'en-têtes est branché — vérifié ici parce que la route est anonyme et simple.
/// </summary>
[Collection("integration")]
public class SanteApiTests(HouseOsFactory factory)
{
    private sealed record Sante(string Statut, string Db);

    [Fact]
    public async Task Sante_AvecLaBaseEnVie_Repond200_AvecStatutEtDb()
    {
        var client = factory.CreateClient();

        var reponse = await client.GetAsync("/api/sante");

        reponse.EnsureSuccessStatusCode();
        var sante = await reponse.Content.ReadFromJsonAsync<Sante>();
        Assert.Equal("ok", sante!.Statut);
        Assert.Equal("ok", sante.Db);
    }

    [Fact]
    public async Task ToutesLesReponses_PortentXContentTypeOptionsNosniff()
    {
        var client = factory.CreateClient();

        var reponse = await client.GetAsync("/api/sante");

        Assert.Equal("nosniff", Assert.Single(reponse.Headers.GetValues("X-Content-Type-Options")));
    }
}
