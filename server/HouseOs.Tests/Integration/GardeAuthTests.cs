using System.Net;

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
