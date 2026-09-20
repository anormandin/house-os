namespace HouseOs.Api.Features.FondsDeTiroir;

/// <summary>
/// Ce que les éditions précédentes ont déjà dit, par clé de fait. C'est la mémoire qui
/// crée la surprise quotidienne : sans elle, le même « le jour raccourcit de trois
/// minutes » sortirait cent quatre-vingts jours de suite.
///
/// <para>À l'étape 2 du plan, cet historique est <b>vide</b> : il se branche aux
/// éditions matérialisées à l'étape 7
/// (vault : D-2026-09-20 Une Édition Par Jour Matérialisée). Un historique vide n'est
/// pas un cas dégradé — c'est l'état d'une installation neuve, et le fonds doit sortir
/// normalement.</para>
/// </summary>
public sealed class HistoriqueDeParution
{
    /// <summary>Mémoire des éditions : au-delà, un fait a le droit de revenir entier.</summary>
    public const int JoursDeMemoire = 7;

    /// <summary>
    /// Plancher de la pénalité. Un fait sorti hier ne doit pas valoir exactement zéro :
    /// le jour où c'est la seule chose vraie qui reste, il vaut mieux se répéter que
    /// laisser un trou dans le journal.
    /// </summary>
    private const double PlancherDeFraicheur = 0.15;

    private readonly IReadOnlyDictionary<string, DateOnly> _dernieresParutions;

    public HistoriqueDeParution(IReadOnlyDictionary<string, DateOnly> dernieresParutions) =>
        _dernieresParutions = dernieresParutions;

    /// <summary>L'historique d'une maison qui n'a encore rien publié.</summary>
    public static readonly HistoriqueDeParution Vide = new(new Dictionary<string, DateOnly>());

    public double Fraicheur(string cle, DateOnly aujourdhui)
    {
        if (_dernieresParutions.TryGetValue(cle, out var derniere) == false)
        {
            return 1;
        }
        var jours = aujourdhui.DayNumber - derniere.DayNumber;
        if (jours >= JoursDeMemoire)
        {
            return 1;
        }
        // Une parution dans le futur (horloge de travers, rattrapage) compte comme
        // aujourd'hui plutôt que de remonter la fraîcheur au-dessus de 1.
        var part = Math.Clamp(jours / (double)JoursDeMemoire, 0, 1);
        return PlancherDeFraicheur + (1 - PlancherDeFraicheur) * part;
    }
}
