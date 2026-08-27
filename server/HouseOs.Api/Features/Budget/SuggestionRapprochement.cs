using System.Text.RegularExpressions;

namespace HouseOs.Api.Features.Budget;

/// <summary>
/// Heuristique de suggestion v1 : l'enveloppe la plus souvent liée à un marchand
/// semblable dans l'historique. Pas de table de règles (la spec la réserve pour
/// plus tard) ; le serveur MCP permet déjà des suggestions LLM par-dessus.
/// </summary>
public static partial class SuggestionRapprochement
{
    [GeneratedRegex(@"[\d#*]+")]
    private static partial Regex ChiffresEtDieses();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Espaces();

    /// <summary>« CANADIAN TIRE #213 » et « CANADIAN TIRE #078 » → même marchand :
    /// majuscules, chiffres et # retirés, espaces repliés.</summary>
    public static string NormaliserMarchand(string description)
    {
        var sansChiffres = ChiffresEtDieses().Replace(description.ToUpperInvariant(), " ");
        return Espaces().Replace(sansChiffres, " ").Trim();
    }

    /// <summary>
    /// Enveloppe suggérée pour un marchand : la plus fréquente parmi les liaisons
    /// passées du même marchand normalisé. Null si jamais vu.
    /// </summary>
    public static Guid? Suggerer(
        string description,
        IEnumerable<(string DescriptionPassee, Guid EnveloppeId)> liaisonsPassees)
    {
        var marchand = NormaliserMarchand(description);
        if (marchand.Length == 0)
        {
            return null;
        }
        var candidats = liaisonsPassees
            .Where(l => NormaliserMarchand(l.DescriptionPassee) == marchand)
            .GroupBy(l => l.EnveloppeId)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();
        return candidats?.Key;
    }

    /// <summary>Un dépôt « proche » du virement suggéré (±10 %) déclenche la proposition
    /// de ventilation et de complétion de la tâche de virement.</summary>
    public static bool EstProcheDuVirement(decimal montantDepot, decimal virementSuggere)
    {
        if (virementSuggere <= 0 || montantDepot <= 0)
        {
            return false;
        }
        var ecart = Math.Abs(montantDepot - virementSuggere);
        return ecart <= virementSuggere * 0.10m;
    }
}
