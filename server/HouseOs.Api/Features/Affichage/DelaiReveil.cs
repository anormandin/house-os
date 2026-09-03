namespace HouseOs.Api.Features.Affichage;

/// <summary>
/// Combien de secondes l'appareil doit dormir avant de redemander l'écran : la
/// cadence du jour, ou d'un trait jusqu'au matin pendant la nuit. Pur, testé.
/// </summary>
public static class DelaiReveil
{
    public static int Calculer(DateTime maintenant, AffichageOptions options)
    {
        var plafond = Math.Max(60, options.PlafondSecondes);
        var cadence = Math.Clamp(options.CadenceJourSecondes, 60, plafond);
        var heure = TimeOnly.FromDateTime(maintenant);

        if (EstLaNuit(heure, options.NuitDebut, options.NuitFin) == false)
        {
            return cadence;
        }

        // Jusqu'à la fin de la nuit — qui peut être demain matin.
        var fin = maintenant.Date.Add(options.NuitFin.ToTimeSpan());
        if (fin <= maintenant)
        {
            fin = fin.AddDays(1);
        }
        var restant = (int)Math.Ceiling((fin - maintenant).TotalSeconds);
        // Un appareil qui se réveille en pleine nuit pour rien vide sa pile pour rien :
        // au plus le plafond, jamais moins que la cadence du jour.
        return Math.Clamp(restant, cadence, plafond);
    }

    /// <summary>Nuit qui enjambe minuit (22 h → 5 h 30) ou, si on l'a configurée à
    /// l'envers, plage simple dans la journée.</summary>
    public static bool EstLaNuit(TimeOnly heure, TimeOnly debut, TimeOnly fin)
    {
        if (debut == fin)
        {
            return false;
        }
        return debut > fin
            ? heure >= debut || heure < fin
            : heure >= debut && heure < fin;
    }
}
