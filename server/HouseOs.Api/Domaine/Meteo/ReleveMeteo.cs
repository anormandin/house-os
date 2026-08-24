namespace HouseOs.Api.Domaine.Meteo;

/// <summary>Payload brut d'une récupération Open-Meteo, archivé pour renormaliser
/// au besoin. Purgé après quelques jours.</summary>
public class ReleveMeteo
{
    public int Id { get; set; }
    public DateTimeOffset RecupereLe { get; set; }
    public required string Payload { get; set; }
}
