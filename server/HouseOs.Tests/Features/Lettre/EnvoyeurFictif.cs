using HouseOs.Api.Features.Lettre;

namespace HouseOs.Tests.Features.Lettre;

/// <summary>L'envoyeur en mémoire : ce qui est parti se relit, et un échec se commande —
/// la suite de tests n'ouvre jamais de connexion SMTP.</summary>
public sealed class EnvoyeurFictif : IEnvoyeurDeCourriel
{
    public bool Actif { get; set; } = true;

    /// <summary>Quand vrai, EnvoyerAsync lève — simule un serveur qui refuse.</summary>
    public bool Echouer { get; set; }

    public List<CourrielAEnvoyer> Envoyes { get; } = [];

    public Task EnvoyerAsync(CourrielAEnvoyer courriel, CancellationToken ct)
    {
        if (Echouer)
        {
            throw new InvalidOperationException("SMTP fictif : refus commandé.");
        }
        Envoyes.Add(courriel);
        return Task.CompletedTask;
    }
}
