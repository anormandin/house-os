using HouseOs.Api.Domaine.Meteo;

namespace HouseOs.Tests.Domaine;

public class MeteoRemarquableTests
{
    private static readonly DateTime Maintenant = new(2026, 8, 22, 13, 0, 0);

    /// <summary>La journée courante, beau temps ordinaire (nuageux, 21 °C) par
    /// défaut ; chaque test dérègle l'heure ou le jour qu'il veut.</summary>
    private static ApercuMeteo Apercu(
        Action<PrevisionHoraire>? ajusterHeure = null,
        Action<PrevisionQuotidienne>? ajusterJour = null)
    {
        var heures = new List<PrevisionHoraire>();
        for (var i = 0; i < 24; i++)
        {
            var heure = new PrevisionHoraire
            {
                Heure = Maintenant.Date.AddHours(i),
                TemperatureC = 21,
                PrecipitationMm = 0,
                ProbabilitePrecipitationPct = 5,
                VentKmh = 10,
                RafalesKmh = 15,
                HumiditePct = 55,
                CouvertureNuageusePct = 60,
                CodeMeteo = 3,
            };
            ajusterHeure?.Invoke(heure);
            heures.Add(heure);
        }

        var jour = new PrevisionQuotidienne
        {
            Date = DateOnly.FromDateTime(Maintenant),
            TemperatureMinC = 12,
            TemperatureMaxC = 22,
            PrecipitationMm = 0,
            ProbabilitePrecipitationMaxPct = 10,
            VentMaxKmh = 15,
            RafalesMaxKmh = 25,
            CodeMeteo = 3,
        };
        ajusterJour?.Invoke(jour);
        return new ApercuMeteo(Maintenant, heures, [jour]);
    }

    [Fact]
    public void JourOrdinaire_RienARemarquer()
    {
        Assert.Null(MeteoRemarquable.Evaluer(Apercu()));
    }

    [Fact]
    public void SansDonnees_RienARemarquer()
    {
        Assert.Null(MeteoRemarquable.Evaluer(new ApercuMeteo(Maintenant, [], [])));
    }

    [Fact]
    public void Orage_Defavorable()
    {
        var signal = MeteoRemarquable.Evaluer(Apercu(h => h.CodeMeteo = h.Heure.Hour == 15 ? 95 : 3));
        Assert.NotNull(signal);
        Assert.False(signal.Favorable);
        Assert.Contains("orages", signal.Description);
    }

    [Fact]
    public void Neige_Defavorable()
    {
        var signal = MeteoRemarquable.Evaluer(Apercu(h => h.CodeMeteo = h.Heure.Hour == 9 ? 71 : 3));
        Assert.NotNull(signal);
        Assert.Contains("neige", signal.Description);
    }

    [Fact]
    public void BruineUneBonnePartieDuJour_Defavorable()
    {
        // 6 heures de bruine (code 51, 0 mm) entre 9 h et 14 h.
        var signal = MeteoRemarquable.Evaluer(
            Apercu(h => h.CodeMeteo = h.Heure.Hour >= 9 && h.Heure.Hour <= 14 ? 51 : 3));
        Assert.NotNull(signal);
        Assert.Contains("pluie", signal.Description);
    }

    [Fact]
    public void GrosCumulDePluie_Defavorable()
    {
        var signal = MeteoRemarquable.Evaluer(Apercu(ajusterJour: j => j.PrecipitationMm = 8));
        Assert.NotNull(signal);
        Assert.Contains("pluie", signal.Description);
    }

    [Fact]
    public void Canicule_Defavorable()
    {
        var signal = MeteoRemarquable.Evaluer(Apercu(ajusterJour: j => j.TemperatureMaxC = 32));
        Assert.NotNull(signal);
        Assert.Contains("chaleur", signal.Description);
    }

    [Fact]
    public void FroidMordant_Defavorable()
    {
        var signal = MeteoRemarquable.Evaluer(Apercu(
            h => h.TemperatureC = -20,
            j => { j.TemperatureMaxC = -18; j.TemperatureMinC = -25; }));
        Assert.NotNull(signal);
        Assert.Contains("froid", signal.Description);
    }

    [Fact]
    public void GrandVent_Defavorable()
    {
        var signal = MeteoRemarquable.Evaluer(Apercu(ajusterJour: j => j.RafalesMaxKmh = 70));
        Assert.NotNull(signal);
        Assert.Contains("vent", signal.Description);
    }

    [Fact]
    public void JourneeMagnifique_Favorable()
    {
        var signal = MeteoRemarquable.Evaluer(Apercu(
            h => { h.CodeMeteo = 1; h.TemperatureC = 23; },
            j => { j.CodeMeteo = 1; j.TemperatureMaxC = 24; }));
        Assert.NotNull(signal);
        Assert.True(signal.Favorable);
        Assert.Contains("magnifique", signal.Description);
    }
}
