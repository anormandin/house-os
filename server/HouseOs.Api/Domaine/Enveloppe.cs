namespace HouseOs.Api.Domaine;

public enum TypeEnveloppe
{
    Equipement,
    Taxes,
    Projet,
    Reserve,
}

public enum StatutEnveloppe
{
    Active,
    Fermee,
}

/// <summary>Un versement daté de l'échéancier d'une enveloppe Taxes (JSONB, statique :
/// les versements passés sont ignorés du calcul, la mise à jour annuelle est manuelle).</summary>
public record Versement(DateOnly Date, decimal Montant);

/// <summary>
/// Partition virtuelle du compte fonds de prévoyance. Le solde n'est jamais stocké :
/// il est la somme du journal de mouvements (D-2026-08-26 Mouvements D'enveloppe En
/// Journal). La cible en dollars vit ici ; l'échéance est dérivée de la tâche liée
/// quand un lien existe (D-2026-08-26 Cibles D'enveloppe Dérivées Des Tâches).
/// </summary>
public class Enveloppe
{
    public Guid Id { get; set; }
    public required string Nom { get; set; }
    public TypeEnveloppe Type { get; set; }
    public decimal? MontantCible { get; set; }

    /// <summary>Date cible saisie librement — ignorée si une tâche est liée
    /// (l'échéance dérive alors de sa prochaine occurrence).</summary>
    public DateOnly? DateCible { get; set; }

    /// <summary>Liens exclusifs : une tâche OU un équipement, jamais les deux.</summary>
    public Guid? TacheId { get; set; }
    public Tache? Tache { get; set; }
    public Guid? EquipementId { get; set; }
    public Equipement? Equipement { get; set; }

    /// <summary>Échéancier de versements (type Taxes), liste JSONB.</summary>
    public List<Versement>? Echeancier { get; set; }

    public StatutEnveloppe Statut { get; set; } = StatutEnveloppe.Active;
    public DateTimeOffset CreeLe { get; set; }

    /// <summary>Fermer exige un solde à zéro (transférer d'abord le reste). Une enveloppe
    /// ne se supprime jamais — son journal de mouvements est de l'historique.</summary>
    public void Fermer(decimal solde)
    {
        if (Statut == StatutEnveloppe.Fermee)
        {
            throw new InvalidOperationException("Enveloppe déjà fermée.");
        }
        if (solde != 0)
        {
            throw new InvalidOperationException(
                "Le solde doit être à zéro pour fermer (transférer d'abord le reste).");
        }
        Statut = StatutEnveloppe.Fermee;
    }
}
