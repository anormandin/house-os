using Microsoft.AspNetCore.SignalR;

namespace HouseOs.Api.Features.Synchro;

/// <summary>
/// Hub de diffusion pur : aucune méthode client→serveur. Les clients s'y connectent
/// pour écouter « Evenement » et rien d'autre — toute écriture passe par l'API REST.
/// Authentifié via la FallbackPolicy (cookie de session, même origine que la SPA).
/// </summary>
public sealed class SynchroHub : Hub
{
    /// <summary>Nom de la méthode appelée sur le client (contrat partagé avec web/src/lib/synchro.ts).</summary>
    public const string MethodeEvenement = "Evenement";

    public const string Chemin = "/hubs/synchro";
}
