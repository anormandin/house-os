using HouseOs.Api.Domaine.Lettre;
using HouseOs.Api.Features.Editorial;
using HouseOs.Api.Features.Humeur;
using Microsoft.Extensions.Options;

namespace HouseOs.Api.Features.Lettre;

/// <summary>Le seul point de contact de la lettre avec un LLM — remplaçable en test par
/// un fictif à compteur.</summary>
public interface IRedacteurLettre
{
    /// <summary>Null = rien d'utilisable (pas de clé, échec, réponse hors contrat).</summary>
    Task<TexteDeLettre?> RedigerAsync(MatiereDeLettre matiere, CancellationToken ct);

    string Modele { get; }

    /// <summary>Faux sans clé API : la note est alors le fonctionnement normal, pas un
    /// échec à réessayer.</summary>
    bool PeutEcrire { get; }
}

/// <summary>Le modèle de l'édition, la clé du titre d'humeur : un seul compte pour la
/// maison (D-2026-09-21 Lettre Écrite À Part Sur La Même Matière).</summary>
public sealed class RedacteurLettreAnthropic(
    IOptions<HumeurOptions> humeur,
    IOptions<EditionOptions> edition,
    ILogger<RedacteurLettreAnthropic> journal) : IRedacteurLettre
{
    public string Modele => edition.Value.Modele;

    public bool PeutEcrire => humeur.Value.CleEffective() is not null;

    public async Task<TexteDeLettre?> RedigerAsync(MatiereDeLettre matiere, CancellationToken ct)
    {
        var cle = humeur.Value.CleEffective();
        if (cle is null)
        {
            journal.LogDebug("Lettre : pas de clé Anthropic — note en gabarit.");
            return null;
        }
        try
        {
            var reponse = await RedactionLettre.RedigerAvecEcart(matiere, cle, Modele, ct);
            if (reponse.Texte is null)
            {
                journal.LogWarning("Lettre : réponse du modèle hors contrat ({Ecart}).", reponse.Ecart);
            }
            return reponse.Texte;
        }
        catch (Exception ex) when (ct.IsCancellationRequested == false)
        {
            journal.LogWarning(ex, "Lettre : appel du modèle raté.");
            return null;
        }
    }
}
