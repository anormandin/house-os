namespace HouseOs.Api.Domaine.Meteo;

/// <summary>Bon moment pour ouvrir les fenêtres : air doux, sec, vent raisonnable,
/// jugé sur les prochaines heures.</summary>
public class RegleAeration : RegleJournee
{
    // Seuils v1 — ajustables par code + test.
    private const int HeuresRegardees = 2;
    private const double TemperatureIdealeMinC = 18;
    private const double TemperatureIdealeMaxC = 26;
    private const double TemperatureTolereeMinC = 15;
    private const double TemperatureTolereeMaxC = 28;
    private const int ProbabilitePluieMaxPct = 30;
    private const double VentMaxKmh = 25;

    public override string Nom => "Aérer";

    public override VerdictRegle Evaluer(ApercuMeteo apercu)
    {
        var prochaines = Fenetre(apercu, 0, HeuresRegardees);
        if (prochaines.Count == 0)
        {
            return SansDonnees();
        }

        if (prochaines.Any(h => h.PrecipitationMm > 0 || h.AnnoncePrecipitation
            || h.ProbabilitePrecipitationPct >= ProbabilitePluieMaxPct))
        {
            return new(Nom, EtatVerdict.Defavorable, "Il pleut ou risque de pleuvoir — fenêtres fermées.");
        }
        if (prochaines.Max(h => h.VentKmh) >= VentMaxKmh)
        {
            return new(Nom, EtatVerdict.Passable, "Pas mal venteux pour ouvrir grand.");
        }

        var temperature = prochaines.Average(h => h.TemperatureC);
        if (temperature >= TemperatureIdealeMinC && temperature <= TemperatureIdealeMaxC)
        {
            return new(Nom, EtatVerdict.Bon, "Air parfait — un bon moment pour aérer la maison.");
        }
        if (temperature >= TemperatureTolereeMinC && temperature <= TemperatureTolereeMaxC)
        {
            var nuance = temperature < TemperatureIdealeMinC ? "frais" : "chaud";
            return new(Nom, EtatVerdict.Passable, $"Un peu {nuance} dehors, mais aérable.");
        }
        if (temperature < TemperatureTolereeMinC)
        {
            return new(Nom, EtatVerdict.Defavorable, "Trop froid dehors pour aérer.");
        }
        return new(Nom, EtatVerdict.Defavorable, "Trop chaud dehors pour aérer.");
    }
}
