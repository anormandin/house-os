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
    /// Plancher de la pénalité d'<b>une</b> parution. Un fait sorti hier ne doit pas
    /// valoir exactement zéro : le jour où c'est la seule chose vraie qui reste, il vaut
    /// mieux se répéter que laisser un trou dans le journal.
    /// </summary>
    private const double PlancherDeFraicheur = 0.02;

    private readonly IReadOnlyDictionary<string, IReadOnlyList<DateOnly>> _parutions;

    /// <param name="parutions">Toutes les dates où chaque clé est sortie, pas seulement
    /// la dernière : c'est l'insistance qui se paie, pas le dernier jour.</param>
    public HistoriqueDeParution(IReadOnlyDictionary<string, IReadOnlyList<DateOnly>> parutions) =>
        _parutions = parutions;

    /// <summary>L'historique d'une maison qui n'a encore rien publié.</summary>
    public static readonly HistoriqueDeParution Vide = new(new Dictionary<string, IReadOnlyList<DateOnly>>());

    /// <summary>
    /// Le produit des pénalités de chaque parution de la semaine, chacune remontant au
    /// carré de son âge. Une pénalité linéaire au seul dernier jour ne pesait pas
    /// assez : un fait sorti hier gardait encore plus du quart de son score, et la rareté
    /// d'un fait à fenêtre (le premier gel, 38 jours par an) vaut dix fois celle d'un
    /// fait quotidien — le gel, la douceur et la collecte spéciale sortaient donc
    /// <b>tous les jours</b> de leur fenêtre, huit sur huit la semaine du 21 septembre
    /// 2026 (vault : D-2026-09-28 Fraîcheur Cumulée Des Faits). Au carré, un fait sorti
    /// hier tombe à ~4 % ; chaque parution de la semaine multiplie la pénalité.
    /// </summary>
    public double Fraicheur(string cle, DateOnly aujourdhui)
    {
        if (_parutions.TryGetValue(cle, out var dates) == false)
        {
            return 1;
        }
        var fraicheur = 1.0;
        foreach (var date in dates)
        {
            var jours = aujourdhui.DayNumber - date.DayNumber;
            if (jours >= JoursDeMemoire)
            {
                continue;
            }
            // Une parution dans le futur (horloge de travers, rattrapage) compte comme
            // aujourd'hui plutôt que de remonter la fraîcheur au-dessus de 1.
            var part = Math.Clamp(jours / (double)JoursDeMemoire, 0, 1);
            fraicheur *= PlancherDeFraicheur + (1 - PlancherDeFraicheur) * part * part;
        }
        return fraicheur;
    }
}
