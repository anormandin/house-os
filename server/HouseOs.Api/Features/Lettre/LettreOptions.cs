namespace HouseOs.Api.Features.Lettre;

/// <summary>
/// La lettre du matin : l'heure de la maison, la limite du rattrapage, et la porte de
/// sortie SMTP (D-2026-09-21 Courriel Sortant Par SMTP, D-2026-09-21 Lettre
/// Matérialisée Et Rattrapée Le Jour Même). La clé Anthropic et le modèle sont ceux de
/// l'édition : un seul compte pour la maison.
/// </summary>
public class LettreOptions
{
    /// <summary>L'heure à laquelle la lettre part, avant que la maison se lève.</summary>
    public TimeOnly HeureEnvoi { get; set; } = new(6, 30);

    /// <summary>Passé cette heure, la lettre du jour n'est plus envoyée : la journée est
    /// perdue, jamais rattrapée le lendemain.</summary>
    public TimeOnly LimiteRattrapage { get; set; } = new(12, 0);

    /// <summary>Base de l'URL de l'app pour le lien « Ouvrir la journée » ; vide = pas de
    /// lien.</summary>
    public string UrlDeLApp { get; set; } = "";

    public SmtpOptions Smtp { get; set; } = new();
}

public class SmtpOptions
{
    public string Hote { get; set; } = "";

    /// <summary>587 = STARTTLS, 465 = TLS dès la connexion ; autre = négocié.</summary>
    public int Port { get; set; } = 587;

    /// <summary>Facultatif : un relais maison sans authentification est un cas réel.</summary>
    public string Usager { get; set; } = "";
    public string MotDePasse { get; set; } = "";

    /// <summary>« La maison &lt;maison@exemple.tld&gt; » ou une adresse nue.</summary>
    public string Expediteur { get; set; } = "";

    /// <summary>Un hôte et un expéditeur suffisent à ouvrir la porte ; sans eux, l'envoi
    /// est désactivé et la lettre reste lisible dans l'app.</summary>
    public bool Actif =>
        string.IsNullOrWhiteSpace(Hote) == false
        && string.IsNullOrWhiteSpace(Expediteur) == false;

    public bool AvecAuthentification => string.IsNullOrWhiteSpace(Usager) == false;
}
