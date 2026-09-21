using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace HouseOs.Api.Features.Lettre;

/// <summary>
/// SMTP par MailKit (D-2026-09-21 Courriel Sortant Par SMTP) : une connexion par envoi,
/// jamais gardée ouverte — une lettre par jour ne mérite pas un pool. Le choix TLS suit
/// le port, comme le font tous les clients de courriel.
/// </summary>
public sealed class EnvoyeurSmtp(SmtpOptions options, ILogger<EnvoyeurSmtp> journal) : IEnvoyeurDeCourriel
{
    public static readonly TimeSpan DelaiMax = TimeSpan.FromSeconds(30);

    public bool Actif => options.Actif;

    internal static SecureSocketOptions OptionsDeSocket(int port) => port switch
    {
        465 => SecureSocketOptions.SslOnConnect,
        587 => SecureSocketOptions.StartTls,
        _ => SecureSocketOptions.Auto,
    };

    /// <summary>« La maison &lt;x@y&gt; » ou « x@y » ; une chaîne qui ne se lit pas comme
    /// une adresse est refusée au démarrage plutôt qu'au premier envoi.</summary>
    internal static MailboxAddress LireExpediteur(string expediteur)
    {
        if (MailboxAddress.TryParse(expediteur, out var adresse) && string.IsNullOrEmpty(adresse.Address) == false)
        {
            return adresse;
        }
        throw new InvalidOperationException($"Lettre:Smtp:Expediteur n'est pas une adresse : « {expediteur} ».");
    }

    public async Task EnvoyerAsync(CourrielAEnvoyer courriel, CancellationToken ct)
    {
        if (courriel.Destinataires.Count == 0)
        {
            throw new ArgumentException("Aucun destinataire.", nameof(courriel));
        }

        var message = new MimeMessage();
        message.From.Add(LireExpediteur(options.Expediteur));
        foreach (var d in courriel.Destinataires)
        {
            message.To.Add(new MailboxAddress(d.Nom, d.Adresse));
        }
        message.Subject = courriel.Sujet;
        message.Body = new BodyBuilder { TextBody = courriel.Texte, HtmlBody = courriel.Html }.ToMessageBody();

        using var client = new SmtpClient { Timeout = (int)DelaiMax.TotalMilliseconds };
        await client.ConnectAsync(options.Hote, options.Port, OptionsDeSocket(options.Port), ct);
        if (options.AvecAuthentification)
        {
            await client.AuthenticateAsync(options.Usager, options.MotDePasse, ct);
        }
        var reponse = await client.SendAsync(message, ct);
        await client.DisconnectAsync(quit: true, ct);
        journal.LogInformation(
            "Courriel envoyé à {Nombre} destinataire(s) via {Hote} : {Reponse}",
            courriel.Destinataires.Count, options.Hote, reponse);
    }
}
