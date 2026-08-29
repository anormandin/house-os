using System.Diagnostics;
using Microsoft.AspNetCore.SignalR;

namespace HouseOs.Api.Features.Synchro;

/// <summary>
/// Diffusion réelle via SignalR. Avale toute exception : la synchro est un confort,
/// jamais une raison de faire échouer une écriture métier déjà commitée.
///
/// Mesuré, parce que cet envoi se produit DANS la requête, après le commit et avant
/// l'écriture du 204 : un client mort mais pas encore détecté peut y faire attendre
/// tout le monde. C'est la première hypothèse du 503 intermittent de complétion, et la
/// durée ci-dessous est ce qui la confirmera ou l'écartera.
/// </summary>
public sealed class DiffuseurSynchro(
    IHubContext<SynchroHub> hub,
    ILogger<DiffuseurSynchro> journal) : IDiffuseurSynchro
{
    /// <summary>Au-delà, la diffusion retarde visiblement la réponse : Warning.</summary>
    internal const int SeuilLenteurMs = 500;

    public async Task DiffuserAsync(EvenementSynchro evenement, CancellationToken annulation = default)
    {
        var chrono = Stopwatch.StartNew();
        try
        {
            await hub.Clients.All.SendAsync(SynchroHub.MethodeEvenement, evenement, annulation);
            chrono.Stop();

            if (chrono.ElapsedMilliseconds >= SeuilLenteurMs)
            {
                journal.LogWarning(
                    "Diffusion synchro lente — {Module}/{Genre} en {DureeMs} ms (source {Source}).",
                    evenement.Module, evenement.Genre, chrono.ElapsedMilliseconds, evenement.Source);
                return;
            }

            journal.LogDebug(
                "Diffusion synchro — {Module}/{Genre} en {DureeMs} ms (source {Source}).",
                evenement.Module, evenement.Genre, chrono.ElapsedMilliseconds, evenement.Source);
        }
        catch (OperationCanceledException ex)
        {
            // Distinct d'une panne : la requête porteuse a été abandonnée pendant
            // qu'on diffusait. Exactement le symptôme cherché — écriture durable,
            // réponse jamais reçue.
            journal.LogWarning(
                ex,
                "Diffusion synchro annulée — {Module}/{Genre} après {DureeMs} ms.",
                evenement.Module, evenement.Genre, chrono.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            journal.LogWarning(
                ex,
                "Diffusion synchro échouée pour le module {Module} après {DureeMs} ms.",
                evenement.Module, chrono.ElapsedMilliseconds);
        }
    }
}
