namespace HouseOs.Api.Domaine.Ephemerides;

/// <summary>
/// Le point pour lequel le ciel se calcule — les coordonnées du `.env`
/// (Meteo:Latitude/Longitude). Le dépôt est public : rien ici ne suppose le Québec.
/// </summary>
public readonly record struct Lieu(double Latitude, double Longitude);

/// <summary>
/// Ce que le soleil fait un jour donné, en heure de l'horloge locale.
///
/// <para>Le lever et le coucher <b>manquent</b> au-delà des cercles polaires : il y a
/// des jours, là-bas, où le soleil ne se lève pas et d'autres où il ne se couche pas.
/// C'est un cas normal, pas une erreur, et le fonds de tiroir doit alors dire autre
/// chose plutôt que d'inventer une heure.</para>
/// </summary>
public sealed record JourSolaire(
    DateOnly Date,
    TimeOnly? Lever,
    TimeOnly? Coucher,
    TimeOnly MidiSolaire,
    TimeSpan? Duree,
    bool JourPolaire,
    bool NuitPolaire);

/// <summary>
/// Position du soleil par les formules de la NOAA (Solar Calculation Details),
/// écrites à la main — aucune dépendance NuGet
/// (vault : Plan 2026-09-20 Journal Éditorial, étape 2).
///
/// <para>Précision : de l'ordre de la minute entre les cercles polaires, ce qui est
/// très au-delà de ce qu'un écran mural demande. Comme la feuille de la NOAA, la
/// déclinaison est évaluée à minuit local et non au midi solaire : c'est ce qui rend
/// les valeurs comparables aux siennes, qui servent de référence aux tests.</para>
/// </summary>
public static class Soleil
{
    /// <summary>
    /// Zénith du lever et du coucher officiels : 90° 50′. Les 50 minutes d'arc sont
    /// le demi-diamètre apparent du disque (16′) plus la réfraction de l'atmosphère
    /// au ras de l'horizon (34′) — c'est pour ça que le jour dure un peu plus de
    /// douze heures à l'équinoxe, partout sur la Terre.
    /// </summary>
    public const double ZenithOfficiel = 90.833;

    /// <summary>Un jour du calendrier, vu du lieu, à l'heure locale de ce jour-là.</summary>
    public static JourSolaire Jour(DateOnly date, Lieu lieu, TimeSpan decalage)
    {
        var siecle = SiecleJulien(JourJulienMinuitLocal(date, decalage));
        var declinaison = Declinaison(siecle);

        // Midi solaire : le passage du soleil au méridien du lieu, ramené à l'heure
        // que l'horloge affiche. 4 minutes par degré de longitude.
        var midiMinutes = 720 - 4 * lieu.Longitude - EquationDuTemps(siecle) + decalage.TotalMinutes;
        var midi = HeureLocale(midiMinutes);

        var cosAngleHoraire =
            Cos(ZenithOfficiel) / (Cos(lieu.Latitude) * Cos(declinaison))
            - Tan(lieu.Latitude) * Tan(declinaison);

        // Hors [-1, 1], l'angle horaire n'existe pas : le soleil ne franchit pas
        // l'horizon de la journée, dans un sens ou dans l'autre.
        if (cosAngleHoraire > 1)
        {
            return new JourSolaire(date, null, null, midi, TimeSpan.Zero, false, true);
        }
        if (cosAngleHoraire < -1)
        {
            return new JourSolaire(date, null, null, midi, TimeSpan.FromDays(1), true, false);
        }

        var angleHoraire = Degres(Math.Acos(cosAngleHoraire));
        return new JourSolaire(
            date,
            HeureLocale(midiMinutes - 4 * angleHoraire),
            HeureLocale(midiMinutes + 4 * angleHoraire),
            midi,
            TimeSpan.FromMinutes(8 * angleHoraire),
            false,
            false);
    }

    /// <summary>
    /// La longitude apparente du soleil sur l'écliptique, en degrés — 0° à l'équinoxe
    /// de mars, 90° au solstice de juin. C'est la grandeur que <see cref="Saisons"/>
    /// fait franchir à ses quatre seuils.
    /// </summary>
    public static double LongitudeApparente(double jourJulien)
    {
        var t = SiecleJulien(jourJulien);
        var vraieLongitude = LongitudeMoyenne(t) + EquationDuCentre(t);
        // La nutation et l'aberration, le seul raffinement qui compte à cette échelle.
        var apparente = vraieLongitude - 0.00569 - 0.00478 * Sin(125.04 - 1934.136 * t);
        return Normaliser360(apparente);
    }

    /// <summary>Jour julien à minuit, heure locale du lieu.</summary>
    public static double JourJulienMinuitLocal(DateOnly date, TimeSpan decalage) =>
        JourJulien(date.ToDateTime(TimeOnly.MinValue)) - decalage.TotalHours / 24.0;

    /// <summary>Jour julien d'un instant UTC.</summary>
    public static double JourJulien(DateTime instantUtc) =>
        // 2415018.5 : le jour julien du 30 décembre 1899, l'origine que .NET partage
        // avec le sérial d'Excel — donc avec la feuille de calcul de la NOAA.
        instantUtc.ToOADate() + 2415018.5;

    private static double SiecleJulien(double jourJulien) => (jourJulien - 2451545.0) / 36525.0;

    private static double LongitudeMoyenne(double t) =>
        Normaliser360(280.46646 + t * (36000.76983 + t * 0.0003032));

    private static double AnomalieMoyenne(double t) => 357.52911 + t * (35999.05029 - 0.0001537 * t);

    private static double Excentricite(double t) => 0.016708634 - t * (0.000042037 + 0.0000001267 * t);

    private static double EquationDuCentre(double t)
    {
        var m = AnomalieMoyenne(t);
        return Sin(m) * (1.914602 - t * (0.004817 + 0.000014 * t))
            + Sin(2 * m) * (0.019993 - 0.000101 * t)
            + Sin(3 * m) * 0.000289;
    }

    /// <summary>Obliquité de l'écliptique, corrigée de la nutation.</summary>
    private static double Obliquite(double t)
    {
        var moyenne = 23 + (26 + (21.448 - t * (46.815 + t * (0.00059 - t * 0.001813))) / 60) / 60;
        return moyenne + 0.00256 * Cos(125.04 - 1934.136 * t);
    }

    private static double Declinaison(double t)
    {
        var jj = t * 36525.0 + 2451545.0;
        return Degres(Math.Asin(Sin(Obliquite(t)) * Sin(LongitudeApparente(jj))));
    }

    /// <summary>
    /// L'équation du temps, en minutes : l'écart entre le soleil et l'horloge, qui
    /// va de −14 min en février à +16 min en novembre parce que l'orbite est une
    /// ellipse et que l'axe est incliné.
    /// </summary>
    private static double EquationDuTemps(double t)
    {
        var l0 = LongitudeMoyenne(t);
        var m = AnomalieMoyenne(t);
        var e = Excentricite(t);
        var y = Math.Pow(Tan(Obliquite(t) / 2), 2);

        return 4 * Degres(
            y * Sin(2 * l0)
            - 2 * e * Sin(m)
            + 4 * e * y * Sin(m) * Cos(2 * l0)
            - 0.5 * y * y * Sin(4 * l0)
            - 1.25 * e * e * Sin(2 * m));
    }

    /// <summary>
    /// Des minutes depuis minuit vers l'heure affichée. Le repliement sur 24 h n'est
    /// pas de la coquetterie : un fuseau très décalé de sa longitude (la Chine, l'ouest
    /// de l'Espagne) sort du jour civil, et un `TimeOnly` hors bornes lèverait.
    /// </summary>
    private static TimeOnly HeureLocale(double minutesDepuisMinuit)
    {
        var minutes = minutesDepuisMinuit % 1440;
        if (minutes < 0)
        {
            minutes += 1440;
        }
        return TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(minutes));
    }

    private static double Normaliser360(double degres)
    {
        var d = degres % 360;
        return d < 0 ? d + 360 : d;
    }

    private static double Radians(double degres) => degres * Math.PI / 180.0;
    private static double Degres(double radians) => radians * 180.0 / Math.PI;
    private static double Sin(double degres) => Math.Sin(Radians(degres));
    private static double Cos(double degres) => Math.Cos(Radians(degres));
    private static double Tan(double degres) => Math.Tan(Radians(degres));
}
