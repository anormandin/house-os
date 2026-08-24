namespace HouseOs.Api.Features.Humeur;

/// <summary>Cadence et modèle du polissage LLM de la phrase du jour
/// (D-2026-08-24 Phrase Du Jour Haiku Matin Et Soir).</summary>
public class HumeurOptions
{
    public TimeOnly HeureMatin { get; set; } = new(5, 30);
    public TimeOnly HeureSoir { get; set; } = new(17, 0);
    public string Modele { get; set; } = "claude-haiku-4-5";
    /// <summary>Clé API Anthropic ; vide → repli sur l'env ANTHROPIC_API_KEY,
    /// et sans clé du tout la banque de gabarits fait le travail.</summary>
    public string CleApi { get; set; } = "";

    public string? CleEffective() =>
        string.IsNullOrWhiteSpace(CleApi)
            ? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") is { Length: > 0 } env ? env : null
            : CleApi;
}
