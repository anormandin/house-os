using MimeKit;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace HouseOs.Tests.Features.Courriel;

/// <summary>Courriels déterministes bâtis en mémoire — pas de fixtures opaques.</summary>
public static class FabriqueCourriels
{
    public static byte[] PetitPdf() => "%PDF-1.4\n1 0 obj << >> endobj\ntrailer\n%%EOF\n"u8.ToArray();

    public static byte[] PetitPng(int cote = 4)
    {
        using var image = new Image<Rgba32>(cote, cote);
        using var memoire = new MemoryStream();
        image.SaveAsPng(memoire);
        return memoire.ToArray();
    }

    /// <summary>Un PNG bien au-delà du seuil « logo de signature » (~120 Ko de bruit).</summary>
    public static byte[] GrosPng() => PngBruit(200);

    /// <summary>Un PNG de bruit non compressible : ~3 octets par pixel, taille prévisible.</summary>
    public static byte[] PngBruit(int cote)
    {
        var alea = new Random(7);
        using var image = new Image<Rgba32>(cote, cote);
        for (var y = 0; y < cote; y++)
        {
            for (var x = 0; x < cote; x++)
            {
                image[x, y] = new Rgba32((byte)alea.Next(256), (byte)alea.Next(256), (byte)alea.Next(256));
            }
        }
        using var memoire = new MemoryStream();
        image.SaveAsPng(memoire);
        return memoire.ToArray();
    }

    public static MimeMessage Message(
        string sujet = "Votre reçu IKEA",
        string de = "alain.normandin@gmail.com",
        string? texte = "Merci pour votre achat.",
        string? html = null,
        string? messageId = "recu-1@exemple.test",
        DateTimeOffset? date = null,
        Action<BodyBuilder>? corps = null)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Alain", de));
        message.To.Add(new MailboxAddress("Documents", "documents@alainnormandin.dev"));
        message.Subject = sujet;
        message.Date = date ?? new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.FromHours(-4));
        var builder = new BodyBuilder { TextBody = texte, HtmlBody = html };
        corps?.Invoke(builder);
        message.Body = builder.ToMessageBody();
        if (messageId is null)
        {
            // MimeKit en génère un d'office : un courriel sans Message-ID, ça se fabrique.
            message.Headers.Remove(HeaderId.MessageId);
        }
        else
        {
            message.MessageId = messageId;
        }
        return message;
    }

    public static byte[] Octets(MimeMessage message)
    {
        using var memoire = new MemoryStream();
        message.WriteTo(memoire);
        return memoire.ToArray();
    }

    public static byte[] AvecPdf(string sujet = "Votre reçu IKEA", string nom = "Facture_48211.pdf", string? messageId = "recu-1@exemple.test") =>
        Octets(Message(sujet, messageId: messageId, corps: b => b.Attachments.Add(nom, PetitPdf(), ContentType.Parse("application/pdf"))));

    public static byte[] HtmlSeul(string sujet = "Confirmation de commande", string? messageId = "html-1@exemple.test") =>
        Octets(Message(sujet, texte: null, messageId: messageId,
            html: "<html><head><title>x</title><style>p{color:red}</style></head><body>" +
                  "<p>Bonjour&nbsp;Alain,</p><p>Total&nbsp;: 129,95&nbsp;$</p><script>alert(1)</script>" +
                  "<table><tr><td>BILLY</td><td>1</td></tr></table></body></html>"));
}
