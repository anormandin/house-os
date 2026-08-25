using HouseOs.Api.Features.Meteo;

namespace HouseOs.Tests.Features.Meteo;

public class OpenMeteoNormalisationTests
{
    // Extrait réduit mais fidèle de la réponse `/v1/forecast` (2 heures, 2 jours),
    // avec les null que l'API renvoie sur les séries des heures passées.
    private const string Fixture = """
        {
          "latitude": 46.85,
          "longitude": -71.62,
          "timezone": "America/Toronto",
          "hourly": {
            "time": ["2026-08-24T13:00", "2026-08-24T14:00"],
            "temperature_2m": [21.4, 22.1],
            "precipitation": [0.0, 0.3],
            "precipitation_probability": [null, 40],
            "wind_speed_10m": [12.5, 18.0],
            "wind_gusts_10m": [22.0, 30.5],
            "relative_humidity_2m": [55, 60],
            "soil_moisture_0_to_1cm": [0.21, null],
            "cloud_cover": [35, 80],
            "uv_index": [5.2, 4.1],
            "weather_code": [2, 51]
          },
          "daily": {
            "time": ["2026-08-24", "2026-08-25"],
            "temperature_2m_min": [12.3, 10.1],
            "temperature_2m_max": [24.8, 19.6],
            "precipitation_sum": [0.3, 6.2],
            "precipitation_probability_max": [40, 85],
            "wind_speed_10m_max": [18.0, 25.3],
            "wind_gusts_10m_max": [30.5, 44.0],
            "uv_index_max": [5.2, 3.0],
            "weather_code": [3, 61],
            "sunrise": ["2026-08-24T06:03", "2026-08-25T06:04"],
            "sunset": ["2026-08-24T19:47", "2026-08-25T19:45"]
          }
        }
        """;

    [Fact]
    public void Normaliser_LitLesHeures()
    {
        var resultat = OpenMeteoNormalisation.Normaliser(Fixture);

        Assert.Equal(2, resultat.Heures.Count);
        var premiere = resultat.Heures[0];
        Assert.Equal(new DateTime(2026, 8, 24, 13, 0, 0), premiere.Heure);
        Assert.Equal(21.4, premiere.TemperatureC);
        Assert.Equal(0.0, premiere.PrecipitationMm);
        Assert.Equal(12.5, premiere.VentKmh);
        Assert.Equal(55, premiere.HumiditePct);
        Assert.Equal(0.21, premiere.HumiditeSol);
        Assert.Equal(35, premiere.CouvertureNuageusePct);
        Assert.Equal(5.2, premiere.IndiceUv);
        Assert.Equal(2, premiere.CodeMeteo);
        Assert.Equal(51, resultat.Heures[1].CodeMeteo);
        Assert.True(resultat.Heures[1].AnnoncePrecipitation);
    }

    [Fact]
    public void Normaliser_ToleraLesNull()
    {
        var resultat = OpenMeteoNormalisation.Normaliser(Fixture);

        // Probabilité null (heure passée) → 0 ; humidité du sol null → inconnue.
        Assert.Equal(0, resultat.Heures[0].ProbabilitePrecipitationPct);
        Assert.Equal(40, resultat.Heures[1].ProbabilitePrecipitationPct);
        Assert.Null(resultat.Heures[1].HumiditeSol);
    }

    [Fact]
    public void Normaliser_LitLesJours()
    {
        var resultat = OpenMeteoNormalisation.Normaliser(Fixture);

        Assert.Equal(2, resultat.Jours.Count);
        var demain = resultat.Jours[1];
        Assert.Equal(new DateOnly(2026, 8, 25), demain.Date);
        Assert.Equal(10.1, demain.TemperatureMinC);
        Assert.Equal(19.6, demain.TemperatureMaxC);
        Assert.Equal(6.2, demain.PrecipitationMm);
        Assert.Equal(85, demain.ProbabilitePrecipitationMaxPct);
        Assert.Equal(61, demain.CodeMeteo);
        Assert.Equal(new TimeOnly(6, 4), demain.Lever);
        Assert.Equal(new TimeOnly(19, 45), demain.Coucher);
    }

    [Fact]
    public void Normaliser_SerieAbsente_ValeurParDefaut()
    {
        const string minimal = """
            {
              "hourly": { "time": ["2026-08-24T13:00"], "temperature_2m": [20.0] },
              "daily": {
                "time": ["2026-08-24"],
                "temperature_2m_min": [10.0],
                "temperature_2m_max": [20.0],
                "sunrise": ["2026-08-24T06:03"],
                "sunset": ["2026-08-24T19:47"]
              }
            }
            """;
        var resultat = OpenMeteoNormalisation.Normaliser(minimal);

        Assert.Equal(0, resultat.Heures[0].PrecipitationMm);
        Assert.Null(resultat.Heures[0].HumiditeSol);
        Assert.Equal(0, resultat.Heures[0].CodeMeteo);
        Assert.Equal(0, resultat.Jours[0].CodeMeteo);
    }

    [Fact]
    public void Normaliser_SerieTronquee_NeLevePas()
    {
        // Une réponse coupée en vol : la série présente est plus courte que time.
        const string tronque = """
            {
              "hourly": {
                "time": ["2026-08-24T13:00", "2026-08-24T14:00"],
                "temperature_2m": [21.4]
              },
              "daily": { "time": ["2026-08-24"], "temperature_2m_max": [20.0] }
            }
            """;

        var resultat = OpenMeteoNormalisation.Normaliser(tronque);

        Assert.Equal(2, resultat.Heures.Count);
        Assert.Equal(21.4, resultat.Heures[0].TemperatureC);
        Assert.Equal(0, resultat.Heures[1].TemperatureC); // valeur par défaut, pas d'exception
    }

    [Fact]
    public void Normaliser_LeverCoucherAbsents_ValeurParDefaut()
    {
        const string sansSoleil = """
            {
              "hourly": { "time": ["2026-08-24T13:00"] },
              "daily": { "time": ["2026-08-24"] }
            }
            """;

        var resultat = OpenMeteoNormalisation.Normaliser(sansSoleil);

        Assert.Equal(TimeOnly.MinValue, resultat.Jours[0].Lever);
        Assert.Equal(TimeOnly.MinValue, resultat.Jours[0].Coucher);
    }

    [Fact]
    public void Normaliser_CorpsSansHourly_ErreurClaire()
    {
        // 200 avec un corps d'erreur Open-Meteo (ou la page d'un proxy) : erreur
        // explicite, jamais une KeyNotFoundException.
        var exception = Assert.Throws<FormatException>(() =>
            OpenMeteoNormalisation.Normaliser("""{"error": true, "reason": "invalid"}"""));

        Assert.Contains("hourly", exception.Message);
    }

    [Fact]
    public void Normaliser_HeureMuraleDupliquee_EstDedoublonnee()
    {
        // Nuit du retour à l'heure normale : 01:00 existe deux fois en heure murale —
        // sans dédoublonnage, l'index unique ferait échouer (et geler) l'ingestion.
        const string retourHeureNormale = """
            {
              "hourly": {
                "time": ["2026-11-01T00:00", "2026-11-01T01:00", "2026-11-01T01:00", "2026-11-01T02:00"],
                "temperature_2m": [4.0, 3.5, 3.0, 2.5]
              },
              "daily": { "time": ["2026-11-01"], "temperature_2m_max": [6.0] }
            }
            """;

        var resultat = OpenMeteoNormalisation.Normaliser(retourHeureNormale);

        Assert.Equal(3, resultat.Heures.Count);
        Assert.Equal(resultat.Heures.Count, resultat.Heures.Select(h => h.Heure).Distinct().Count());
        // La première des deux 01:00 est conservée.
        Assert.Equal(3.5, resultat.Heures.Single(h => h.Heure.Hour == 1).TemperatureC);
    }
}
