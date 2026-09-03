namespace HouseOs.Api.Domaine;

/// <summary>
/// Un écran e-ink enrôlé (D-2026-09-03 Registre Des Appareils D'affichage) : son
/// identité selon le protocole TRMNL (MAC, identifiant court, clé) et la dernière
/// télémétrie qu'il a annoncée. Une ligne par appareil, réécrite à chaque réveil —
/// pas d'historique tant qu'aucune règle n'en consomme.
/// </summary>
public class AppareilAffichage
{
    public Guid Id { get; set; }

    /// <summary>Adresse MAC telle que le firmware l'envoie (en-tête ID), en majuscules.</summary>
    public required string AdresseMac { get; set; }

    /// <summary>Six caractères hexadécimaux : ce que l'appareil affiche à l'accueil et
    /// ce qui nomme ses URL d'image.</summary>
    public required string Identifiant { get; set; }

    /// <summary>La clé remise à l'enrôlement, renvoyée telle quelle à chaque réveil
    /// (Access-Token). En clair : le protocole exige de pouvoir la redonner.</summary>
    public required string Cle { get; set; }

    public string? Nom { get; set; }
    public string? Modele { get; set; }
    public int? Largeur { get; set; }
    public int? Hauteur { get; set; }
    public string? VersionFirmware { get; set; }
    public double? TensionPile { get; set; }
    public int? Rssi { get; set; }
    public DateTimeOffset EnroleLe { get; set; }
    public DateTimeOffset? DernierContact { get; set; }

    /// <summary>Signature du dernier bitmap servi — ce que l'appareil affiche en ce moment.</summary>
    public string? DernierFichier { get; set; }
}
