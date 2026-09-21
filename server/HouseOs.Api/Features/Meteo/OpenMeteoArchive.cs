using System.Globalization;
using System.Text.Json;
using HouseOs.Api.Domaine.Meteo;

namespace HouseOs.Api.Features.Meteo;

/// <summary>
/// L'archive Open-Meteo (réanalyse ERA5) : l'URL du tirage annuel et la traduction de
/// sa réponse vers <see cref="JourDeClimat"/>. Seul endroit du code qui connaît la
/// forme de cette API, comme <see cref="OpenMeteoNormalisation"/> l'est pour les
/// prévisions (vault : D-2026-09-20 Normales Climatiques Depuis L'archive Open-Meteo).
/// Pur et testé sur fixture, sans réseau.
/// </summary>
public static class OpenMeteoArchive
{
    /// <summary>
    /// La réanalyse accuse quelques jours de retard. On s'arrête donc avant le bord :
    /// demander hier rendrait une série de <c>null</c>, pas une erreur.
    /// </summary>
    public const int RetardDeLArchiveJours = 3;

    /// <summary>Le dernier jour qu'on peut espérer lire depuis l'archive.</summary>
    public static DateOnly FinDeLArchive(DateOnly aujourdhui) =>
        aujourdhui.AddDays(-RetardDeLArchiveJours);

    public static string Url(double latitude, double longitude, DateOnly debut, DateOnly fin, string fuseau) =>
        "https://archive-api.open-meteo.com/v1/archive"
        + $"?latitude={latitude.ToString(CultureInfo.InvariantCulture)}"
        + $"&longitude={longitude.ToString(CultureInfo.InvariantCulture)}"
        + $"&start_date={debut:yyyy-MM-dd}&end_date={fin:yyyy-MM-dd}"
        + "&daily=temperature_2m_min,temperature_2m_max,precipitation_sum,snowfall_sum"
        + $"&timezone={Uri.EscapeDataString(fuseau)}";

    /// <summary>
    /// Les journées lisibles de la réponse. Une journée sans température est
    /// <b>écartée</b> plutôt que comptée à zéro : les derniers jours de la fenêtre
    /// arrivent souvent vides, et un zéro y ferait un gel qui n'a pas eu lieu.
    /// </summary>
    public static List<JourDeClimat> Normaliser(string json, string coordonnees)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.TryGetProperty("daily", out var quotidien) == false)
        {
            throw new FormatException("Réponse de l'archive Open-Meteo sans bloc daily.");
        }

        if (Serie(quotidien, "time") is not { } temps)
        {
            throw new FormatException("Réponse de l'archive Open-Meteo sans série de dates.");
        }
        var jours = new List<JourDeClimat>(temps.GetArrayLength());
        var vues = new HashSet<DateOnly>();
        for (var i = 0; i < temps.GetArrayLength(); i++)
        {
            var min = Nombre(quotidien, "temperature_2m_min", i);
            var max = Nombre(quotidien, "temperature_2m_max", i);
            if (min is null || max is null)
            {
                continue;
            }
            var date = DateOnly.Parse(temps[i].GetString()!, CultureInfo.InvariantCulture);
            // L'index unique (coordonnées, date) ferait échouer toute l'ingestion sur
            // un doublon — et une ingestion qui échoue, c'est une année sans normales.
            if (vues.Add(date) == false)
            {
                continue;
            }
            jours.Add(new JourDeClimat
            {
                Coordonnees = coordonnees,
                Date = date,
                TemperatureMinC = min.Value,
                TemperatureMaxC = max.Value,
                PrecipitationMm = Nombre(quotidien, "precipitation_sum", i) ?? 0,
                NeigeCm = Nombre(quotidien, "snowfall_sum", i) ?? 0,
            });
        }
        return jours;
    }

    private static double? Nombre(JsonElement element, string serie, int index)
    {
        if (Serie(element, serie) is not { } valeurs || index >= valeurs.GetArrayLength())
        {
            return null;
        }
        var valeur = valeurs[index];
        return valeur.ValueKind == JsonValueKind.Number ? valeur.GetDouble() : null;
    }

    /// <summary>
    /// Une série du bloc, si c'en est une. Une propriété présente mais nulle — ce que
    /// rend Open-Meteo quand la série entière manque — n'est pas un tableau, et la
    /// lire comme tel ferait tomber le tirage annuel entier sur une réponse que le
    /// reste du code sait très bien traiter comme incomplète.
    /// </summary>
    private static JsonElement? Serie(JsonElement element, string nom) =>
        element.TryGetProperty(nom, out var valeurs) && valeurs.ValueKind == JsonValueKind.Array
            ? valeurs
            : null;
}
