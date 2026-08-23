namespace HouseOs.Api.Domaine;

public enum TypeZone
{
    Interieur,
    Exterieur,
}

/// <summary>Une pièce ou un espace extérieur. Liste plate, pas de hiérarchie.</summary>
public class Zone
{
    public Guid Id { get; set; }
    public required string Nom { get; set; }
    public TypeZone Type { get; set; } = TypeZone.Interieur;
    public int Ordre { get; set; }
}
