using HouseOs.Api.Features.Meteo;

namespace HouseOs.Tests.Features.Meteo;

/// <summary>
/// La traduction de l'archive Open-Meteo, sur fixture. C'est le seul endroit du code
/// qui connaît la forme de cette API : ce qu'on vérifie ici, c'est qu'une réponse
/// partielle ou vide donne moins de journées, jamais une ingestion cassée ni une
/// journée inventée (vault : D-2026-09-20 Normales Climatiques Depuis L'archive Open-Meteo).
/// </summary>
public class OpenMeteoArchiveTests
{
    private const string Ici = "46.8500,-71.6200";

    /// <summary>Un extrait réel de l'archive, réduit à quatre journées.</summary>
    private const string Reponse = """
        {
          "latitude": 46.85413, "longitude": -71.65048,
          "daily_units": {"temperature_2m_min": "°C", "snowfall_sum": "cm"},
          "daily": {
            "time": ["2025-09-17", "2025-09-18", "2025-09-19", "2025-09-20"],
            "temperature_2m_min": [10.7, 12.3, 6.5, 3.3],
            "temperature_2m_max": [21.2, 22.0, 14.7, 15.1],
            "precipitation_sum": [0.00, 0.30, 0.00, 0.00],
            "snowfall_sum": [0.00, 0.00, 0.00, 0.00]
          }
        }
        """;

    [Fact]
    public void Une_reponse_ordinaire_donne_une_ligne_par_journee()
    {
        var jours = OpenMeteoArchive.Normaliser(Reponse, Ici);

        Assert.Equal(4, jours.Count);
        Assert.All(jours, j => Assert.Equal(Ici, j.Coordonnees));
        var deuxieme = jours[1];
        Assert.Equal(new DateOnly(2025, 9, 18), deuxieme.Date);
        Assert.Equal(12.3, deuxieme.TemperatureMinC);
        Assert.Equal(22.0, deuxieme.TemperatureMaxC);
        Assert.Equal(0.30, deuxieme.PrecipitationMm);
        Assert.Equal(0, deuxieme.NeigeCm);
    }

    [Fact]
    public void Une_journee_sans_temperature_est_ecartee_plutot_que_comptee_a_zero()
    {
        // Les derniers jours de la fenêtre arrivent souvent vides : la réanalyse
        // accuse quelques jours de retard. Un zéro y ferait un gel qui n'a pas eu lieu,
        // et déplacerait la normale du premier gel de plusieurs semaines.
        const string trouee = """
            {"daily": {
              "time": ["2025-09-19", "2025-09-20"],
              "temperature_2m_min": [6.5, null],
              "temperature_2m_max": [14.7, null],
              "precipitation_sum": [0.00, null],
              "snowfall_sum": [0.00, null]
            }}
            """;

        var jour = Assert.Single(OpenMeteoArchive.Normaliser(trouee, Ici));

        Assert.Equal(new DateOnly(2025, 9, 19), jour.Date);
    }

    [Fact]
    public void Une_serie_plus_courte_que_les_dates_ne_fait_pas_deborder_la_lecture()
    {
        // Réponse tronquée : la neige manque pour la seconde journée. Elle vaut zéro,
        // et la lecture continue — une exception ici, c'est une année sans normales.
        const string tronquee = """
            {"daily": {
              "time": ["2025-09-19", "2025-09-20"],
              "temperature_2m_min": [6.5, 3.3],
              "temperature_2m_max": [14.7, 15.1],
              "precipitation_sum": [0.00, 0.00],
              "snowfall_sum": [0.00]
            }}
            """;

        var jours = OpenMeteoArchive.Normaliser(tronquee, Ici);

        Assert.Equal(2, jours.Count);
        Assert.Equal(0, jours[1].NeigeCm);
    }

    [Fact]
    public void Une_date_repetee_ne_sort_qu_une_fois()
    {
        // L'index unique (coordonnées, date) ferait échouer toute l'ingestion sur un
        // doublon — le même piège que l'heure murale dupliquée de l'ingestion horaire.
        const string doublon = """
            {"daily": {
              "time": ["2025-09-19", "2025-09-19"],
              "temperature_2m_min": [6.5, 6.5],
              "temperature_2m_max": [14.7, 14.7],
              "precipitation_sum": [0.00, 0.00],
              "snowfall_sum": [0.00, 0.00]
            }}
            """;

        Assert.Single(OpenMeteoArchive.Normaliser(doublon, Ici));
    }

    [Fact]
    public void Une_serie_nulle_ne_fait_pas_tomber_le_tirage_de_l_annee()
    {
        // Open-Meteo rend `null` — et non un tableau vide — quand une série entière
        // manque, et `GetArrayLength` lève dessus. Sur le tirage annuel, une exception
        // ici coûte une année de normales. Trouvé en revue de code, étape 5.
        const string sansNeige = """
            {"daily": {
              "time": ["2025-09-19"],
              "temperature_2m_min": [6.5],
              "temperature_2m_max": [14.7],
              "precipitation_sum": [0.00],
              "snowfall_sum": null
            }}
            """;

        var jour = Assert.Single(OpenMeteoArchive.Normaliser(sansNeige, Ici));

        Assert.Equal(0, jour.NeigeCm);
    }

    [Fact]
    public void Un_bloc_daily_sans_dates_donne_une_erreur_claire()
    {
        // Le bloc est là, la série de dates non : erreur lisible, pas une
        // KeyNotFoundException. Trouvé en revue de code, étape 5.
        var erreur = Assert.Throws<FormatException>(
            () => OpenMeteoArchive.Normaliser("""{"daily": {"temperature_2m_min": [6.5]}}""", Ici));

        Assert.Contains("dates", erreur.Message);
    }

    [Fact]
    public void Un_corps_sans_bloc_daily_donne_une_erreur_claire()
    {
        // Un 200 d'un proxy ou une erreur d'Open-Meteo : le worker doit journaliser
        // une erreur lisible, pas une KeyNotFoundException.
        var erreur = Assert.Throws<FormatException>(
            () => OpenMeteoArchive.Normaliser("""{"error": true, "reason": "out of range"}""", Ici));

        Assert.Contains("daily", erreur.Message);
    }

    [Fact]
    public void L_url_porte_les_coordonnees_en_invariant_et_s_arrete_avant_le_bord()
    {
        // La virgule décimale du fr-CA dans une URL donnerait une requête invalide ;
        // et demander hier rendrait une série de null (la réanalyse a du retard).
        var aujourdhui = new DateOnly(2026, 9, 20);
        var fin = OpenMeteoArchive.FinDeLArchive(aujourdhui);

        Assert.Equal(new DateOnly(2026, 9, 17), fin);

        var url = OpenMeteoArchive.Url(46.85, -71.62, fin.AddYears(-10).AddDays(1), fin, "America/Toronto");

        Assert.Contains("latitude=46.85&longitude=-71.62", url);
        Assert.Contains("start_date=2016-09-18&end_date=2026-09-17", url);
        Assert.Contains("snowfall_sum", url);
        Assert.Contains("timezone=America%2FToronto", url);
    }
}
