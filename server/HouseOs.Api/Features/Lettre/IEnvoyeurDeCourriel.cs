namespace HouseOs.Api.Features.Lettre;

public sealed record Destinataire(string Nom, string Adresse);

/// <summary>Un courriel prêt à partir : les deux corps sont toujours fournis, le client
/// de courriel choisit.</summary>
public sealed record CourrielAEnvoyer(
    IReadOnlyList<Destinataire> Destinataires,
    string Sujet,
    string Texte,
    string Html);

/// <summary>
/// La seule porte de sortie du courriel de la maison — remplaçable en test par un fictif
/// à compteur : la suite de tests n'ouvre jamais de connexion réseau
/// (D-2026-09-21 Courriel Sortant Par SMTP).
/// </summary>
public interface IEnvoyeurDeCourriel
{
    /// <summary>Faux sans configuration SMTP : la lettre est alors composée mais ne part
    /// pas, et ce n'est pas un échec.</summary>
    bool Actif { get; }

    Task EnvoyerAsync(CourrielAEnvoyer courriel, CancellationToken ct);
}

/// <summary>Sans hôte SMTP : ne part rien, ne promet rien.</summary>
public sealed class EnvoyeurInactif : IEnvoyeurDeCourriel
{
    public bool Actif => false;

    public Task EnvoyerAsync(CourrielAEnvoyer courriel, CancellationToken ct) =>
        throw new InvalidOperationException("Envoi de courriel non configuré (Lettre:Smtp).");
}
