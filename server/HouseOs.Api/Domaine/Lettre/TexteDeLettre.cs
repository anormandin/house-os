namespace HouseOs.Api.Domaine.Lettre;

/// <summary>Ce que la maison écrit — et seulement ça. La salutation, la signature et
/// le pied sont du gabarit.</summary>
public sealed record TexteDeLettre(string Sujet, IReadOnlyList<string> Paragraphes)
{
    /// <summary>Ce qui part seul dans l'aperçu de notification : les premiers signes du
    /// premier paragraphe. C'est aussi ce que la mémoire retient.</summary>
    public string PremiereLigne(int longueur)
    {
        var premier = Paragraphes.Count > 0 ? Paragraphes[0] : "";
        return premier.Length <= longueur ? premier : premier[..longueur].TrimEnd() + "…";
    }
}
