using System.Collections.Concurrent;
using HouseOs.Api.Features.Courriel;

namespace HouseOs.Tests.Features.Courriel;

/// <summary>Le dépôt en mémoire : un dictionnaire clé → octets, plus le journal des
/// suppressions — ce que le service doit avoir fait au dépôt est vérifiable.</summary>
public sealed class DepotCourrielsFictif : IDepotCourriels
{
    private readonly ConcurrentDictionary<string, byte[]> _objets = new();
    private int _compteur;

    public bool Actif { get; set; } = true;

    /// <summary>Quand vrai, SupprimerAsync lève — simule un R2 qui refuse l'effacement.</summary>
    public bool EchouerSuppression { get; set; }

    public List<string> Supprimes { get; } = [];

    public IReadOnlyCollection<string> Cles => _objets.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();

    public string Deposer(byte[] eml, string? cle = null)
    {
        cle ??= $"entrants/2026-09-02T10-00-{Interlocked.Increment(ref _compteur):D2}-{Guid.NewGuid():N}.eml";
        _objets[cle] = eml;
        return cle;
    }

    public void Vider()
    {
        _objets.Clear();
        Supprimes.Clear();
    }

    public Task<IReadOnlyList<CourrielEnDepot>> ListerAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<CourrielEnDepot>>(
            _objets.OrderBy(o => o.Key, StringComparer.Ordinal)
                .Select(o => new CourrielEnDepot(o.Key, o.Value.Length))
                .ToList());

    public Task<byte[]> TelechargerAsync(string cle, CancellationToken ct) =>
        Task.FromResult(_objets[cle]);

    public Task SupprimerAsync(string cle, CancellationToken ct)
    {
        if (EchouerSuppression)
        {
            throw new IOException("R2 indisponible");
        }
        _objets.TryRemove(cle, out _);
        Supprimes.Add(cle);
        return Task.CompletedTask;
    }
}

/// <summary>Enrichisseur de test : renvoie une proposition fixée d'avance (ou null).</summary>
public sealed class EnrichisseurFictif : IEnrichisseurCourriel
{
    public PropositionLlm? Proposition { get; set; }
    public int Appels { get; private set; }
    public ContexteEnrichissement? DernierContexte { get; private set; }

    public Task<PropositionLlm?> ProposerAsync(ContexteEnrichissement contexte, CancellationToken ct)
    {
        Appels++;
        DernierContexte = contexte;
        return Task.FromResult(Proposition);
    }
}
