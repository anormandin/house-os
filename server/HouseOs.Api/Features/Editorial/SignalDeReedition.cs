namespace HouseOs.Api.Features.Editorial;

/// <summary>
/// Le coup de coude du chemin de requête au service de fond : « une édition attend
/// l'éditorialiste ». Le rendu ne fait jamais l'appel LLM lui-même
/// (D-2026-09-20 Une Édition Par Jour Matérialisée) ; il pose un gabarit, lève le
/// drapeau sur l'édition, et réveille le service, qui repasse avec Opus.
/// </summary>
public sealed class SignalDeReedition
{
    private readonly SemaphoreSlim _semaphore = new(0, 1);

    public void Demander()
    {
        try
        {
            _semaphore.Release();
        }
        catch (SemaphoreFullException)
        {
            // Déjà demandé, pas encore consommé : une demande suffit.
        }
    }

    /// <summary>Vrai si le signal a été levé avant la fin du délai.</summary>
    public Task<bool> AttendreAsync(TimeSpan delai, CancellationToken ct) =>
        _semaphore.WaitAsync(delai, ct);
}
