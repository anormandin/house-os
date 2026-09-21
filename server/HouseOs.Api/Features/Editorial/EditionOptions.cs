namespace HouseOs.Api.Features.Editorial;

/// <summary>
/// Le réglage propre à l'édition : son modèle. Le créneau du matin et la clé API sont
/// ceux de <see cref="Humeur.HumeurOptions"/> — une seule heure de réveil et un seul
/// compte Anthropic pour la maison — mais les deux couches ne partagent ni modèle ni
/// prompt (D-2026-09-20 Édition Écrite Par Opus).
/// </summary>
public class EditionOptions
{
    public string Modele { get; set; } = "claude-opus-5";
}
