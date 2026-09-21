namespace HouseOs.Api.Domaine.Editorial;

/// <summary>
/// L'édition écrite sans modèle : le repli obligatoire, et le fonctionnement normal
/// d'une installation sans clé API (D-2026-09-20 Édition Écrite Par Opus). Elle rend
/// exactement ce que le mur montrait avant l'éditorialiste — la phrase du jour en
/// manchette, la raison du plancher en surtitre — et aucun corps : un gabarit qui
/// écrirait des paragraphes serait un éditorialiste déguisé.
/// </summary>
public static class GabaritEdition
{
    public static TexteDEdition Ecrire(PlancherDuJour? plancher, (string Titre, string SousTitre) phrase)
    {
        if (plancher is not null)
        {
            return new TexteDEdition(Plancher.Surtitre(plancher.Raison), plancher.Titre, phrase.SousTitre, [], []);
        }
        return new TexteDEdition("", phrase.Titre, phrase.SousTitre, [], []);
    }
}
