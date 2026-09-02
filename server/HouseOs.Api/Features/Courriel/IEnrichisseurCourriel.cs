using HouseOs.Api.Features.Humeur;
using Microsoft.Extensions.Options;

namespace HouseOs.Api.Features.Courriel;

/// <summary>Le seul point de contact du relevé avec un LLM — remplaçable en test par
/// un fictif, pour qu'aucune suite ne parle jamais à Anthropic.</summary>
public interface IEnrichisseurCourriel
{
    /// <summary>Null = pas de proposition (pas de clé, échec, réponse illisible) → repli.</summary>
    Task<PropositionLlm?> ProposerAsync(ContexteEnrichissement contexte, CancellationToken ct);
}

/// <summary>Haiku via le SDK Anthropic ; clé et modèle partagés avec le titre d'humeur.</summary>
public sealed class EnrichisseurAnthropic(
    IOptions<HumeurOptions> humeur,
    ILogger<EnrichisseurAnthropic> journal) : IEnrichisseurCourriel
{
    public async Task<PropositionLlm?> ProposerAsync(ContexteEnrichissement contexte, CancellationToken ct)
    {
        var cle = humeur.Value.CleEffective();
        if (cle is null)
        {
            journal.LogDebug("Courriel : pas de clé Anthropic — métadonnées de repli.");
            return null;
        }
        try
        {
            return await EnrichissementCourriel.ProposerAsync(contexte, cle, humeur.Value.Modele, ct);
        }
        catch (Exception ex) when (ct.IsCancellationRequested == false)
        {
            journal.LogWarning(ex, "Courriel : enrichissement LLM échoué pour « {Sujet} » — repli.", contexte.Sujet);
            return null;
        }
    }
}
