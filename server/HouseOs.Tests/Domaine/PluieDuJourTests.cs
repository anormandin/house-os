using HouseOs.Api.Domaine.Meteo;

namespace HouseOs.Tests.Domaine;

public class PluieDuJourTests
{
    private static readonly DateTime Maintenant = new(2026, 8, 28, 10, 15, 0);

    private static PrevisionHoraire Heure(int heure, int probabilite) => new()
    {
        Heure = Maintenant.Date.AddHours(heure),
        ProbabilitePrecipitationPct = probabilite,
    };

    [Fact]
    public void LaNuitPasseeNeComptePas()
    {
        // Le cas de l'issue #58 : 66 % à minuit (passé), reste de la journée à 18 %
        // max — la carte doit annoncer 18 %, pas 66 %.
        var heures = new List<PrevisionHoraire>
        {
            Heure(0, 66), Heure(1, 60), Heure(9, 10), Heure(10, 18), Heure(14, 5),
        };
        Assert.Equal(18, PluieDuJour.ProbabiliteRestantePct(new ApercuMeteo(Maintenant, heures, [])));
    }

    [Fact]
    public void LHeureEnCoursCompte()
    {
        // À 10 h 15, la ligne de 10 h décrit le moment présent.
        var heures = new List<PrevisionHoraire> { Heure(10, 75), Heure(11, 5) };
        Assert.Equal(75, PluieDuJour.ProbabiliteRestantePct(new ApercuMeteo(Maintenant, heures, [])));
    }

    [Fact]
    public void PluieAVenirEnFinDeJournee_Comptee()
    {
        var heures = new List<PrevisionHoraire> { Heure(11, 10), Heure(17, 80) };
        Assert.Equal(80, PluieDuJour.ProbabiliteRestantePct(new ApercuMeteo(Maintenant, heures, [])));
    }

    [Fact]
    public void SansHeuresRestantes_Null()
    {
        // Seulement des heures passées ou d'un autre jour : repli sur l'agrégat du jour.
        var heures = new List<PrevisionHoraire> { Heure(0, 66), Heure(-2, 40) };
        Assert.Null(PluieDuJour.ProbabiliteRestantePct(new ApercuMeteo(Maintenant, heures, [])));
    }
}
