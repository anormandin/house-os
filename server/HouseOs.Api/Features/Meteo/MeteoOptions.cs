using System.Globalization;

namespace HouseOs.Api.Features.Meteo;

/// <summary>Localisation et cadence de l'ingestion météo. Défauts : la ville de
/// Québec — chaque instance pointe sa propre maison (Meteo:Latitude/Longitude, ou
/// METEO_LATITUDE/METEO_LONGITUDE dans le .env du compose).</summary>
public class MeteoOptions
{
    public double Latitude { get; set; } = 46.81;
    public double Longitude { get; set; } = -71.21;
    public string FuseauHoraire { get; set; } = "America/Toronto";
    public int CadenceMinutes { get; set; } = 60;
    /// <summary>Jours de rétention des payloads bruts (releves_meteo).</summary>
    public int RetentionRelevesJours { get; set; } = 7;

    /// <summary>Années d'archive tirées pour les normales climatiques.</summary>
    public int NormalesAnnees { get; set; } = 10;

    /// <summary>
    /// À quelle fréquence on <b>vérifie</b> si les normales sont encore bonnes — pas à
    /// quelle fréquence on tire l'archive. La vérification est une lecture d'une ligne ;
    /// le tirage, lui, n'a lieu qu'au changement de coordonnées ou une fois l'an.
    /// </summary>
    public int NormalesCadenceHeures { get; set; } = 6;

    /// <summary>
    /// Âge au-delà duquel les normales se recalculent. Trois cent soixante et non
    /// trois cent soixante-cinq : l'archive s'arrête quelques jours avant aujourd'hui
    /// (<see cref="OpenMeteoArchive.RetardDeLArchiveJours"/>), et une fenêtre pile d'un
    /// an ouvrirait chaque année un trou d'une semaine dans « il a fait X° ce jour-là
    /// l'an dernier », juste avant le tirage suivant.
    /// </summary>
    public int NormalesAgeMaxJours { get; set; } = 360;

    /// <summary>
    /// Le fuseau du foyer. Un identifiant inconnu (faute de frappe dans le `.env`, base
    /// tzdata absente de l'image) ne doit rien faire tomber : on retombe sur celui du
    /// conteneur, qui est déjà réglé par FUSEAU_HORAIRE.
    /// </summary>
    public TimeZoneInfo Fuseau()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(FuseauHoraire);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return TimeZoneInfo.Local;
        }
    }

    /// <summary>
    /// Les coordonnées, telles qu'elles sont inscrites dans les lignes qu'on en déduit.
    /// C'est cette clé qui dit si les normales en base décrivent encore l'endroit où la
    /// maison se trouve — au déménagement, le `.env` change et tout se recalcule
    /// (vault : D-2026-09-20 Normales Climatiques Depuis L'archive Open-Meteo).
    ///
    /// <para>Quatre décimales, soit une dizaine de mètres : bien plus fin que la grille
    /// d'ERA5 (~9 km), et c'est voulu — dans le doute, mieux vaut recalculer pour rien
    /// que servir les normales de l'ancienne adresse.</para>
    /// </summary>
    public string CleCoordonnees =>
        $"{Latitude.ToString("F4", CultureInfo.InvariantCulture)},"
        + $"{Longitude.ToString("F4", CultureInfo.InvariantCulture)}";
}
