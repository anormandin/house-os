namespace HouseOs.Api.Domaine.Meteo;

/// <summary>Le signal de pluie « aujourd'hui » montré à l'utilisateur. L'agrégat
/// quotidien d'Open-Meteo couvre le jour civil entier : à 10 h, son max peut venir
/// d'heures déjà passées (66 % à minuit) et contredire les règles, qui jugent les
/// heures à venir (issue #58). Ici : le max des heures restantes de la journée.</summary>
public static class PluieDuJour
{
    /// <summary>Probabilité de pluie max de l'heure en cours à la fin de la journée,
    /// ou null quand aucune heure restante n'est connue (repli : l'agrégat du jour).</summary>
    public static int? ProbabiliteRestantePct(ApercuMeteo apercu)
    {
        var aujourdhui = DateOnly.FromDateTime(apercu.Maintenant);
        var heureCourante = apercu.Maintenant.Date.AddHours(apercu.Maintenant.Hour);
        var restantes = apercu.Heures
            .Where(h => DateOnly.FromDateTime(h.Heure) == aujourdhui && h.Heure >= heureCourante)
            .ToList();
        return restantes.Count == 0
            ? null
            : restantes.Max(h => h.ProbabilitePrecipitationPct);
    }
}
