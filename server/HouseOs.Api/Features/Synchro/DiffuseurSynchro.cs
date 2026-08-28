using Microsoft.AspNetCore.SignalR;

namespace HouseOs.Api.Features.Synchro;

/// <summary>
/// Diffusion réelle via SignalR. Avale toute exception : la synchro est un confort,
/// jamais une raison de faire échouer une écriture métier déjà commitée.
/// </summary>
public sealed class DiffuseurSynchro(
    IHubContext<SynchroHub> hub,
    ILogger<DiffuseurSynchro> journal) : IDiffuseurSynchro
{
    public async Task DiffuserAsync(EvenementSynchro evenement, CancellationToken annulation = default)
    {
        try
        {
            await hub.Clients.All.SendAsync(SynchroHub.MethodeEvenement, evenement, annulation);
        }
        catch (Exception ex)
        {
            journal.LogWarning(ex, "Diffusion synchro échouée pour le module {Module}.", evenement.Module);
        }
    }
}
