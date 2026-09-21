using HouseOs.Api.Domaine.Editorial;
using HouseOs.Api.Features.Humeur;
using Microsoft.Extensions.Options;

namespace HouseOs.Api.Features.Editorial;

/// <summary>Le seul point de contact de l'édition avec un LLM — remplaçable en test par
/// un fictif à compteur, pour prouver qu'un second rendu n'appelle personne.</summary>
public interface IRedacteurEdition
{
    /// <summary>Null = rien d'utilisable (pas de clé, échec, réponse hors contrat) → gabarit.</summary>
    Task<TexteDEdition?> RedigerAsync(MatiereDEdition matiere, CancellationToken ct);

    /// <summary>Le modèle qui écrit, pour le consigner sur l'édition.</summary>
    string Modele { get; }
}

/// <summary>Opus via le SDK Anthropic ; la clé est celle du titre d'humeur.</summary>
public sealed class RedacteurAnthropic(
    IOptions<HumeurOptions> humeur,
    IOptions<EditionOptions> edition,
    ILogger<RedacteurAnthropic> journal) : IRedacteurEdition
{
    public string Modele => edition.Value.Modele;

    public async Task<TexteDEdition?> RedigerAsync(MatiereDEdition matiere, CancellationToken ct)
    {
        var cle = humeur.Value.CleEffective();
        if (cle is null)
        {
            journal.LogDebug("Édition : pas de clé Anthropic — gabarit.");
            return null;
        }
        try
        {
            var (texte, ecart) = await RedactionLlm.RedigerAvecEcart(matiere, cle, Modele, ct);
            if (texte is null)
            {
                journal.LogWarning("Édition : réponse du modèle hors contrat ({Ecart}) — gabarit.", ecart);
            }
            return texte;
        }
        catch (Exception ex) when (ct.IsCancellationRequested == false)
        {
            journal.LogWarning(ex, "Édition : appel du modèle raté — gabarit.");
            return null;
        }
    }
}
