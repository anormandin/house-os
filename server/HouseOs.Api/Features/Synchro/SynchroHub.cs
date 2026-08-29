using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.SignalR;

namespace HouseOs.Api.Features.Synchro;

/// <summary>
/// Hub de diffusion pur : aucune méthode client→serveur. Les clients s'y connectent
/// pour écouter « Evenement » et rien d'autre — toute écriture passe par l'API REST.
/// Authentifié via la FallbackPolicy (cookie de session, même origine que la SPA).
///
/// Le cycle de vie est journalisé : une connexion fantôme (fermée côté navigateur mais
/// pas encore expirée côté serveur) est le suspect n°1 du 503 de complétion, puisque
/// la diffusion attend tous les clients avant que la réponse ne parte. Sans ces lignes,
/// une déconnexion ratée ne laissait aucune trace.
/// </summary>
public sealed class SynchroHub(ILogger<SynchroHub> journal) : Hub
{
    /// <summary>Nom de la méthode appelée sur le client (contrat partagé avec web/src/lib/synchro.ts).</summary>
    public const string MethodeEvenement = "Evenement";

    public const string Chemin = "/hubs/synchro";

    public override Task OnConnectedAsync()
    {
        journal.LogInformation(
            "Hub synchro — connexion {ConnexionId} de {Utilisateur} ({Transport}).",
            Context.ConnectionId,
            Context.User?.Identity?.Name,
            Context.Features.Get<IHttpTransportFeature>()?.TransportType.ToString());
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception is null)
        {
            journal.LogInformation(
                "Hub synchro — déconnexion propre {ConnexionId} de {Utilisateur}.",
                Context.ConnectionId, Context.User?.Identity?.Name);
        }
        else
        {
            journal.LogWarning(
                exception,
                "Hub synchro — déconnexion en erreur {ConnexionId} de {Utilisateur}.",
                Context.ConnectionId, Context.User?.Identity?.Name);
        }
        return base.OnDisconnectedAsync(exception);
    }
}
