using System.Globalization;

namespace HouseOs.Api.Features.FondsDeTiroir;

/// <summary>
/// Les quelques tournures que les familles partagent. Rien de mis en forme ici — ni
/// pixel, ni colonne : seulement du français correct
/// (vault : D-2026-09-20 Fonds De Tiroir Séparé Du Journal).
/// </summary>
internal static class Mots
{
    internal static readonly CultureInfo Francais = CultureInfo.GetCultureInfo("fr-CA");

    /// <summary>« 6 h 30 » — l'heure à la québécoise (OQLF), comme dans le reste de l'app.</summary>
    internal static string Heure(TimeOnly heure) => $"{heure.Hour} h {heure.Minute:00}";

    internal static string Duree(TimeSpan duree) => $"{(int)duree.TotalHours} h {duree.Minutes:00}";

    /// <summary>« 1er novembre », « 22 septembre » — le premier du mois est ordinal.</summary>
    internal static string DateLongue(DateOnly date) =>
        date.Day == 1
            ? $"1er {date.ToString("MMMM", Francais)}"
            : date.ToString("d MMMM", Francais);

    /// <summary>La même, avec l'année : pour ce qui est assez vieux pour qu'elle compte.</summary>
    internal static string DateAvecAnnee(DateOnly date) => $"{DateLongue(date)} {date.Year}";

    internal static string Jours(int nombre) => $"{nombre} jour{Marque(nombre)}";

    /// <summary>
    /// « de septembre », mais « d'août » : la préposition s'élide devant une voyelle.
    /// Trois mois de l'année en commencent par une, et « du mois de août » se voit à
    /// trois mètres.
    /// </summary>
    internal static string De(string mot) =>
        mot.Length > 0 && "aeiouyàâäéèêëîïôöùû".Contains(char.ToLowerInvariant(mot[0]))
            ? $"d'{mot}"
            : $"de {mot}";

    /// <summary>Le « s » du pluriel — zéro et un restent au singulier, en français.</summary>
    internal static string Marque(int nombre) => nombre > 1 ? "s" : "";

    /// <summary>« 1 240 $ » — sans cents, qui ne se lisent pas à trois mètres.</summary>
    internal static string Montant(decimal montant) => montant.ToString("C0", Francais);

    /// <summary>
    /// Un titre de tâche finit souvent par un point d'exclamation (« Boîtes! ») : le
    /// recoller à la ponctuation d'une phrase donne « Boîtes!. ».
    /// </summary>
    internal static string SansPonctuationFinale(string titre) => titre.TrimEnd('.', '!', '?', ' ');

    /// <summary>
    /// Au-delà, un titre cité dans une phrase fait déborder la phrase de la place que
    /// le consommateur lui donne.
    /// </summary>
    internal const int LongueurDeTitre = 32;

    /// <summary>
    /// Un titre saisi par le foyer, prêt à entrer dans une phrase : sans sa ponctuation
    /// finale, et <b>élagué</b> s'il est trop long, à un mot entier.
    ///
    /// <para>Un titre de soixante signes faisait couper la phrase en cours de route et
    /// lui faisait perdre sa fin — « La première le 28 septembre : Homelab — plan de
    /// migratio… ». Couper le titre plutôt que la phrase, c'est la règle du chantier :
    /// <b>élaguer, pas rapetisser</b>. Ce n'est pas une mesure en pixels mais une
    /// longueur d'écriture, comme les trente signes de la valeur courte.</para>
    /// </summary>
    internal static string TitreCourt(string titre, int maximum = LongueurDeTitre)
    {
        var propre = SansPonctuationFinale(titre);
        if (propre.Length <= maximum)
        {
            return propre;
        }
        var coupe = propre[..maximum];
        var dernierEspace = coupe.LastIndexOf(' ');
        // Un seul mot plus long que la coupe (une URL, un mot-valise) se coupe net :
        // mieux vaut un mot tronqué qu'une citation vide. Les points de suspension
        // comptent dans le maximum — sans quoi un titre d'un seul mot le dépasserait
        // du signe qui dit qu'il a été coupé.
        var garde = dernierEspace > 0 ? coupe[..dernierEspace] : coupe[..(maximum - 1)];
        // Ce qui traîne au bout de la coupe — une virgule, un tiret, un « + » de titre
        // de tâche — ne doit pas rester collé aux points de suspension.
        while (garde.Length > 0 && char.IsLetterOrDigit(garde[^1]) == false)
        {
            garde = garde[..^1];
        }
        return garde + "…";
    }
}
