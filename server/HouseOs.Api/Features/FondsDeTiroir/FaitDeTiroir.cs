namespace HouseOs.Api.Features.FondsDeTiroir;

/// <summary>Les six familles du fonds de tiroir (vault : Fonds De Tiroir).</summary>
public enum FamilleDeFait
{
    Ciel,
    Climat,
    Maison,
    Calendrier,
    Ville,
    Hasard,
}

/// <summary>
/// Les trois composantes du score, gardées séparées plutôt que multipliées d'avance :
/// quand un fait sort ou ne sort pas, on veut pouvoir dire lequel des trois a tranché.
/// </summary>
/// <param name="Rarete">La part de l'année que le fait peut occuper : 1 pour un fait
/// annuel, 1/365 pour un fait quotidien. Voir <see cref="Rarete.ParAn"/>.</param>
/// <param name="Fraicheur">La pénalité de radotage, de 0 (sorti aujourd'hui) à 1
/// (jamais sorti). Elle vient de l'historique des éditions.</param>
/// <param name="Pertinence">Ce que le fait change à aujourd'hui. Zéro fait disparaître
/// le fait : c'est comme ça qu'un fait contextuel se tait les jours ordinaires.</param>
public readonly record struct ScoreDeFait(double Rarete, double Fraicheur, double Pertinence)
{
    public double Total => Rarete * Fraicheur * Pertinence;
}

/// <summary>
/// Une petite chose vraie que la maison sait, datée, classée, et <b>sans mise en
/// forme</b> — ni pixel, ni colonne, ni 1-bit
/// (vault : D-2026-09-20 Fonds De Tiroir Séparé Du Journal).
/// </summary>
/// <param name="Cle">Stable d'un jour à l'autre : c'est elle qui porte la pénalité de
/// fraîcheur, et elle est publiée avec l'édition.</param>
/// <param name="Valeur">La forme courte, pour un widget étroit.</param>
/// <param name="Texte">La forme longue, pour un journal qui a de la place. Le
/// consommateur choisit ce qu'il peut montrer ; les widgets rétrécissent avant de
/// disparaître.</param>
public sealed record FaitDeTiroir(
    string Cle,
    FamilleDeFait Famille,
    string Etiquette,
    string Valeur,
    string Texte,
    ScoreDeFait Score)
{
    /// <summary>
    /// Le texte long, achevé. Un titre élagué finit par des points de suspension, et
    /// la phrase qui le cite finit par un point : « … . » ne s'écrit pas, et le
    /// corriger dans chaque famille serait une règle qu'une famille future oublierait.
    /// C'est le pendant, à l'autre bout de la phrase, de la ponctuation finale qu'on
    /// retire d'un titre avant de le citer.
    /// </summary>
    public string Texte { get; init; } = Texte.Replace("….", "…");

    /// <summary>
    /// Au-delà, la forme courte se fait couper dans une colonne étroite — « Équinoxe de
    /// septembre dans 2 jours » coupé au milieu, au mur, le 2026-09-20. Un test balaie
    /// l'année et le vérifie pour toutes les familles.
    /// </summary>
    public const int LongueurDeValeur = 30;
}

/// <summary>
/// La rareté d'un item : « combien de fois par année il peut paraître »
/// (vault : Éditorialiste De L'Écran), retournée en part de l'année. L'équinoxe, qui
/// ne peut sortir que quatre jours sur trois cent soixante-cinq, vaut ainsi presque
/// cent fois la durée du jour, qui peut sortir tous les jours.
/// </summary>
public static class Rarete
{
    public static double ParAn(double parutionsParAn) => 1.0 / parutionsParAn;

    /// <summary>Un fait que rien n'empêche de sortir chaque jour.</summary>
    public static readonly double Quotidien = ParAn(365);
}

/// <summary>
/// L'échelle de pertinence, écrite plutôt que devinée (vault : Fonds De Tiroir). Elle
/// existe parce que la <see cref="Rarete"/> ne se négocie pas : elle compte des jours
/// de parution possibles, et rien d'autre. Beaucoup de faits sont vrais <b>tous les
/// jours</b> une fois leur condition remplie — le doyen a toujours seize ans. C'est
/// donc la pertinence qui porte tout le poids éditorial : ce que le fait change à
/// <i>aujourd'hui</i>.
/// </summary>
public static class Pertinence
{
    /// <summary>
    /// Ça ne change rien à la journée : c'est là pour ne pas laisser de trou. Un cran
    /// <b>sous</b> la décoration, et c'est voulu — un bouche-trou doit passer derrière
    /// le fait le plus banal du fonds, sans quoi il ne serait plus un bouche-trou.
    /// </summary>
    public const double BoucheTrou = 0.5;

    public const double Decoration = 1;
    public const double EclaireLaJournee = 1.5;
    public const double SuggereUnGeste = 2;
    public const double EngageLaJournee = 3;
}
