namespace HouseOs.Api.Features.Courriel;

public record CourrielEnDepot(string Cle, long Taille);

/// <summary>
/// Le dépôt où le Worker Cloudflare pose les courriels bruts. Abstraction sur le même
/// modèle qu'IFournisseurTransactions : R2 en prod, un dictionnaire en test — le
/// service de relève ne connaît ni S3 ni Cloudflare.
/// </summary>
public interface IDepotCourriels
{
    bool Actif { get; }

    /// <summary>Toutes les clés en attente, triées (l'horodatage ouvre la clé).</summary>
    Task<IReadOnlyList<CourrielEnDepot>> ListerAsync(CancellationToken ct);

    Task<byte[]> TelechargerAsync(string cle, CancellationToken ct);

    Task SupprimerAsync(string cle, CancellationToken ct);
}

/// <summary>Configuration absente : rien à relever, jamais d'appel réseau.</summary>
public sealed class DepotCourrielsInactif : IDepotCourriels
{
    public bool Actif => false;

    public Task<IReadOnlyList<CourrielEnDepot>> ListerAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<CourrielEnDepot>>([]);

    public Task<byte[]> TelechargerAsync(string cle, CancellationToken ct) =>
        throw new InvalidOperationException("Dépôt de courriels non configuré.");

    public Task SupprimerAsync(string cle, CancellationToken ct) =>
        throw new InvalidOperationException("Dépôt de courriels non configuré.");
}
