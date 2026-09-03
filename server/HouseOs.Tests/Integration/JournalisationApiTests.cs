using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace HouseOs.Tests.Integration;

/// <summary>
/// La chaîne de corrélation vue du client : chaque réponse porte son identifiant de
/// trace, et le navigateur peut déposer sa piste de session. Sans ces deux-là, un
/// incident reste un récit — c'est ce qui a rendu le 503 de complétion inenquêtable.
/// </summary>
[Collection("integration")]
public class JournalisationApiTests(HouseOsFactory factory)
{
    [Fact]
    public async Task ToutesLesReponses_PortentUnIdentifiantDeTrace()
    {
        var client = factory.CreateClient();

        var reponse = await client.GetAsync("/api/sante");

        var trace = Assert.Single(reponse.Headers.GetValues("X-Trace-Id"));
        Assert.False(string.IsNullOrWhiteSpace(trace));
    }

    [Fact]
    public async Task UneReponseEnErreur_PorteAussiSonIdentifiantDeTrace()
    {
        // Le cas qui compte : c'est quand ça casse qu'Alain a besoin de la référence.
        var client = factory.CreateClient();

        var reponse = await client.GetAsync("/api/taches");

        Assert.Equal(HttpStatusCode.Unauthorized, reponse.StatusCode);
        Assert.Single(reponse.Headers.GetValues("X-Trace-Id"));
    }

    [Fact]
    public async Task UnParametreQuiNeSeLiePas_EstUne400PasUne500()
    {
        // « ?date=hier » est la faute du client : le filet d'exceptions le dit tel quel
        // au lieu de fabriquer une « Erreur serveur » et une fausse alerte dans Seq.
        var client = await factory.ClientConnecte();

        var reponse = await client.GetAsync("/api/occurrences?filtre=aujourdhui&date=hier");

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
        var corps = await reponse.Content.ReadAsStringAsync();
        Assert.Contains("Requête invalide", corps);
        Assert.Single(reponse.Headers.GetValues("X-Trace-Id"));
    }

    [Fact]
    public async Task UnRefusDeValidation_PorteLIdentifiantDeTraceDansLeCorps()
    {
        var client = await factory.ClientConnecte();

        var reponse = await client.PostAsJsonAsync(
            "/api/zones", new { nom = "", type = (string?)null, ordre = (int?)null });

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
        var probleme = await reponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.True(probleme!.ContainsKey("traceId"));
    }

    [Fact]
    public async Task JournalClient_AccepteUnLotSansAuthentification()
    {
        // Anonyme par nécessité : une panne sur l'écran de connexion doit pouvoir
        // se signaler, sinon c'est justement le cas le plus opaque qu'on rate.
        var client = factory.CreateClient();

        var reponse = await client.PostAsJsonAsync("/api/journal-client", new
        {
            sessionId = "s-1",
            origine = "http://localhost:5173",
            evenements = new[]
            {
                new { niveau = "error", categorie = "Hub", message = "Connexion refusée", url = "/" },
            },
        });

        Assert.Equal(HttpStatusCode.Accepted, reponse.StatusCode);
    }

    [Fact]
    public async Task JournalClient_RefuseUnCorpsSurdimensionne()
    {
        var client = factory.CreateClient();
        var enorme = new string('x', 128 * 1024);
        var contenu = new StringContent(
            $$"""{"sessionId":"s","evenements":[{"message":"{{enorme}}"}]}""",
            Encoding.UTF8,
            "application/json");

        var reponse = await client.PostAsync("/api/journal-client", contenu);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, reponse.StatusCode);
    }

    [Fact]
    public async Task JournalClient_RefuseUnCorpsIllisible()
    {
        var client = factory.CreateClient();
        var contenu = new StringContent("{pas du json", Encoding.UTF8, "application/json");

        var reponse = await client.PostAsync("/api/journal-client", contenu);

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
    }

    [Fact]
    public async Task JournalClient_TolereUnLotAuDelaDeLaBorneSansEchouer()
    {
        // 200 évènements pour une borne de 50 : on tronque, on n'échoue pas — un
        // journal qui refuse est un journal qui perd l'incident.
        var client = factory.CreateClient();
        var evenements = Enumerable.Range(0, 200)
            .Select(i => new { niveau = "info", categorie = "Api", message = $"évènement {i}", url = "/" })
            .ToArray();

        var reponse = await client.PostAsJsonAsync(
            "/api/journal-client", new { sessionId = "s-2", origine = "http://localhost", evenements });

        Assert.Equal(HttpStatusCode.Accepted, reponse.StatusCode);
    }
}
