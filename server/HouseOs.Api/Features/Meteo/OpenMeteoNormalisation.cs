using System.Text.Json;
using HouseOs.Api.Domaine.Meteo;

namespace HouseOs.Api.Features.Meteo;

/// <summary>Traduit le JSON d'Open-Meteo `/v1/forecast` vers le modèle normalisé.
/// Seul endroit du code qui connaît la forme de l'API
/// (D-2026-08-24 Météo Open-Meteo). Pur et testé sur fixture, sans réseau.</summary>
public static class OpenMeteoNormalisation
{
    public record Resultat(List<PrevisionHoraire> Heures, List<PrevisionQuotidienne> Jours);

    public static Resultat Normaliser(string json)
    {
        using var document = JsonDocument.Parse(json);
        var racine = document.RootElement;
        return new Resultat(
            NormaliserHoraire(racine.GetProperty("hourly")),
            NormaliserQuotidien(racine.GetProperty("daily")));
    }

    private static List<PrevisionHoraire> NormaliserHoraire(JsonElement horaire)
    {
        var temps = horaire.GetProperty("time");
        var heures = new List<PrevisionHoraire>(temps.GetArrayLength());
        for (var i = 0; i < temps.GetArrayLength(); i++)
        {
            heures.Add(new PrevisionHoraire
            {
                Heure = DateTime.Parse(temps[i].GetString()!),
                TemperatureC = Nombre(horaire, "temperature_2m", i) ?? 0,
                PrecipitationMm = Nombre(horaire, "precipitation", i) ?? 0,
                ProbabilitePrecipitationPct = (int)(Nombre(horaire, "precipitation_probability", i) ?? 0),
                VentKmh = Nombre(horaire, "wind_speed_10m", i) ?? 0,
                RafalesKmh = Nombre(horaire, "wind_gusts_10m", i) ?? 0,
                HumiditePct = (int)(Nombre(horaire, "relative_humidity_2m", i) ?? 0),
                HumiditeSol = Nombre(horaire, "soil_moisture_0_to_1cm", i),
                CouvertureNuageusePct = (int)(Nombre(horaire, "cloud_cover", i) ?? 0),
                IndiceUv = Nombre(horaire, "uv_index", i) ?? 0,
            });
        }
        return heures;
    }

    private static List<PrevisionQuotidienne> NormaliserQuotidien(JsonElement quotidien)
    {
        var temps = quotidien.GetProperty("time");
        var jours = new List<PrevisionQuotidienne>(temps.GetArrayLength());
        for (var i = 0; i < temps.GetArrayLength(); i++)
        {
            jours.Add(new PrevisionQuotidienne
            {
                Date = DateOnly.Parse(temps[i].GetString()!),
                TemperatureMinC = Nombre(quotidien, "temperature_2m_min", i) ?? 0,
                TemperatureMaxC = Nombre(quotidien, "temperature_2m_max", i) ?? 0,
                PrecipitationMm = Nombre(quotidien, "precipitation_sum", i) ?? 0,
                ProbabilitePrecipitationMaxPct = (int)(Nombre(quotidien, "precipitation_probability_max", i) ?? 0),
                VentMaxKmh = Nombre(quotidien, "wind_speed_10m_max", i) ?? 0,
                RafalesMaxKmh = Nombre(quotidien, "wind_gusts_10m_max", i) ?? 0,
                IndiceUvMax = Nombre(quotidien, "uv_index_max", i) ?? 0,
                CodeMeteo = (int)(Nombre(quotidien, "weather_code", i) ?? 0),
                Lever = HeureLocale(quotidien, "sunrise", i),
                Coucher = HeureLocale(quotidien, "sunset", i),
            });
        }
        return jours;
    }

    // Les séries d'Open-Meteo contiennent des null (ex. probabilité sur les heures
    // passées) : on lit défensivement.
    private static double? Nombre(JsonElement element, string serie, int index)
    {
        if (element.TryGetProperty(serie, out var valeurs) == false)
        {
            return null;
        }
        var valeur = valeurs[index];
        return valeur.ValueKind == JsonValueKind.Number ? valeur.GetDouble() : null;
    }

    private static TimeOnly HeureLocale(JsonElement element, string serie, int index)
    {
        var valeur = element.GetProperty(serie)[index].GetString();
        return valeur is null ? TimeOnly.MinValue : TimeOnly.FromDateTime(DateTime.Parse(valeur));
    }
}
