using HouseOs.Api.Domaine.Meteo;

namespace HouseOs.Tests.Domaine;

public class ReglesJourneeTests
{
    // Un samedi 13 h : plein cœur de la fenêtre de jour des règles.
    private static readonly DateTime Maintenant = new(2026, 8, 22, 13, 0, 0);

    /// <summary>48 h d'heures autour de Maintenant (veille 00 h → lendemain 00 h),
    /// beau temps sec par défaut ; chaque test dérègle ce qu'il veut.</summary>
    private static List<PrevisionHoraire> BellesHeures(Action<PrevisionHoraire>? ajuster = null)
    {
        var debut = Maintenant.Date.AddDays(-1);
        var heures = new List<PrevisionHoraire>();
        for (var i = 0; i < 49; i++)
        {
            var heure = new PrevisionHoraire
            {
                Heure = debut.AddHours(i),
                TemperatureC = 21,
                PrecipitationMm = 0,
                ProbabilitePrecipitationPct = 5,
                VentKmh = 10,
                RafalesKmh = 15,
                HumiditePct = 55,
                HumiditeSol = 0.2,
                CouvertureNuageusePct = 30,
                IndiceUv = 4,
            };
            ajuster?.Invoke(heure);
            heures.Add(heure);
        }
        return heures;
    }

    private static ApercuMeteo Apercu(List<PrevisionHoraire> heures) => new(Maintenant, heures, []);

    // --- Tonte ---

    [Fact]
    public void Tonte_BeauTempsSec_Bon()
    {
        var verdict = new RegleTonte().Evaluer(Apercu(BellesHeures()));
        Assert.Equal(EtatVerdict.Bon, verdict.Etat);
    }

    [Fact]
    public void Tonte_PluieRecente_Defavorable()
    {
        var heures = BellesHeures(h =>
        {
            if (h.Heure == Maintenant.AddHours(-6))
            {
                h.PrecipitationMm = 2;
            }
        });
        var verdict = new RegleTonte().Evaluer(Apercu(heures));
        Assert.Equal(EtatVerdict.Defavorable, verdict.Etat);
        Assert.Contains("plu", verdict.Raison);
    }

    [Fact]
    public void Tonte_PluieProbableBientot_Defavorable()
    {
        var heures = BellesHeures(h =>
        {
            if (h.Heure == Maintenant.AddHours(2))
            {
                h.ProbabilitePrecipitationPct = 60;
            }
        });
        var verdict = new RegleTonte().Evaluer(Apercu(heures));
        Assert.Equal(EtatVerdict.Defavorable, verdict.Etat);
    }

    [Fact]
    public void Tonte_TropFroid_Defavorable()
    {
        var heures = BellesHeures(h => h.TemperatureC = 7);
        var verdict = new RegleTonte().Evaluer(Apercu(heures));
        Assert.Equal(EtatVerdict.Defavorable, verdict.Etat);
    }

    [Fact]
    public void Tonte_Venteux_Passable()
    {
        var heures = BellesHeures(h => h.VentKmh = 35);
        var verdict = new RegleTonte().Evaluer(Apercu(heures));
        Assert.Equal(EtatVerdict.Passable, verdict.Etat);
    }

    [Fact]
    public void Tonte_SolDetrempe_Passable()
    {
        var heures = BellesHeures(h => h.HumiditeSol = 0.45);
        var verdict = new RegleTonte().Evaluer(Apercu(heures));
        Assert.Equal(EtatVerdict.Passable, verdict.Etat);
        Assert.Contains("détrempé", verdict.Raison);
    }

    [Fact]
    public void Tonte_HumiditeSolInconnue_ResteBon()
    {
        var heures = BellesHeures(h => h.HumiditeSol = null);
        var verdict = new RegleTonte().Evaluer(Apercu(heures));
        Assert.Equal(EtatVerdict.Bon, verdict.Etat);
    }

    [Fact]
    public void Tonte_SansDonnees_Defavorable()
    {
        var verdict = new RegleTonte().Evaluer(Apercu([]));
        Assert.Equal(EtatVerdict.Defavorable, verdict.Etat);
        Assert.Contains("prévisions", verdict.Raison);
    }

    // --- Aération ---

    [Fact]
    public void Aeration_AirDoux_Bon()
    {
        var verdict = new RegleAeration().Evaluer(Apercu(BellesHeures()));
        Assert.Equal(EtatVerdict.Bon, verdict.Etat);
    }

    [Fact]
    public void Aeration_UnPeuFrais_Passable()
    {
        var heures = BellesHeures(h => h.TemperatureC = 16);
        var verdict = new RegleAeration().Evaluer(Apercu(heures));
        Assert.Equal(EtatVerdict.Passable, verdict.Etat);
        Assert.Contains("frais", verdict.Raison);
    }

    [Fact]
    public void Aeration_TropFroid_Defavorable()
    {
        var heures = BellesHeures(h => h.TemperatureC = 5);
        var verdict = new RegleAeration().Evaluer(Apercu(heures));
        Assert.Equal(EtatVerdict.Defavorable, verdict.Etat);
        Assert.Contains("froid", verdict.Raison);
    }

    [Fact]
    public void Aeration_TropChaud_Defavorable()
    {
        var heures = BellesHeures(h => h.TemperatureC = 31);
        var verdict = new RegleAeration().Evaluer(Apercu(heures));
        Assert.Equal(EtatVerdict.Defavorable, verdict.Etat);
        Assert.Contains("chaud", verdict.Raison);
    }

    [Fact]
    public void Aeration_Pluie_Defavorable()
    {
        var heures = BellesHeures(h => h.ProbabilitePrecipitationPct = 70);
        var verdict = new RegleAeration().Evaluer(Apercu(heures));
        Assert.Equal(EtatVerdict.Defavorable, verdict.Etat);
    }

    [Fact]
    public void Aeration_Venteux_Passable()
    {
        var heures = BellesHeures(h => h.VentKmh = 28);
        var verdict = new RegleAeration().Evaluer(Apercu(heures));
        Assert.Equal(EtatVerdict.Passable, verdict.Etat);
    }

    // --- Journée dehors / en dedans ---

    [Fact]
    public void Dehors_BelleJournee_Bon()
    {
        var verdict = new RegleJourneeDehors().Evaluer(Apercu(BellesHeures()));
        Assert.Equal(EtatVerdict.Bon, verdict.Etat);
    }

    [Fact]
    public void Dehors_QuelquesBellesHeures_Passable()
    {
        // Pluie toute la journée sauf 13 h-15 h.
        var heures = BellesHeures(h =>
        {
            var belleFenetre = h.Heure.Hour >= 13 && h.Heure.Hour <= 15;
            if (belleFenetre == false)
            {
                h.PrecipitationMm = 1;
            }
        });
        var verdict = new RegleJourneeDehors().Evaluer(Apercu(heures));
        Assert.Equal(EtatVerdict.Passable, verdict.Etat);
    }

    [Fact]
    public void Dehors_JourneeDePluie_DefavorableDoncEnDedans()
    {
        var heures = BellesHeures(h => h.PrecipitationMm = 2);
        var verdict = new RegleJourneeDehors().Evaluer(Apercu(heures));
        Assert.Equal(EtatVerdict.Defavorable, verdict.Etat);
        Assert.Contains("en dedans", verdict.Raison);
    }

    [Fact]
    public void Dehors_JourneeGlaciale_Defavorable()
    {
        var heures = BellesHeures(h => h.TemperatureC = -5);
        var verdict = new RegleJourneeDehors().Evaluer(Apercu(heures));
        Assert.Equal(EtatVerdict.Defavorable, verdict.Etat);
    }

    // --- Bruine : code 51+ à 0 mm et faible probabilité (le piège du 2026-08-25) ---

    [Fact]
    public void Dehors_Bruine_SansMillimetres_PasBon()
    {
        var heures = BellesHeures(h => h.CodeMeteo = 51);
        var verdict = new RegleJourneeDehors().Evaluer(Apercu(heures));
        Assert.Equal(EtatVerdict.Defavorable, verdict.Etat);
    }

    [Fact]
    public void Tonte_BruineRecente_Defavorable()
    {
        var heures = BellesHeures(h =>
        {
            if (h.Heure == Maintenant.AddHours(-3))
            {
                h.CodeMeteo = 53;
            }
        });
        var verdict = new RegleTonte().Evaluer(Apercu(heures));
        Assert.Equal(EtatVerdict.Defavorable, verdict.Etat);
        Assert.Contains("plu", verdict.Raison);
    }

    [Fact]
    public void Aeration_Bruine_Defavorable()
    {
        var heures = BellesHeures(h => h.CodeMeteo = 51);
        var verdict = new RegleAeration().Evaluer(Apercu(heures));
        Assert.Equal(EtatVerdict.Defavorable, verdict.Etat);
    }

    [Fact]
    public void Aeration_SansDonnees_Defavorable()
    {
        var verdict = new RegleAeration().Evaluer(Apercu([]));
        Assert.Equal(EtatVerdict.Defavorable, verdict.Etat);
        Assert.Contains("prévisions", verdict.Raison);
    }

    [Fact]
    public void Dehors_SansDonnees_Defavorable()
    {
        var verdict = new RegleJourneeDehors().Evaluer(Apercu([]));
        Assert.Equal(EtatVerdict.Defavorable, verdict.Etat);
        Assert.Contains("prévisions", verdict.Raison);
    }
}
