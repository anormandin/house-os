using ModelContextProtocol;

namespace HouseOs.Api.Features.Mcp;

/// <summary>Conversions communes des paramètres d'outils MCP.</summary>
public static class Conversions
{
    /// <summary>Parse une date « YYYY-MM-DD » ; null/vide → null ; invalide → McpException.</summary>
    public static DateOnly? ParserDate(string? valeur, string champ)
    {
        if (string.IsNullOrWhiteSpace(valeur))
        {
            return null;
        }
        if (DateOnly.TryParseExact(valeur.Trim(), "yyyy-MM-dd", out var date))
        {
            return date;
        }
        throw new McpException(
            $"{champ} invalide : '{valeur}' — format attendu YYYY-MM-DD (ex. 2026-10-06).");
    }

    /// <summary>
    /// Normalise le paramètre action des outils gerer_* : casse et espaces tolérés,
    /// comme agirComme — un aller-retour de correction coûte cher au client LLM.
    /// </summary>
    public static string NormaliserAction(string? action) =>
        action?.Trim().ToLowerInvariant() ?? string.Empty;

    /// <summary>Parse un nom d'enum en tolérant la casse, en refusant les valeurs numériques.</summary>
    public static bool ParserEnum<T>(string valeur, out T resultat) where T : struct, Enum =>
        Enum.TryParse(valeur, ignoreCase: true, out resultat)
        && char.IsDigit(valeur.Trim()[0]) == false
        && Enum.IsDefined(resultat);
}
