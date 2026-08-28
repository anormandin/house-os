using System.Collections.Concurrent;
using HouseOs.Api.Features.Synchro;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace HouseOs.Tests.Integration;

/// <summary>
/// Décorateur du vrai diffuseur : enregistre ce qui passe ET diffuse pour de bon.
/// Remplacer le diffuseur par un simple mouchard rendrait invérifiable le chemin
/// SignalR lui-même (négociation, auth) — ici les deux restent testables.
/// </summary>
public sealed class DiffuseurEspion(
    IHubContext<SynchroHub> hub,
    ILogger<DiffuseurSynchro> journal) : IDiffuseurSynchro
{
    private readonly DiffuseurSynchro _reel = new(hub, journal);
    private readonly ConcurrentQueue<EvenementSynchro> _recus = new();

    public IReadOnlyList<EvenementSynchro> Recus => _recus.ToArray();

    /// <summary>Événements grossiers (invalidation seule) reçus depuis le dernier vidage.</summary>
    public IReadOnlyList<EvenementSynchro> Grossiers =>
        _recus.Where(e => e.Genre is null).ToArray();

    /// <summary>Événements fins (annonçables) reçus depuis le dernier vidage.</summary>
    public IReadOnlyList<EvenementSynchro> Fins =>
        _recus.Where(e => e.Genre is not null).ToArray();

    public void Vider() => _recus.Clear();

    public async Task DiffuserAsync(EvenementSynchro evenement, CancellationToken annulation = default)
    {
        _recus.Enqueue(evenement);
        await _reel.DiffuserAsync(evenement, annulation);
    }
}
