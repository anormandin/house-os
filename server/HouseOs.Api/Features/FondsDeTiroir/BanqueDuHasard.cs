namespace HouseOs.Api.Features.FondsDeTiroir;

/// <summary>
/// Un dicton de l'almanach, rattaché à un mois. C'est le mois qui porte le dicton :
/// « Quand septembre est beau et doux, octobre est fou » n'a rien à dire en février.
/// </summary>
public sealed record DictonDeLAlmanach(int Mois, string Texte);

/// <summary>
/// Quand une fête tombe, dans une année donnée. Quatre formes, toutes <b>déclaratives</b> :
/// le fichier reste de la donnée, la règle reste du C# testable
/// (vault : D-2026-08-23 Pas De N8n Dans Le Cœur).
///
/// <list type="bullet">
/// <item><c>mois</c> + <c>jour</c> — une date fixe (le 24 juin).</item>
/// <item><c>mois</c> + <c>jourDeSemaine</c> + <c>rang</c> — le n-ième de ce jour dans le
/// mois (le 1er lundi de septembre) ; un rang négatif se compte depuis la fin.</item>
/// <item><c>mois</c> + <c>jourDeSemaine</c> + <c>jour</c> — le dernier de ce jour
/// <b>avant</b> la date (le lundi qui précède le 25 mai).</item>
/// <item><c>depuisPaques</c> — un décalage en jours depuis Pâques (−2 pour le Vendredi
/// saint). Sans ça, un foyer chrétien perdrait la moitié de ses jours fériés.</item>
/// </list>
///
/// <para>Ces quatre formes couvrent les systèmes de fêtes occidentaux : la banque
/// québécoise s'échange contre celle d'un autre pays sans toucher au code
/// (vault : Distribution).</para>
/// </summary>
public sealed record QuandLaFete
{
    public int? Mois { get; init; }
    public int? Jour { get; init; }

    /// <summary>En toutes lettres et en français : « lundi », « dimanche ».</summary>
    public string? JourDeSemaine { get; init; }

    /// <summary>1 = le premier du mois, −1 = le dernier.</summary>
    public int? Rang { get; init; }

    public int? DepuisPaques { get; init; }

    private static readonly Dictionary<string, DayOfWeek> JoursDeSemaine = new(StringComparer.OrdinalIgnoreCase)
    {
        ["dimanche"] = DayOfWeek.Sunday,
        ["lundi"] = DayOfWeek.Monday,
        ["mardi"] = DayOfWeek.Tuesday,
        ["mercredi"] = DayOfWeek.Wednesday,
        ["jeudi"] = DayOfWeek.Thursday,
        ["vendredi"] = DayOfWeek.Friday,
        ["samedi"] = DayOfWeek.Saturday,
    };

    /// <summary>
    /// La date de cette fête dans l'année donnée, ou <c>null</c> si la description ne
    /// tient pas debout (un mois hors bornes, un jour de semaine mal orthographié, un
    /// cinquième lundi dans un mois qui n'en a que quatre). Une fête qu'on ne sait pas
    /// dater ne sort pas — c'est la règle du fonds : une source qui manque se tait.
    /// </summary>
    public DateOnly? DansLAnnee(int annee)
    {
        if (DepuisPaques is { } decalage)
        {
            return Paques(annee).AddDays(decalage);
        }
        if (Mois is not { } mois || mois < 1 || mois > 12)
        {
            return null;
        }
        var joursDuMois = DateTime.DaysInMonth(annee, mois);

        if (JourDeSemaine is null)
        {
            return Jour is { } jour && jour >= 1 && jour <= joursDuMois
                ? new DateOnly(annee, mois, jour)
                : null;
        }
        if (JoursDeSemaine.TryGetValue(JourDeSemaine, out var jourVise) == false)
        {
            return null;
        }

        // « Le lundi qui précède le 25 » : on recule depuis la veille de la date pivot.
        if (Jour is { } pivot)
        {
            if (pivot < 2 || pivot > joursDuMois)
            {
                return null;
            }
            var date = new DateOnly(annee, mois, pivot).AddDays(-1);
            while (date.DayOfWeek != jourVise)
            {
                date = date.AddDays(-1);
            }
            // Une semaine plus tôt peut déborder sur le mois d'avant : ce n'est plus la
            // fête décrite.
            return date.Month == mois ? date : null;
        }

        if (Rang is not { } rang || rang == 0)
        {
            return null;
        }
        var candidats = Enumerable.Range(1, joursDuMois)
            .Select(j => new DateOnly(annee, mois, j))
            .Where(d => d.DayOfWeek == jourVise)
            .ToList();
        var index = rang > 0 ? rang - 1 : candidats.Count + rang;
        return index >= 0 && index < candidats.Count ? candidats[index] : null;
    }

    /// <summary>
    /// Le comput grégorien (Meeus/Jones/Butcher). Écrit à la main comme les
    /// éphémérides : c'est de l'arithmétique fermée, pas une dépendance.
    /// </summary>
    private static DateOnly Paques(int annee)
    {
        var a = annee % 19;
        var b = annee / 100;
        var c = annee % 100;
        var d = b / 4;
        var e = b % 4;
        var f = (b + 8) / 25;
        var g = (b - f + 1) / 3;
        var h = ((19 * a) + b - d - g + 15) % 30;
        var i = c / 4;
        var k = c % 4;
        var l = (32 + (2 * e) + (2 * i) - h - k) % 7;
        var m = (a + (11 * h) + (22 * l)) / 451;
        var mois = (h + l - (7 * m) + 114) / 31;
        var jour = ((h + l - (7 * m) + 114) % 31) + 1;
        return new DateOnly(annee, mois, jour);
    }
}

/// <param name="Nom">La forme courte, celle qui prend la valeur du fait. Un nom trop
/// long est élagué plutôt que coupé au mur.</param>
/// <param name="Texte">Ce que le nom ne dit pas déjà — sinon la ligne est perdue.</param>
/// <param name="Ferie">Un jour chômé, par opposition à une journée qu'on souligne
/// sans fermer boutique. Les deux se disent, mais pas avec le même poids.</param>
public sealed record FeteDeLAnnee(string Nom, string Texte, bool Ferie, QuandLaFete Quand);

/// <summary>
/// La banque du hasard : des dictons et des fêtes, en <b>données</b>, jamais en dur
/// (vault : Fonds De Tiroir, étape 4). Un foyer ailleurs remplace le fichier et garde
/// le code — c'est tout l'intérêt
/// (vault : D-2026-09-20 Banque Du Hasard En Fichier De Données).
///
/// <para>Une banque vide n'est pas un cas dégradé : c'est l'état d'une installation qui
/// n'en veut pas, et la famille se tait, comme le ciel sans coordonnées.</para>
/// </summary>
public sealed record BanqueDuHasard(
    IReadOnlyList<DictonDeLAlmanach> Dictons,
    IReadOnlyList<FeteDeLAnnee> Fetes)
{
    public static readonly BanqueDuHasard Vide = new([], []);

    public bool EstVide => Dictons.Count == 0 && Fetes.Count == 0;
}
