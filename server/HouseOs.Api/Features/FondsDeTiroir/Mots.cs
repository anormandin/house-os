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

    /// <summary>Le « s » du pluriel — zéro et un restent au singulier, en français.</summary>
    internal static string Marque(int nombre) => nombre > 1 ? "s" : "";

    /// <summary>« 1 240 $ » — sans cents, qui ne se lisent pas à trois mètres.</summary>
    internal static string Montant(decimal montant) => montant.ToString("C0", Francais);

    /// <summary>
    /// Un titre de tâche finit souvent par un point d'exclamation (« Boîtes! ») : le
    /// recoller à la ponctuation d'une phrase donne « Boîtes!. ».
    /// </summary>
    internal static string SansPonctuationFinale(string titre) => titre.TrimEnd('.', '!', '?', ' ');
}
