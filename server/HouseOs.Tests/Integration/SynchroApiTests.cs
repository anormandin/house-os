using System.Net;
using System.Net.Http.Json;
using HouseOs.Api.Features.Synchro;
using HouseOs.Api.Features.Taches;
using HouseOs.Api.Features.Zones;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace HouseOs.Tests.Integration;

/// <summary>
/// La synchro temps réel vue de l'extérieur : les écritures HTTP diffusent bien leurs
/// deux tiers, et le hub lui-même se laisse joindre par un vrai client SignalR — la
/// seule preuve que la négociation traverse l'antiforgery global et la FallbackPolicy
/// authentifiée.
/// </summary>
[Collection("integration")]
public class SynchroApiTests(HouseOsFactory factory)
{
    private static readonly DateOnly Aujourdhui = DateOnly.FromDateTime(DateTime.Now);

    private sealed record CorpsId(Guid Id);

    private async Task<(HttpClient Client, Guid OccurrenceId)> TachePrete(string titre)
    {
        var client = await factory.ClientConnecte();
        var reponse = await client.PostAsJsonAsync("/api/taches", new CreerTacheRequete(
            titre, null, Aujourdhui, null, null, null, null, null, null));
        Assert.Equal(HttpStatusCode.Created, reponse.StatusCode);

        var occurrences = await client.GetFromJsonAsync<List<OccurrenceDto>>(
            "/api/occurrences?filtre=en-attente");
        var occurrence = Assert.Single(occurrences!, o => o.Titre == titre);
        return (client, occurrence.Id);
    }

    [Fact]
    public async Task CompleterViaHttp_diffuseLesDeuxTiers()
    {
        var (client, occurrenceId) = await TachePrete($"Litière {Guid.NewGuid():N}");
        factory.Synchro.Vider();

        var reponse = await client.PostAsJsonAsync(
            $"/api/occurrences/{occurrenceId}/completer", new CompleterRequete(null));
        Assert.Equal(HttpStatusCode.NoContent, reponse.StatusCode);

        // Tier grossier : de quoi rafraîchir, sans acteur.
        Assert.Contains(factory.Synchro.Grossiers, e => e.Module == ModulesSynchro.Taches);
        // Tier fin : de quoi annoncer.
        var fin = Assert.Single(
            factory.Synchro.Fins, e => e.Genre == EvenementSynchro.GenreOccurrenceCompletee);
        Assert.Equal(EvenementSynchro.SourceWeb, fin.Source);
        Assert.Equal("Alain", fin.ActeurNom);
        Assert.StartsWith("Litière", fin.Libelle);
    }

    [Fact]
    public async Task AnnulerLaCompletion_diffuseSonPropreGenre()
    {
        var (client, occurrenceId) = await TachePrete($"Poubelles {Guid.NewGuid():N}");
        await client.PostAsJsonAsync(
            $"/api/occurrences/{occurrenceId}/completer", new CompleterRequete(null));
        factory.Synchro.Vider();

        var reponse = await client.PostAsync(
            $"/api/occurrences/{occurrenceId}/annuler-completion", null);
        Assert.Equal(HttpStatusCode.NoContent, reponse.StatusCode);

        var fin = Assert.Single(
            factory.Synchro.Fins, e => e.Genre == EvenementSynchro.GenreOccurrenceAnnulee);
        Assert.Equal("Alain", fin.ActeurNom);
    }

    [Fact]
    public async Task UneEcritureDUnAutreModule_diffuseSonModule()
    {
        // Le tier grossier n'a rien à savoir des slices : créer une zone suffit.
        var client = await factory.ClientConnecte();
        factory.Synchro.Vider();

        var reponse = await client.PostAsJsonAsync(
            "/api/zones", new { nom = $"Garage {Guid.NewGuid():N}", type = "Interieur" });
        Assert.Equal(HttpStatusCode.Created, reponse.StatusCode);

        Assert.Contains(factory.Synchro.Grossiers, e => e.Module == ModulesSynchro.Zones);
    }

    [Fact]
    public async Task UneEcritureRefusee_neDiffuseRien()
    {
        var client = await factory.ClientConnecte();
        factory.Synchro.Vider();

        var reponse = await client.PostAsJsonAsync(
            "/api/zones", new { nom = "", type = "Interieur" });

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
        Assert.Empty(factory.Synchro.Recus);
    }

    [Fact]
    public async Task UnClientConnecte_recoitLesEvenementsPousses()
    {
        // Le test qui couvre la plomberie réelle : négociation (malgré UseAntiforgery
        // global), authentification par cookie (FallbackPolicy), et livraison.
        await using var connexion = ConstruireConnexion(await factory.CookieSession());

        var recus = new List<EvenementSynchro>();
        var arrivee = new TaskCompletionSource();
        connexion.On<EvenementSynchro>(SynchroHub.MethodeEvenement, evenement =>
        {
            recus.Add(evenement);
            if (evenement.Genre == EvenementSynchro.GenreOccurrenceCompletee)
            {
                arrivee.TrySetResult();
            }
        });
        await connexion.StartAsync();
        Assert.Equal(HubConnectionState.Connected, connexion.State);

        var (auteur, occurrenceId) = await TachePrete($"Filtre {Guid.NewGuid():N}");
        await auteur.PostAsJsonAsync(
            $"/api/occurrences/{occurrenceId}/completer", new CompleterRequete(null));

        await arrivee.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Contains(recus, e => e.Module == ModulesSynchro.Taches && e.Genre is null);
        var fin = Assert.Single(recus, e => e.Genre == EvenementSynchro.GenreOccurrenceCompletee);
        Assert.Equal("Alain", fin.ActeurNom);
        Assert.Equal(1, fin.Nombre);
    }

    [Fact]
    public async Task SansCookie_leHubRefuseLaConnexion()
    {
        await using var connexion = ConstruireConnexion(cookieSession: null);

        var exception = await Assert.ThrowsAnyAsync<Exception>(() => connexion.StartAsync());

        Assert.Contains("401", exception.Message);
    }

    /// <summary>
    /// Client SignalR branché sur le TestServer : le handler en mémoire remplace le
    /// réseau, donc transport long-polling (pas de vraie socket) — ce qui teste tout
    /// de même la négociation, l'auth et la sérialisation du message.
    /// </summary>
    private HubConnection ConstruireConnexion(string? cookieSession) =>
        new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, SynchroHub.Chemin), options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
                if (cookieSession is not null)
                {
                    options.Headers["Cookie"] = cookieSession;
                }
            })
            .Build();
}
