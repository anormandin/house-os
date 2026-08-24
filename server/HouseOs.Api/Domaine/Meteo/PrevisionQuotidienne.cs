namespace HouseOs.Api.Domaine.Meteo;

/// <summary>Un jour de prévision normalisée (agrégats + lever/coucher).</summary>
public class PrevisionQuotidienne
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public double TemperatureMinC { get; set; }
    public double TemperatureMaxC { get; set; }
    public double PrecipitationMm { get; set; }
    public int ProbabilitePrecipitationMaxPct { get; set; }
    public double VentMaxKmh { get; set; }
    public double RafalesMaxKmh { get; set; }
    public double IndiceUvMax { get; set; }
    /// <summary>Code temps WMO (0 = dégagé, 61 = pluie…), pour l'icône de l'UI.</summary>
    public int CodeMeteo { get; set; }
    public TimeOnly Lever { get; set; }
    public TimeOnly Coucher { get; set; }
}
