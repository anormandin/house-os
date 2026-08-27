using System.Security.Cryptography;
using System.Text;

namespace HouseOs.Api.Domaine;

public enum StatutTransaction
{
    Nouvelle,
    Liee,
    Ignoree,
}

/// <summary>
/// Transaction du compte fonds de prévoyance, importée d'un fichier CSV/OFX
/// (D-2026-08-26 Import Manuel D'abord Sync Ensuite). Déduplication par FITID
/// (OFX) ou hash (date, montant, description) — réimporter est sans effet.
/// </summary>
public class TransactionBancaire
{
    public Guid Id { get; set; }
    public Guid CompteBudgetId { get; set; }
    public CompteBudget? CompteBudget { get; set; }
    public DateOnly Date { get; set; }

    /// <summary>Montant signé : positif = dépôt, négatif = retrait.</summary>
    public decimal Montant { get; set; }
    public required string Description { get; set; }

    /// <summary>FITID OFX quand le fichier en fournit un, sinon null.</summary>
    public string? IdExterne { get; set; }

    /// <summary>Clé de dédup : FITID s'il existe, sinon hash (date, montant, description).</summary>
    public required string CleDedup { get; set; }

    public StatutTransaction Statut { get; set; } = StatutTransaction.Nouvelle;
    public DateTimeOffset ImporteeLe { get; set; }

    public static string CalculerCleDedup(string? idExterne, DateOnly date, decimal montant, string description)
    {
        if (string.IsNullOrWhiteSpace(idExterne) == false)
        {
            return $"fitid:{idExterne.Trim()}";
        }
        var brut = $"{date:yyyy-MM-dd}|{montant:0.00}|{description.Trim().ToUpperInvariant()}";
        return $"hash:{Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(brut)))}";
    }
}
