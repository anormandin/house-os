namespace HouseOs.Api.Domaine.Meteo;

/// <summary>
/// Une journée de l'archive climatique (réanalyse ERA5), normalisée — jamais la forme
/// de l'API source. Une dizaine d'années de ces lignes, c'est la matière des
/// <see cref="NormalesClimatiques"/>
/// (vault : D-2026-09-20 Normales Climatiques Depuis L'archive Open-Meteo).
///
/// <para>Elles sont gardées plutôt que jetées après le calcul, pour deux raisons : le
/// fait « il a fait X° ce jour-là l'an dernier » lit une de ces lignes — les tables de
/// prévisions, elles, sont remplacées à chaque heure et ne se souviennent de rien — et
/// un changement de statistique se recalcule alors sans rappeler le réseau, comme le
/// payload brut archivé de l'ingestion horaire
/// (vault : D-2026-08-24 Tables Météo Normalisées).</para>
/// </summary>
public class JourDeClimat
{
    public int Id { get; set; }

    /// <summary>
    /// Les coordonnées auxquelles cette journée a été tirée, telles que
    /// <c>MeteoOptions.CleCoordonnees</c> les écrit. La table entière ne porte qu'une
    /// clé à la fois : le jour où le `.env` en change, ces lignes ne décrivent plus le
    /// lieu où la maison se trouve, et elles partent.
    /// </summary>
    public string Coordonnees { get; set; } = "";

    public DateOnly Date { get; set; }
    public double TemperatureMinC { get; set; }
    public double TemperatureMaxC { get; set; }
    public double PrecipitationMm { get; set; }

    /// <summary>La neige tombée, en centimètres (l'archive la donne ainsi).</summary>
    public double NeigeCm { get; set; }
}
