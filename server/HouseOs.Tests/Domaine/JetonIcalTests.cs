using System.Text.Json;
using HouseOs.Api.Features.FluxIcal;
using Microsoft.Extensions.Configuration;

namespace HouseOs.Tests.Domaine;

/// <summary>
/// Le jeton iCal est la seule authentification du flux public : format, unicité,
/// et composition de l'URL publique (Funnel) selon la config.
/// </summary>
public class JetonIcalTests
{
    private static IConfiguration Config(string? basePublique) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ical:UrlPubliqueBase"] = basePublique,
            })
            .Build();

    private static JsonElement Reponse(string jeton, string? basePublique) =>
        JsonSerializer.SerializeToElement(JetonIcal.ReponseFlux(jeton, Config(basePublique)));

    [Fact]
    public void LeJeton_Fait48Hex_EtNeSeRepetePas()
    {
        var a = JetonIcal.Generer();
        var b = JetonIcal.Generer();

        Assert.Matches("^[0-9a-f]{48}$", a);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void AvecBasePublique_LUrlEstComposee_SansDoubleBarre()
    {
        var reponse = Reponse("abc123", "https://houseos.tailnet.ts.net/");

        Assert.Equal("/ical/abc123.ics", reponse.GetProperty("chemin").GetString());
        Assert.Equal("https://houseos.tailnet.ts.net/ical/abc123.ics",
            reponse.GetProperty("urlPublique").GetString());
    }

    [Fact]
    public void SansBasePublique_LUrlPubliqueEstNulle()
    {
        var reponse = Reponse("abc123", null);

        Assert.Equal(JsonValueKind.Null, reponse.GetProperty("urlPublique").ValueKind);
    }
}
