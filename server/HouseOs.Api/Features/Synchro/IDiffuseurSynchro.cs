namespace HouseOs.Api.Features.Synchro;

/// <summary>
/// Couture de diffusion : permet aux tests d'observer les événements sans monter un
/// vrai WebSocket, et garde SignalR hors des slices métier.
/// </summary>
public interface IDiffuseurSynchro
{
    /// <summary>Diffuse à tous les clients connectés. Ne lève jamais.</summary>
    Task DiffuserAsync(EvenementSynchro evenement, CancellationToken annulation = default);
}
