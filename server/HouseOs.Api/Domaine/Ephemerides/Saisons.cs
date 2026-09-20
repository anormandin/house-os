namespace HouseOs.Api.Domaine.Ephemerides;

public enum Saison
{
    EquinoxeDeMars,
    SolsticeDeJuin,
    EquinoxeDeSeptembre,
    SolsticeDeDecembre,
}

/// <summary>
/// Un des quatre instants de l'année, en UTC. Le nom est celui du mois et non de la
/// saison : « équinoxe de printemps » est faux dans l'hémisphère sud, et le dépôt est
/// public. C'est au consommateur de le dire dans les mots de son foyer.
/// </summary>
public sealed record EvenementSaisonnier(Saison Saison, DateTimeOffset Instant);

/// <summary>
/// Équinoxes et solstices : les instants où la longitude apparente du soleil franchit
/// 0°, 90°, 180° et 270°. On part d'une date approchée et on itère sur la vitesse du
/// soleil sur l'écliptique (~0,9856° par jour) — trois passes suffisent à descendre
/// sous la seconde.
/// </summary>
public static class Saisons
{
    /// <summary>Degrés parcourus par jour, en moyenne, sur l'écliptique.</summary>
    private const double DegresParJour = 360.0 / 365.2422;

    private static readonly (Saison Saison, int Mois, int Jour, double Longitude)[] Reperes =
    [
        (Saison.EquinoxeDeMars, 3, 20, 0),
        (Saison.SolsticeDeJuin, 6, 21, 90),
        (Saison.EquinoxeDeSeptembre, 9, 22, 180),
        (Saison.SolsticeDeDecembre, 12, 21, 270),
    ];

    /// <summary>Les quatre instants d'une année civile, dans l'ordre.</summary>
    public static IReadOnlyList<EvenementSaisonnier> DeLAnnee(int annee) =>
        [.. Reperes.Select(r => new EvenementSaisonnier(r.Saison, Affiner(annee, r.Mois, r.Jour, r.Longitude)))];

    /// <summary>
    /// Le prochain des quatre, strictement après l'instant donné. On regarde l'année
    /// en cours puis la suivante : le solstice de décembre passé, le prochain rendez-vous
    /// est l'équinoxe de mars de l'an prochain.
    /// </summary>
    public static EvenementSaisonnier Prochain(DateTimeOffset apres)
    {
        var annee = apres.UtcDateTime.Year;
        return DeLAnnee(annee).Concat(DeLAnnee(annee + 1)).First(e => e.Instant > apres);
    }

    private static DateTimeOffset Affiner(int annee, int mois, int jour, double cible)
    {
        var jj = Soleil.JourJulien(new DateTime(annee, mois, jour, 0, 0, 0, DateTimeKind.Utc));
        for (var passe = 0; passe < 5; passe++)
        {
            var ecart = Ecart180(Soleil.LongitudeApparente(jj) - cible);
            jj -= ecart / DegresParJour;
        }
        return new DateTimeOffset(DateTime.FromOADate(jj - 2415018.5), TimeSpan.Zero);
    }

    /// <summary>Un écart d'angle ramené dans [−180, 180] : 359° de moins que 0°, c'est −1°.</summary>
    private static double Ecart180(double degres)
    {
        var d = (degres + 180) % 360;
        if (d < 0)
        {
            d += 360;
        }
        return d - 180;
    }
}
