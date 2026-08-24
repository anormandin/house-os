namespace HouseOs.Api.Domaine.Meteo;

/// <summary>Une heure de prévision normalisée — jamais la forme de l'API source.</summary>
public class PrevisionHoraire
{
    public int Id { get; set; }
    /// <summary>Heure locale de la maison (fuseau configuré, sans offset).</summary>
    public DateTime Heure { get; set; }
    public double TemperatureC { get; set; }
    public double PrecipitationMm { get; set; }
    public int ProbabilitePrecipitationPct { get; set; }
    public double VentKmh { get; set; }
    public double RafalesKmh { get; set; }
    public int HumiditePct { get; set; }
    /// <summary>Humidité du sol en surface (m³/m³) — signal « le gazon est-il sec ».</summary>
    public double? HumiditeSol { get; set; }
    public int CouvertureNuageusePct { get; set; }
    public double IndiceUv { get; set; }
}
