namespace HouseOs.Api.Domaine;

public enum StatutImportCourriel
{
    Importe,
    Ignore,
    Erreur,
}

/// <summary>
/// Trace d'un courriel relevé du dépôt (Courriel Entrant) : ce qui en est sorti, ou
/// pourquoi rien n'en est sorti. C'est aussi la garde de déduplication — un courriel
/// transféré deux fois (même Message-ID) ou un objet dont l'effacement R2 a raté ne
/// produit jamais un second lot de documents.
/// </summary>
public class ImportCourriel
{
    public Guid Id { get; set; }

    /// <summary>Clé de l'objet dans le dépôt (R2) — unique : un objet, un passage.</summary>
    public required string CleDepot { get; set; }

    /// <summary>Message-ID RFC 5322 — unique quand présent ; null pour les lignes
    /// ignorées/en erreur, qui n'ont pas à réserver l'identifiant.</summary>
    public string? MessageId { get; set; }

    public required string Expediteur { get; set; }
    public required string Sujet { get; set; }

    /// <summary>Date portée par le courriel (en-tête Date).</summary>
    public DateTimeOffset RecuLe { get; set; }

    public DateTimeOffset TraiteLe { get; set; }
    public StatutImportCourriel Statut { get; set; }
    public string? Erreur { get; set; }
    public int NbDocuments { get; set; }
}
