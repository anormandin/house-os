namespace HouseOs.Api.Domaine;

public enum CategorieDocument
{
    Manuel,
    Photo,
    Assurance,
    Facture,
    Garantie,
    Contrat,
    PlanPermis,
    ImpotsTaxes,
    Autre,
}

/// <summary>
/// Un papier de la maison (acte, assurance, facture, manuel, photo…). Le fichier
/// vit sur disque ; l'entité survit à l'équipement ou la zone qu'elle référence.
/// </summary>
public class Document
{
    public Guid Id { get; set; }
    public required string Titre { get; set; }
    public CategorieDocument Categorie { get; set; }
    public Guid? EquipementId { get; set; }
    public Guid? ZoneId { get; set; }

    /// <summary>Dossier libre de classement (« 17 rue de la Colline », « Déménagement »…).</summary>
    public string? Dossier { get; set; }

    public string? Notes { get; set; }

    /// <summary>Date portée par le document (facture, signature de contrat…).</summary>
    public DateOnly? DateDocument { get; set; }

    /// <summary>Date d'expiration (assurance, permis…) — rappel visuel seulement.</summary>
    public DateOnly? Echeance { get; set; }

    public required string NomFichier { get; set; }

    /// <summary>Nom du fichier sur disque (relatif au dossier configuré), jamais le nom client.</summary>
    public required string CheminDisque { get; set; }

    public required string TypeMime { get; set; }
    public long Taille { get; set; }
    public DateTimeOffset CreeLe { get; set; }

    /// <summary>Vrai tant qu'un humain n'a pas confirmé la fiche d'un document arrivé
    /// tout seul (courriel) — la boîte « À classer » de la page Documents
    /// (D-2026-09-02 Boîte À Classer Des Documents).</summary>
    public bool AClasser { get; set; }

    /// <summary>Le courriel dont ce document est issu, s'il y a lieu (SET NULL).</summary>
    public Guid? ImportCourrielId { get; set; }
}
