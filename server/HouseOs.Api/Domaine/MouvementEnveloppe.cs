namespace HouseOs.Api.Domaine;

public enum TypeMouvement
{
    Provision,
    Retrait,
    Ajustement,
    Transfert,
}

/// <summary>
/// Journal des mouvements d'une enveloppe — le solde est toujours Σ des montants,
/// jamais une colonne mutée (D-2026-08-26 Mouvements D'enveloppe En Journal).
/// Un transfert entre enveloppes = deux mouvements opposés, même date.
/// Un dépôt ventilé = N mouvements Provision pointant la même transaction
/// (D-2026-08-26 Ventilation Du Dépôt Multi-Enveloppes).
/// </summary>
public class MouvementEnveloppe
{
    public Guid Id { get; set; }
    public Guid EnveloppeId { get; set; }
    public Enveloppe? Enveloppe { get; set; }
    public DateOnly Date { get; set; }

    /// <summary>Montant signé : positif entre dans l'enveloppe, négatif en sort.</summary>
    public decimal Montant { get; set; }
    public TypeMouvement Type { get; set; }

    public Guid? TransactionBancaireId { get; set; }
    public TransactionBancaire? TransactionBancaire { get; set; }

    /// <summary>Référence historique vers une entrée du journal de complétion — pas de FK,
    /// même patron que le journal lui-même (survit aux suppressions).</summary>
    public Guid? EntreeJournalId { get; set; }

    public string? Note { get; set; }
    public DateTimeOffset CreeLe { get; set; }
}
