using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace HouseOs.Tests.Integration;

/// <summary>
/// Test-garde de la surface d'authentification : toutes les routes /api exigent la
/// session, et la liste des routes anonymes est EXPLICITE — un AllowAnonymous posé
/// par distraction ouvrirait une tranche entière sans qu'aucun test ne bronche.
/// </summary>
[Collection("integration")]
public class GardeAuthTests(HouseOsFactory factory)
{
    [Theory]
    [InlineData("/api/occurrences")]
    [InlineData("/api/taches/00000000-0000-0000-0000-000000000001")]
    [InlineData("/api/utilisateurs")]
    [InlineData("/api/journal/bilan?de=2026-01-01&a=2026-01-02")]
    [InlineData("/api/zones")]
    [InlineData("/api/comptes-a-rebours")]
    [InlineData("/api/equipements")]
    [InlineData("/api/documents")]
    [InlineData("/api/documents/00000000-0000-0000-0000-000000000001/fichier")]
    [InlineData("/api/documents/00000000-0000-0000-0000-000000000001/miniature")]
    [InlineData("/api/budget")]
    [InlineData("/api/budget/transactions")]
    [InlineData("/api/budget/enveloppes/00000000-0000-0000-0000-000000000001")]
    [InlineData("/api/flux-externes")]
    [InlineData("/api/meteo")]
    [InlineData("/api/phrase-du-jour")]
    [InlineData("/api/ical/mon-flux")]
    [InlineData("/api/auth/moi")]
    public async Task ToutesLesRoutesApi_SansCookie_Repondent401(string route)
    {
        var client = factory.CreateClient();

        var reponse = await client.GetAsync(route);

        Assert.Equal(HttpStatusCode.Unauthorized, reponse.StatusCode);
    }

    /// <summary>
    /// Tout ce qui répond sans session. Un ajout ici se justifie dans la spec Auth
    /// (vault), pas seulement dans le code.
    /// </summary>
    private static readonly string[] RoutesAnonymesConnues =
    [
        "/api/sante",
        "/api/auth/connexion", // on ne peut pas exiger la session pour l'ouvrir
        "/api/journal-client", // les erreurs du navigateur, écran de connexion compris
        "/ical/{jeton}.ics", // le jeton EST l'authentification
        // L'écran e-ink : l'appareil s'identifie par son jeton, le navigateur de rendu
        // lit les données du mur sans cookie.
        "/api/setup",
        "/api/display",
        "/api/log",
        "/api/affichage/{identifiant:length(6)}/{jeton:length(32)}/{fichier:regex(^[a-z0-9]{{1,32}}$)}.png",
        "/api/affichage/donnees",
        "{*path:nonfile}", // la SPA (index.html)
    ];

    [Fact]
    public void LesRoutesAnonymes_SontExactementLaListeConnue()
    {
        // Dérivé de la table de routage, pas d'un échantillon : un AllowAnonymous posé
        // par distraction casse ici.
        var anonymes = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(route => route.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            .Select(route => route.RoutePattern.RawText)
            .Distinct()
            .Order(StringComparer.Ordinal);

        Assert.Equal(RoutesAnonymesConnues.Order(StringComparer.Ordinal), anonymes);
    }

    [Theory]
    [InlineData("/api/sante")]
    [InlineData("/ical/jeton-bidon.ics")] // le jeton EST l'authentification (404 attendu)
    public async Task LesRoutesAnonymesConnues_NeRepondentPas401(string route)
    {
        var client = factory.CreateClient();

        var reponse = await client.GetAsync(route);

        Assert.NotEqual(HttpStatusCode.Unauthorized, reponse.StatusCode);
    }
}
