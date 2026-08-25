namespace HouseOs.Api.Domaine.Meteo;

/// <summary>Bonne fenêtre pour tondre : gazon sec (pas de pluie récente, sol pas
/// détrempé), rien à l'horizon proche, ni trop froid ni trop venteux.</summary>
public class RegleTonte : RegleJournee
{
    // Seuils v1 — ajustables par code + test.
    private const int HeuresSechesAvant = 12;
    private const int HeuresSechesApres = 4;
    private const double PluieRecenteMaxMm = 0.5;
    private const int ProbabilitePluieMaxPct = 30;
    private const double TemperatureMinC = 10;
    private const double VentMaxKmh = 30;
    private const double SolDetrempe = 0.35;

    public override string Nom => "Tondre";

    public override VerdictRegle Evaluer(ApercuMeteo apercu)
    {
        var avant = Fenetre(apercu, HeuresSechesAvant, 0);
        var apres = Fenetre(apercu, 0, HeuresSechesApres);
        if (avant.Count == 0 || apres.Count == 0)
        {
            return SansDonnees();
        }

        if (avant.Sum(h => h.PrecipitationMm) > PluieRecenteMaxMm
            || avant.Any(h => h.AnnoncePrecipitation))
        {
            return new(Nom, EtatVerdict.Defavorable, "Il a plu dans les dernières heures — le gazon est mouillé.");
        }
        if (apres.Any(h => h.PrecipitationMm > 0 || h.AnnoncePrecipitation
            || h.ProbabilitePrecipitationPct >= ProbabilitePluieMaxPct))
        {
            return new(Nom, EtatVerdict.Defavorable, "De la pluie s'en vient.");
        }
        if (apres.Max(h => h.TemperatureC) < TemperatureMinC)
        {
            return new(Nom, EtatVerdict.Defavorable, "Trop froid pour tondre.");
        }
        if (apres.Max(h => h.VentKmh) >= VentMaxKmh)
        {
            return new(Nom, EtatVerdict.Passable, "Venteux — faisable, mais pas idéal.");
        }

        var humiditeSol = apres.Select(h => h.HumiditeSol).FirstOrDefault(s => s.HasValue);
        if (humiditeSol >= SolDetrempe)
        {
            return new(Nom, EtatVerdict.Passable, "Le sol est encore détrempé.");
        }

        return new(Nom, EtatVerdict.Bon, "Gazon sec et pas de pluie en vue — belle fenêtre pour tondre.");
    }
}
