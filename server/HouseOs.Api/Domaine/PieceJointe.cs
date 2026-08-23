namespace HouseOs.Api.Domaine;

/// <summary>Manuel PDF ou photo d'un équipement. Le fichier vit sur disque.</summary>
public class PieceJointe
{
    public Guid Id { get; set; }
    public Guid EquipementId { get; set; }
    public required string NomFichier { get; set; }

    /// <summary>Nom du fichier sur disque (relatif au dossier configuré), jamais le nom client.</summary>
    public required string CheminDisque { get; set; }

    public required string TypeMime { get; set; }
    public long Taille { get; set; }
    public DateTimeOffset CreeLe { get; set; }
}
