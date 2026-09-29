using System.Globalization;
using System.Text;

namespace HouseOs.Api.Infrastructure;

/// <summary>
/// Comparer deux titres comme un humain les lit : « Nettoyer les gouttieres » et
/// « Nettoyer les gouttières  » sont la même tâche. Même règle que
/// <c>web/src/lib/recherche.ts</c> côté navigateur — sans accents, sans casse, sans
/// blancs de trop.
/// </summary>
public static class Texte
{
    public static string Normaliser(string? texte)
    {
        if (string.IsNullOrWhiteSpace(texte))
        {
            return string.Empty;
        }
        var decompose = texte.Normalize(NormalizationForm.FormD);
        var sortie = new StringBuilder(decompose.Length);
        var blancEnAttente = false;
        foreach (var c in decompose)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }
            if (char.IsWhiteSpace(c))
            {
                blancEnAttente = sortie.Length > 0;
                continue;
            }
            if (blancEnAttente)
            {
                sortie.Append(' ');
                blancEnAttente = false;
            }
            sortie.Append(char.ToLowerInvariant(c));
        }
        return sortie.ToString();
    }
}
