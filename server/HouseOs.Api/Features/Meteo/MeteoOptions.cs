namespace HouseOs.Api.Features.Meteo;

/// <summary>Localisation et cadence de l'ingestion météo. Défauts : la nouvelle
/// maison à Sainte-Catherine-de-la-Jacques-Cartier.</summary>
public class MeteoOptions
{
    public double Latitude { get; set; } = 46.85;
    public double Longitude { get; set; } = -71.62;
    public string FuseauHoraire { get; set; } = "America/Toronto";
    public int CadenceMinutes { get; set; } = 60;
    /// <summary>Jours de rétention des payloads bruts (releves_meteo).</summary>
    public int RetentionRelevesJours { get; set; } = 7;
}
