namespace HouseOs.Api.Domaine.Meteo;

/// <summary>Une météo qui mérite d'être mentionnée — en bien ou en mal. Absente
/// les jours ordinaires : la phrase du jour ne parle météo que sur ce signal
/// (D-2026-08-25 Phrase Du Jour Axée Tâches).</summary>
public record SignalMeteoRemarquable(string Description, bool Favorable);

public static class MeteoRemarquable
{
    // Seuils v1 — ajustables par code + test.
    private const int CodeOrageMin = 95;
    private const double PluieSoutenueMm = 5;
    private const int HeuresPluvieusesPourJourneeDePluie = 5;
    private const int HeureDebutJour = 8;
    private const int HeureFinJour = 20;
    private const double ChaleurIntenseC = 30;
    private const double FroidIntenseC = -15;
    private const double GrandVentRafalesKmh = 60;
    private const int CodeCielDegageMax = 1;
    private const double BelleJourneeMinC = 18;
    private const double BelleJourneeMaxC = 27;

    public static SignalMeteoRemarquable? Evaluer(ApercuMeteo apercu)
    {
        var date = DateOnly.FromDateTime(apercu.Maintenant);
        var jour = apercu.Jours.FirstOrDefault(j => j.Date == date);
        var heures = apercu.Heures
            .Where(h => DateOnly.FromDateTime(h.Heure) == date)
            .ToList();
        if (jour is null || heures.Count == 0)
        {
            return null;
        }

        if (heures.Any(h => h.CodeMeteo >= CodeOrageMin))
        {
            return new("des orages attendus aujourd'hui", Favorable: false);
        }
        if (heures.Any(h => h.CodeMeteo is (>= 71 and <= 77) or 85 or 86))
        {
            return new("de la neige attendue aujourd'hui", Favorable: false);
        }

        var heuresPluvieuses = heures.Count(h =>
            h.Heure.Hour >= HeureDebutJour && h.Heure.Hour <= HeureFinJour
            && (h.PrecipitationMm > 0 || h.AnnoncePrecipitation));
        if (jour.PrecipitationMm >= PluieSoutenueMm
            || heuresPluvieuses >= HeuresPluvieusesPourJourneeDePluie)
        {
            return new("de la pluie pour une bonne partie de la journée", Favorable: false);
        }

        if (jour.TemperatureMaxC >= ChaleurIntenseC)
        {
            return new($"une grosse chaleur (jusqu'à {Math.Round(jour.TemperatureMaxC)} °C)", Favorable: false);
        }
        if (jour.TemperatureMaxC <= FroidIntenseC)
        {
            return new($"un froid mordant (au mieux {Math.Round(jour.TemperatureMaxC)} °C)", Favorable: false);
        }
        if (jour.RafalesMaxKmh >= GrandVentRafalesKmh)
        {
            return new("du grand vent aujourd'hui", Favorable: false);
        }

        var dehors = new RegleJourneeDehors().Evaluer(apercu);
        if (jour.CodeMeteo <= CodeCielDegageMax
            && jour.TemperatureMaxC >= BelleJourneeMinC && jour.TemperatureMaxC <= BelleJourneeMaxC
            && dehors.Etat == EtatVerdict.Bon)
        {
            return new("une journée magnifique pour être dehors", Favorable: true);
        }

        return null;
    }
}
