namespace HouseOs.Api.Features.FluxExternes;

/// <summary>
/// Les bornes d'écriture que partagent les <b>deux</b> chemins d'entrée d'un flux
/// externe : l'ICS téléchargé (<see cref="LectureIcs"/>) et la poussée par l'API
/// (<see cref="PousseeEndpoints"/>). Ce sont les longueurs du schéma
/// (HouseOsDbContext) : un titre réel de cinq cents signes ne doit pas faire échouer
/// tout le flux en « value too long », en boucle, à vie.
///
/// <para>La décision demande « les mêmes bornes de longueur que l'ICS »
/// (vault : D-2026-09-20 Flux Externe Poussé) — elles vivent donc ici, à un seul
/// endroit, plutôt qu'en double dans les deux chemins.</para>
/// </summary>
public static class BornesDuFlux
{
    public const int LongueurMaxTitre = 200;
    public const int LongueurMaxUid = 300;

    /// <summary>Un titre entrant, élagué à la borne du schéma. Vide ou blanc : le
    /// calendrier n'a pas de titre, et l'événement le dit plutôt que de mentir.</summary>
    public static string Titre(string? titre) =>
        string.IsNullOrWhiteSpace(titre)
            ? "(sans titre)"
            : titre.Trim().Length > LongueurMaxTitre
                ? titre.Trim()[..LongueurMaxTitre]
                : titre.Trim();

    /// <summary>
    /// L'identifiant d'une occurrence : celui de la source, suffixé de sa date. C'est
    /// le <b>préfixe</b> qui se fait tronquer, jamais la date — sans quoi deux
    /// occurrences d'un même événement porteraient le même identifiant.
    /// </summary>
    public static string Uid(string? uidSource, DateOnly date)
    {
        var suffixe = $":{date:yyyy-MM-dd}";
        var source = uidSource ?? "";
        if (source.Length + suffixe.Length > LongueurMaxUid)
        {
            source = source[..(LongueurMaxUid - suffixe.Length)];
        }
        return source + suffixe;
    }
}
