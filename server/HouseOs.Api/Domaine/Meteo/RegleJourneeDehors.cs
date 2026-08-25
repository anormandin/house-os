namespace HouseOs.Api.Domaine.Meteo;

/// <summary>Journée dehors ou journée en dedans : un seul verdict, basé sur le
/// nombre d'heures confortables entre le matin et la fin d'après-midi. Défavorable
/// signifie « une belle journée pour rester en dedans ».</summary>
public class RegleJourneeDehors : RegleJournee
{
    // Seuils v1 — ajustables par code + test.
    private const double ConfortMinC = 12;
    private const double ConfortMaxC = 28;
    private const int ProbabilitePluieMaxPct = 30;
    private const double VentMaxKmh = 30;
    private const int HeureDebutJour = 9;
    private const int HeureFinJour = 18;
    private const int HeuresConfortablesPourBon = 4;
    private const int HeuresConfortablesPourPassable = 2;

    public override string Nom => "Être dehors";

    public override VerdictRegle Evaluer(ApercuMeteo apercu)
    {
        var journee = apercu.Heures
            .Where(h => DateOnly.FromDateTime(h.Heure) == DateOnly.FromDateTime(apercu.Maintenant)
                && h.Heure.Hour >= HeureDebutJour && h.Heure.Hour <= HeureFinJour)
            .ToList();
        if (journee.Count == 0)
        {
            return SansDonnees();
        }

        var confortables = journee.Count(h =>
            h.TemperatureC >= ConfortMinC && h.TemperatureC <= ConfortMaxC
            && h.PrecipitationMm == 0 && h.AnnoncePrecipitation == false
            && h.ProbabilitePrecipitationPct < ProbabilitePluieMaxPct
            && h.VentKmh < VentMaxKmh);

        if (confortables >= HeuresConfortablesPourBon)
        {
            return new(Nom, EtatVerdict.Bon, "Belle journée pour être dehors.");
        }
        if (confortables >= HeuresConfortablesPourPassable)
        {
            return new(Nom, EtatVerdict.Passable, "Quelques belles heures dehors, choisis ton moment.");
        }
        return new(Nom, EtatVerdict.Defavorable, "Une journée parfaite pour rester en dedans.");
    }
}
