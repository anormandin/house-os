namespace HouseOs.Api.Domaine.Ephemerides;

/// <summary>
/// Un passage à l'heure avancée ou à l'heure normale. <paramref name="Avance"/> est
/// vrai quand on perd une heure.
/// </summary>
public sealed record ChangementDHeure(DateOnly Date, bool Avance, TimeSpan Ecart);

/// <summary>
/// Le prochain changement d'heure du fuseau, cherché dans les règles du système
/// (tzdata) plutôt que codé en dur : les dates de bascule changent, et beaucoup de
/// fuseaux n'en ont aucune. Un foyer à Phoenix ou à Singapour n'en verra jamais —
/// c'est un fait qui ne sort pas, pas un fait faux.
/// </summary>
public static class ChangementHeure
{
    /// <summary>
    /// Le prochain changement, <b>le jour même compris</b>. La comparaison part de la
    /// veille, pas d'aujourd'hui : la bascule a lieu au petit matin, si bien qu'à midi
    /// le jour du changement l'horloge porte déjà le nouveau décalage. En partant
    /// d'aujourd'hui, le changement devenait invisible le seul jour où il compte, et
    /// l'écran sautait d'un « dans 1 jour » la veille à plus rien du tout.
    /// </summary>
    public static ChangementDHeure? Prochain(TimeZoneInfo fuseau, DateOnly apres, int fenetreJours = 400)
    {
        if (fuseau.SupportsDaylightSavingTime == false)
        {
            return null;
        }

        var precedent = Decalage(fuseau, apres.AddDays(-1));
        for (var i = 0; i <= fenetreJours; i++)
        {
            var jour = apres.AddDays(i);
            var courant = Decalage(fuseau, jour);
            if (courant != precedent)
            {
                return new ChangementDHeure(jour, courant > precedent, courant - precedent);
            }
            precedent = courant;
        }
        return null;
    }

    /// <summary>
    /// Le décalage vu à midi : c'est la seule heure de la journée qui existe à coup
    /// sûr des deux côtés d'une bascule (2 h du matin, le jour du passage à l'heure
    /// avancée, n'existe pas).
    /// </summary>
    private static TimeSpan Decalage(TimeZoneInfo fuseau, DateOnly jour) =>
        fuseau.GetUtcOffset(DateTime.SpecifyKind(jour.ToDateTime(new TimeOnly(12, 0)), DateTimeKind.Unspecified));
}
