namespace HouseOs.Api.Domaine;

/// <summary>
/// Le compte bancaire réel du fonds de prévoyance, partitionné en enveloppes
/// virtuelles (D-2026-08-26 Compte Unique Et Enveloppes Virtuelles). V1 : un seul
/// compte, mais rien dans le schéma n'en empêche d'autres.
/// Solde courant = solde initial ancré + Σ transactions importées postérieures —
/// jamais saisi à la main après l'ancrage. L'ancrage reste éditable.
/// </summary>
public class CompteBudget
{
    public Guid Id { get; set; }
    public required string Nom { get; set; }
    public string? Institution { get; set; }
    public decimal SoldeInitial { get; set; }
    public DateOnly DateAncrage { get; set; }

    /// <summary>Tâche récurrente « virer X $ au fonds » — sert à suggérer sa complétion
    /// quand un dépôt proche du virement suggéré apparaît. Sans lien : aucune suggestion.</summary>
    public Guid? TacheVirementId { get; set; }
    public Tache? TacheVirement { get; set; }

    public DateTimeOffset CreeLe { get; set; }
}
