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
}
