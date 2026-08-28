using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using HouseOs.Api.Domaine;

namespace HouseOs.Tests.Domaine;

public class TransactionBancaireTests
{
    private static readonly DateOnly Date = new(2026, 8, 24);

    [Fact]
    public void Cle_independante_de_la_culture_du_serveur_et_montant_avec_point()
    {
        // La forme historique de la clé : montant formaté avec un point, quelle que
        // soit la locale de l'hôte (un réimport après déménagement doit dédupliquer).
        var attendu = "hash:" + Convert.ToHexStringLower(SHA256.HashData(
            Encoding.UTF8.GetBytes("2026-08-24|-84.12|CANADIAN TIRE")));

        var cultureInitiale = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-CA");
            var cle = TransactionBancaire.CalculerCleDedup(null, Date, -84.12m, "CANADIAN TIRE");
            Assert.Equal(attendu, cle);
        }
        finally
        {
            CultureInfo.CurrentCulture = cultureInitiale;
        }
    }

    [Fact]
    public void Fitid_court_reste_la_cle_brute()
    {
        Assert.Equal("fitid:2026082401",
            TransactionBancaire.CalculerCleDedup("2026082401", Date, -84.12m, "CANADIAN TIRE"));
    }

    [Fact]
    public void Fitid_trop_long_est_hache_sous_la_limite_de_colonne()
    {
        var fitidVerbeux = new string('X', 200); // la spec OFX permet 255 caractères

        var cle = TransactionBancaire.CalculerCleDedup(fitidVerbeux, Date, -84.12m, "CANADIAN TIRE");

        Assert.StartsWith("fitid-sha:", cle);
        Assert.True(cle.Length <= 100);
        // Déterministe : le même FITID redonne la même clé (réimport sans effet).
        Assert.Equal(cle, TransactionBancaire.CalculerCleDedup(fitidVerbeux, Date, -84.12m, "CANADIAN TIRE"));
    }

    [Fact]
    public void Numero_de_sequence_distingue_deux_transactions_identiques()
    {
        var sans = TransactionBancaire.CalculerCleDedup(null, Date, -60m, "PLEIN ESSENCE");
        var seq301 = TransactionBancaire.CalculerCleDedup(null, Date, -60m, "PLEIN ESSENCE", "301");
        var seq302 = TransactionBancaire.CalculerCleDedup(null, Date, -60m, "PLEIN ESSENCE", "302");

        Assert.NotEqual(sans, seq301);
        Assert.NotEqual(seq301, seq302);
    }

    [Fact]
    public void Occurrence_zero_garde_la_cle_historique_et_les_suivantes_divergent()
    {
        var historique = TransactionBancaire.CalculerCleDedup(null, Date, -60m, "PLEIN ESSENCE");
        var premiere = TransactionBancaire.CalculerCleDedup(null, Date, -60m, "PLEIN ESSENCE", null, 0);
        var seconde = TransactionBancaire.CalculerCleDedup(null, Date, -60m, "PLEIN ESSENCE", null, 1);

        Assert.Equal(historique, premiere);
        Assert.NotEqual(premiere, seconde);
    }
}
