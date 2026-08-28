using System.Globalization;
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

    /// <summary>FITID OFX ou numéro de séquence CSV quand le fichier en fournit un, sinon null.</summary>
    public string? IdExterne { get; set; }

    /// <summary>Clé de dédup : FITID s'il existe, sinon hash (date, montant, description,
    /// numéro de séquence et rang d'occurrence intra-fichier le cas échéant).</summary>
    public required string CleDedup { get; set; }

    public StatutTransaction Statut { get; set; } = StatutTransaction.Nouvelle;
    public DateTimeOffset ImporteeLe { get; set; }

    /// <summary>
    /// Formats en culture invariante : la clé doit survivre à un changement de locale
    /// de l'hôte (déménagement du conteneur) sans invalider les clés déjà stockées.
    /// <paramref name="numeroSequence"/> (CSV AccWeb) et <paramref name="occurrence"/>
    /// (rang d'un doublon exact intra-fichier, 0 = première) distinguent deux
    /// transactions légitimes identiques le même jour tout en gardant le réimport
    /// idempotent — absents, la clé reste identique à sa forme historique.
    /// </summary>
    public static string CalculerCleDedup(
        string? idExterne, DateOnly date, decimal montant, string description,
        string? numeroSequence = null, int occurrence = 0)
    {
        if (string.IsNullOrWhiteSpace(idExterne) == false)
        {
            var cle = $"fitid:{idExterne.Trim()}";
            // La spec OFX permet 255 caractères de FITID ; la colonne en stocke 100.
            return cle.Length <= 100 ? cle : $"fitid-sha:{Hacher(cle)}";
        }
        var brut = $"{date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}" +
            $"|{montant.ToString("0.00", CultureInfo.InvariantCulture)}" +
            $"|{description.Trim().ToUpperInvariant()}";
        if (string.IsNullOrWhiteSpace(numeroSequence) == false)
        {
            brut += $"|seq:{numeroSequence.Trim()}";
        }
        if (occurrence > 0)
        {
            brut += $"|occ:{occurrence.ToString(CultureInfo.InvariantCulture)}";
        }
        return $"hash:{Hacher(brut)}";
    }

    private static string Hacher(string valeur) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(valeur)));
}
