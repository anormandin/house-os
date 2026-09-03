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
}
