using System.Net;
using System.Net.Http.Json;
using HouseOs.Api.Features.Auth;
using HouseOs.Api.Features.Taches;

namespace HouseOs.Tests.Integration;

/// <summary>
/// Le flux iCal par personne — le seul endpoint anonyme qui expose des données du
/// foyer : filtre inter-personnes, échappement, bornes des comptes à rebours.
/// </summary>
[Collection("integration")]
public class FluxIcalApiTests(HouseOsFactory factory)
{
    private static readonly DateOnly Aujourdhui = DateOnly.FromDateTime(DateTime.Now);

    private sealed record MonFlux(string Chemin, string? UrlPublique);
    private sealed record CorpsId(Guid Id);

    private static async Task<string> FluxDe(HttpClient client)
    {
        var monFlux = await client.GetFromJsonAsync<MonFlux>("/api/ical/mon-flux");
        return monFlux!.Chemin;
    }

    private static async Task<Guid> CreerTache(
        HttpClient client, string titre, Guid? assigneAId, DateOnly? echeance)
    {
        var reponse = await client.PostAsJsonAsync("/api/taches", new CreerTacheRequete(
            titre, null, echeance, assigneAId, null, null, null, null));
        Assert.Equal(HttpStatusCode.Created, reponse.StatusCode);
        return (await reponse.Content.ReadFromJsonAsync<CorpsId>())!.Id;
    }

    [Fact]
    public async Task RotationDuJeton_RevoqueLAncienneUrl_EtSertLaNouvelle()
    {
        var client = await factory.ClientConnecte("ariane");
        var ancien = await client.GetFromJsonAsync<MonFlux>("/api/ical/mon-flux");

        var rotation = await client.PostAsync("/api/ical/rotation", null);
        rotation.EnsureSuccessStatusCode();
        var nouveau = await rotation.Content.ReadFromJsonAsync<MonFlux>();

        // La rotation est la seule révocation possible d'un jeton fuité : l'ancien
        // flux doit mourir immédiatement, le nouveau servir tout de suite.
        Assert.NotEqual(ancien!.Chemin, nouveau!.Chemin);
        var anonyme = factory.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await anonyme.GetAsync(ancien.Chemin)).StatusCode);
        (await anonyme.GetAsync(nouveau.Chemin)).EnsureSuccessStatusCode();

        var relu = await client.GetFromJsonAsync<MonFlux>("/api/ical/mon-flux");
        Assert.Equal(nouveau.Chemin, relu!.Chemin);
    }

    [Fact]
    public async Task SansConfigPublique_MonFluxNaPasDUrlPublique()
    {
        var client = await factory.ClientConnecte();

        var monFlux = await client.GetFromJsonAsync<MonFlux>("/api/ical/mon-flux");

        Assert.Null(monFlux!.UrlPublique);
    }

    [Fact]
    public async Task JetonInconnu_Repond404()
    {
        var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync("/ical/jeton-invente.ics")).StatusCode);
    }

    [Fact]
    public async Task LeFluxDAlain_ExclutLesOccurrencesDAriane_MaisIncluLesNonAssignees()
    {
        var client = await factory.ClientConnecte();
        var utilisateurs = await client.GetFromJsonAsync<List<UtilisateurDto>>("/api/utilisateurs");
        var alain = utilisateurs!.Single(u => u.NomUtilisateur == "alain");
        var ariane = utilisateurs.Single(u => u.NomUtilisateur == "ariane");

        var marqueur = Guid.NewGuid().ToString("N");
        await CreerTache(client, $"Pour Alain {marqueur}", alain.Id, Aujourdhui.AddDays(1));
        await CreerTache(client, $"Pour Ariane {marqueur}", ariane.Id, Aujourdhui.AddDays(1));
        await CreerTache(client, $"Pour le foyer {marqueur}", null, Aujourdhui.AddDays(1));

        var ics = await factory.CreateClient().GetStringAsync(await FluxDe(client));

        // Une inversion de ce filtre exposerait le calendrier de l'autre personne.
        Assert.Contains($"Pour Alain {marqueur}", ics);
        Assert.Contains($"Pour le foyer {marqueur}", ics);
        Assert.DoesNotContain($"Pour Ariane {marqueur}", ics);
    }

    [Fact]
    public async Task LesOccurrencesSansEcheance_SontExcluesSansPlanter()
    {
        var client = await factory.ClientConnecte();
        var marqueur = Guid.NewGuid().ToString("N");
        await CreerTache(client, $"Sans échéance {marqueur}", null, null);

        var reponse = await factory.CreateClient().GetAsync(await FluxDe(client));

        reponse.EnsureSuccessStatusCode();
        Assert.DoesNotContain(marqueur, await reponse.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task LesTitresAvecPonctuationIcs_SontEchappes()
    {
        var client = await factory.ClientConnecte();
        var marqueur = Guid.NewGuid().ToString("N");
        await CreerTache(client, $"Épicerie, IGA; 2 sacs {marqueur}", null, Aujourdhui.AddDays(1));

        var ics = await factory.CreateClient().GetStringAsync(await FluxDe(client));

        // Sans échappement, la virgule et le point-virgule cassent le VEVENT et
        // l'abonnement du téléphone échoue en entier.
        Assert.Contains(@"Épicerie\, IGA\; 2 sacs", ics);
    }

    [Fact]
    public async Task LeCompteAReboursDuJourJ_EstEncoreDansLeFlux_LesPassesNon()
    {
        var client = await factory.ClientConnecte();
        var marqueur = Guid.NewGuid().ToString("N");
        foreach (var (suffixe, date) in new[]
        {
            ("jour J", Aujourdhui),
            ("passé", Aujourdhui.AddDays(-1)),
            ("à venir", Aujourdhui.AddDays(30)),
        })
        {
            var creation = await client.PostAsJsonAsync("/api/comptes-a-rebours",
                new { titre = $"{suffixe} {marqueur}", dateCible = date.ToString("yyyy-MM-dd") });
            creation.EnsureSuccessStatusCode();
        }

        var ics = await factory.CreateClient().GetStringAsync(await FluxDe(client));

        // La borne est inclusive : le jour du déménagement doit rester au calendrier.
        Assert.Contains($"jour J {marqueur}", ics);
        Assert.Contains($"à venir {marqueur}", ics);
        Assert.DoesNotContain($"passé {marqueur}", ics);
    }
}
