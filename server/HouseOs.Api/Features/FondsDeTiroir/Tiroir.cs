namespace HouseOs.Api.Features.FondsDeTiroir;

/// <summary>
/// Le fonds de tiroir : il rend une <b>liste de faits ordonnés par score</b>, et rien
/// d'autre. Il ne connaît ni pixel, ni colonne, ni 1-bit — le budget de widgets, les
/// rangs et la lettrine appartiennent au journal
/// (vault : D-2026-09-20 Fonds De Tiroir Séparé Du Journal).
///
/// <para>Familles branchées à ce jour : <see cref="FamilleDeFait.Ciel"/>. Les cinq
/// autres arrivent aux étapes 3 à 6 du plan, et chacune n'ajoute qu'une ligne à
/// <see cref="Candidats"/>.</para>
/// </summary>
public static class Tiroir
{
    /// <summary>
    /// Les faits du jour, du meilleur au moins bon. Un fait dont une source manque
    /// n'est simplement pas là ; un fait sans pertinence du jour ne sort pas.
    /// </summary>
    public static IReadOnlyList<FaitDeTiroir> Ouvrir(
        ContexteDuJour contexte, HistoriqueDeParution historique) =>
        [.. Classer(Candidats(contexte), historique, contexte.Date)];

    private static IEnumerable<FaitDeTiroir> Candidats(ContexteDuJour contexte) =>
        FaitsDuCiel.Produire(contexte);

    /// <summary>
    /// `score = rareté × fraîcheur × pertinence du jour`. La fraîcheur n'est pas
    /// connue du producteur du fait : elle se lit ici, dans les clés que les éditions
    /// précédentes ont publiées.
    /// </summary>
    public static IEnumerable<FaitDeTiroir> Classer(
        IEnumerable<FaitDeTiroir> candidats, HistoriqueDeParution historique, DateOnly aujourdhui) =>
        candidats
            .Select(f => f with { Score = f.Score with { Fraicheur = historique.Fraicheur(f.Cle, aujourdhui) } })
            .Where(f => f.Score.Total > 0)
            // La clé départage les ex æquo : deux tirages de la même journée doivent
            // donner le même journal, sinon l'écran change tout seul au réveil suivant.
            .OrderByDescending(f => f.Score.Total)
            .ThenBy(f => f.Cle, StringComparer.Ordinal);
}
