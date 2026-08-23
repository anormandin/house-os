namespace HouseOs.Api.Domaine;

/// <summary>
/// Un actif de la maison (fournaise, chauffe-eau, tondeuse…). Les specs libres
/// (taille de filtre, code de peinture) vivent en JSONB.
/// </summary>
public class Equipement
{
    public Guid Id { get; set; }
    public required string Nom { get; set; }
    public Guid? ZoneId { get; set; }
    public Zone? Zone { get; set; }
    public string? Marque { get; set; }
    public string? Modele { get; set; }
    public string? NumeroSerie { get; set; }
    public DateOnly? DateAchat { get; set; }
    public DateOnly? FinGarantie { get; set; }
    public string? Notes { get; set; }
    public Dictionary<string, string> Specs { get; set; } = [];
    public DateTimeOffset CreeLe { get; set; }
    public List<PieceJointe> PiecesJointes { get; } = [];
}
