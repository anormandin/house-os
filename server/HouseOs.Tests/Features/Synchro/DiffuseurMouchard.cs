using HouseOs.Api.Features.Synchro;

namespace HouseOs.Tests.Features.Synchro;

/// <summary>Diffuseur de test hors DI : enregistre, ne diffuse rien.</summary>
public sealed class DiffuseurMouchard : IDiffuseurSynchro
{
    public List<EvenementSynchro> Recus { get; } = [];

    public IReadOnlyList<EvenementSynchro> Grossiers => Recus.Where(e => e.Genre is null).ToList();

    public IReadOnlyList<EvenementSynchro> Fins => Recus.Where(e => e.Genre is not null).ToList();

    public Task DiffuserAsync(EvenementSynchro evenement, CancellationToken annulation = default)
    {
        Recus.Add(evenement);
        return Task.CompletedTask;
    }
}

/// <summary>Diffuseur qui échoue toujours — la synchro ne doit jamais casser une écriture.</summary>
public sealed class DiffuseurQuiEchoue : IDiffuseurSynchro
{
    public Task DiffuserAsync(EvenementSynchro evenement, CancellationToken annulation = default) =>
        throw new InvalidOperationException("hub indisponible");
}
