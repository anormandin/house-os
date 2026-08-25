using HouseOs.Api.Domaine.Humeur;
using HouseOs.Api.Features.Humeur;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HouseOs.Tests.Features.Humeur;

/// <summary>
/// Bornes des créneaux matin (5 h 30) / soir (17 h) et calcul du prochain réveil.
/// Les instants sont construits en heure murale locale pour rester déterministes
/// quel que soit le fuseau de la machine.
/// </summary>
public class HumeurServiceTests
{
    private static readonly HumeurService Service = new(
        null!, Options.Create(new HumeurOptions()), NullLogger<HumeurService>.Instance);

    private static DateTimeOffset Local(int annee, int mois, int jour, int heure, int minute, int seconde = 0)
    {
        var mural = new DateTime(annee, mois, jour, heure, minute, seconde);
        return new DateTimeOffset(mural, TimeZoneInfo.Local.GetUtcOffset(mural));
    }

    [Theory]
    [InlineData(5, 29, 59, 14, MomentJournee.Soir)]  // 05:29:59 → soir d'HIER
    [InlineData(5, 30, 0, 15, MomentJournee.Matin)]  // 05:30:00 pile → matin d'aujourd'hui
    [InlineData(16, 59, 59, 15, MomentJournee.Matin)]
    [InlineData(17, 0, 0, 15, MomentJournee.Soir)]   // 17:00:00 pile → soir
    [InlineData(0, 0, 0, 14, MomentJournee.Soir)]    // minuit → soir de la veille
    [InlineData(23, 59, 59, 15, MomentJournee.Soir)]
    public void CreneauCourant_Bornes(int heure, int minute, int seconde, int jourAttendu, MomentJournee momentAttendu)
    {
        var (date, moment) = Service.CreneauCourant(Local(2026, 9, 15, heure, minute, seconde));

        Assert.Equal(new DateOnly(2026, 9, jourAttendu), date);
        Assert.Equal(momentAttendu, moment);
    }

    [Fact]
    public void CreneauCourant_MinuitDuPremierJanvier_TraverseLAnnee()
    {
        var (date, moment) = Service.CreneauCourant(Local(2027, 1, 1, 0, 30));

        Assert.Equal(new DateOnly(2026, 12, 31), date);
        Assert.Equal(MomentJournee.Soir, moment);
    }

    [Theory]
    [InlineData(3, 0)]   // avant le matin → réveil à 5 h 31
    [InlineData(10, 0)]  // entre les deux → réveil à 17 h 01
    [InlineData(22, 0)]  // après le soir → réveil au matin du lendemain
    public void ProchainCreneau_EstToujoursStrictementFutur(int heure, int minute)
    {
        var maintenant = Local(2026, 9, 15, heure, minute);

        var prochain = Service.ProchainCreneau(maintenant);

        Assert.True(prochain > maintenant);
        Assert.True(prochain - maintenant <= TimeSpan.FromHours(25));
    }

    [Fact]
    public void ProchainCreneau_ViseLeCreneauSuivantAvecUneMinuteDeMarge()
    {
        var prochain = Service.ProchainCreneau(Local(2026, 9, 15, 10, 0));

        Assert.Equal(17, prochain.LocalDateTime.Hour);
        Assert.Equal(1, prochain.LocalDateTime.Minute);
    }
}
