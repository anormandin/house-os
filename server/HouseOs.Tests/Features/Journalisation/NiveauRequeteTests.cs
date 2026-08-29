using HouseOs.Api.Infrastructure.Journalisation;
using Microsoft.AspNetCore.Http;
using Serilog.Events;

namespace HouseOs.Tests.Features.Journalisation;

/// <summary>
/// La table de niveaux des lignes de requête. C'est elle qui décide si Seq reste
/// lisible : le healthcheck frappe /api/sante toutes les 30 s en prod, et une page
/// tire des dizaines d'assets. Si ce bruit sort en Information, l'incident qu'on
/// attend se noie dedans — c'était exactement l'état d'avant.
/// </summary>
public class NiveauRequeteTests
{
    private const int Seuil = 1000;

    private static LogEventLevel Niveau(
        string chemin, int statut, double ecouleMs = 5, Exception? exception = null) =>
        JournalisationExtensions.NiveauRequete(
            new PathString(chemin), statut, ecouleMs, exception, Seuil);

    [Theory]
    [InlineData("/api/sante")]
    [InlineData("/assets/index-abc123.js")]
    [InlineData("/logo.svg")]
    [InlineData("/manifest.webmanifest")]
    public void Bruit_de_fond_en_succes_reste_sous_information(string chemin) =>
        Assert.Equal(LogEventLevel.Verbose, Niveau(chemin, StatusCodes.Status200OK));

    [Fact]
    public void Un_geste_normal_sort_en_information() =>
        Assert.Equal(
            LogEventLevel.Information,
            Niveau("/api/occurrences/1/completer", StatusCodes.Status204NoContent));

    [Fact]
    public void Une_requete_lente_alerte() =>
        Assert.Equal(
            LogEventLevel.Warning,
            Niveau("/api/occurrences/1/completer", StatusCodes.Status204NoContent, ecouleMs: 1500));

    [Fact]
    public void Un_websocket_de_longue_duree_n_alerte_pas() =>
        // Une connexion au hub vit aussi longtemps que l'onglet : la juger sur sa
        // durée ferait de chaque session normale une alerte.
        Assert.Equal(
            LogEventLevel.Information,
            Niveau("/hubs/synchro", StatusCodes.Status101SwitchingProtocols, ecouleMs: 3_600_000));

    [Fact]
    public void Un_refus_sort_en_warning() =>
        Assert.Equal(
            LogEventLevel.Warning,
            Niveau("/api/auth/connexion", StatusCodes.Status401Unauthorized));

    [Fact]
    public void Une_panne_serveur_sort_en_erreur() =>
        Assert.Equal(
            LogEventLevel.Error,
            Niveau("/api/occurrences/1/completer", StatusCodes.Status503ServiceUnavailable));

    [Fact]
    public void Le_503_du_healthcheck_reste_une_erreur_malgre_le_filtre_de_bruit() =>
        // Le filtre de bruit ne doit jamais avaler un échec : une base morte est
        // exactement ce qu'on veut voir.
        Assert.Equal(
            LogEventLevel.Error,
            Niveau("/api/sante", StatusCodes.Status503ServiceUnavailable));

    [Fact]
    public void Une_exception_sort_en_erreur_meme_avec_un_statut_de_succes() =>
        Assert.Equal(
            LogEventLevel.Error,
            Niveau("/api/taches", StatusCodes.Status200OK, exception: new InvalidOperationException()));
}
