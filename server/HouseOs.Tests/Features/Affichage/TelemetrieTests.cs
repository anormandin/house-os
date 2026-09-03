using HouseOs.Api.Domaine;
using HouseOs.Api.Features.Affichage;
using Microsoft.AspNetCore.Http;

namespace HouseOs.Tests.Features.Affichage;

public class TelemetrieTests
{
    [Fact]
    public void Les_entetes_du_firmware_sont_lus_en_culture_invariante()
    {
        var appareil = new AppareilAffichage { AdresseMac = "AA", Identifiant = "ABCDEF", Cle = "k" };
        var entetes = new HeaderDictionary
        {
            ["Width"] = "1872", ["Height"] = "1404", ["Battery-Voltage"] = "3.94",
            ["RSSI"] = "-61", ["FW-Version"] = "1.8.7", ["Model"] = "reterminal_e1003",
        };
        var instant = new DateTimeOffset(2026, 9, 3, 14, 30, 0, TimeSpan.FromHours(-4));

        Telemetrie.Appliquer(appareil, entetes, instant);

        Assert.Equal(1872, appareil.Largeur);
        Assert.Equal(1404, appareil.Hauteur);
        Assert.Equal(3.94, appareil.TensionPile);
        Assert.Equal(-61, appareil.Rssi);
        Assert.Equal("1.8.7", appareil.VersionFirmware);
        Assert.Equal("reterminal_e1003", appareil.Modele);
        Assert.Equal(instant, appareil.DernierContact);
    }

    [Fact]
    public void Un_entete_absent_ou_illisible_ne_touche_pas_la_valeur_connue()
    {
        var appareil = new AppareilAffichage
        {
            AdresseMac = "AA", Identifiant = "ABCDEF", Cle = "k", Largeur = 800, TensionPile = 4.0,
        };

        Telemetrie.Appliquer(appareil, new HeaderDictionary { ["Width"] = "large", ["Battery-Voltage"] = "" },
            DateTimeOffset.UtcNow);

        Assert.Equal(800, appareil.Largeur);
        Assert.Equal(4.0, appareil.TensionPile);
    }

    [Theory]
    [InlineData(4.2, 100)]
    [InlineData(4.5, 100)]
    [InlineData(3.75, 50)]
    [InlineData(3.3, 0)]
    [InlineData(3.0, 0)]
    public void La_pile_en_pourcent_est_lineaire_et_bornee(double tension, int attendu)
    {
        Assert.Equal(attendu, Telemetrie.PileEnPourcent(tension));
        Assert.Null(Telemetrie.PileEnPourcent(null));
    }
}
