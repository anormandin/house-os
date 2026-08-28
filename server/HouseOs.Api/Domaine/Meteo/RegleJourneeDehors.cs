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
        if (apercu.Heures.Count == 0)
        {
            return SansDonnees();
        }

        // Le verdict porte sur le reste de la journée : compter les heures déjà
        // écoulées laissait un beau matin masquer la pluie de l'après-midi —
        // « Belle journée pour être dehors » affiché pendant l'averse (issue #58).
        var restantes = apercu.Heures
            .Where(h => DateOnly.FromDateTime(h.Heure) == DateOnly.FromDateTime(apercu.Maintenant)
                && h.Heure >= HeureCourante(apercu)
                && h.Heure.Hour >= HeureDebutJour && h.Heure.Hour <= HeureFinJour)
            .ToList();
        if (restantes.Count == 0)
        {
            // En soirée la fenêtre de jour est passée (verdict neutre, sans
            // pastille) ; plus tôt, c'est que les prévisions du jour manquent.
            return apercu.Maintenant.Hour > HeureFinJour
                ? new(Nom, EtatVerdict.Passable, "La journée est derrière nous — verdict demain matin.")
                : SansDonnees();
        }

        var confortables = restantes.Count(h =>
            h.TemperatureC >= ConfortMinC && h.TemperatureC <= ConfortMaxC
            && h.PrecipitationMm == 0 && h.AnnoncePrecipitation == false
            && h.ProbabilitePrecipitationPct < ProbabilitePluieMaxPct
            && h.VentKmh < VentMaxKmh);

        // En fin de journée, moins d'heures restent que les seuils : on juge ce
        // qui reste (une belle soirée demeure « dehors », pas « en dedans »).
        if (confortables >= Math.Min(HeuresConfortablesPourBon, restantes.Count))
        {
            return new(Nom, EtatVerdict.Bon, "Belle journée pour être dehors.");
        }
        if (confortables >= Math.Min(HeuresConfortablesPourPassable, restantes.Count))
        {
            return new(Nom, EtatVerdict.Passable, "Quelques belles heures dehors, choisis ton moment.");
        }
        return new(Nom, EtatVerdict.Defavorable, "Une journée parfaite pour rester en dedans.");
    }
}
