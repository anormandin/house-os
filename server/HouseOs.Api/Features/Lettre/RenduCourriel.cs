using System.Globalization;
using System.Text;
using HouseOs.Api.Domaine.Lettre;

namespace HouseOs.Api.Features.Lettre;

/// <summary>
/// Le courriel tel qu'il part : le gabarit pose la date, « Bonjour vous deux. », les
/// paragraphes de la maison, « Bonne journée. », la signature et le pied. Deux corps —
/// texte et HTML — la palette de la maquette C1, aucune ressource distante.
/// </summary>
public static class RenduCourriel
{
    public const string Salutation = "Bonjour vous deux.";
    public const string Adieu = "Bonne journée.";
    public const string Signature = "— la maison";
    private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-CA");

    public static CourrielAEnvoyer Composer(
        LettreDuMatin lettre, IReadOnlyList<Destinataire> destinataires, LettreOptions options) =>
        new(destinataires, lettre.Sujet, Texte(lettre, options), Html(lettre, options));

    public static string DateEnToutesLettres(DateOnly date)
    {
        var s = date.ToString("dddd d MMMM yyyy", Fr);
        return char.ToUpper(s[0], Fr) + s[1..];
    }

    public static string Pied(LettreDuMatin lettre) =>
        $"House OS · écrite à {lettre.ComposeeLe.LocalDateTime:%H} h {lettre.ComposeeLe.LocalDateTime:mm}";

    private static string? Lien(LettreOptions options) =>
        string.IsNullOrWhiteSpace(options.UrlDeLApp) ? null : options.UrlDeLApp.TrimEnd('/') + "/";

    public static string Texte(LettreDuMatin lettre, LettreOptions options)
    {
        var sb = new StringBuilder();
        sb.Append(DateEnToutesLettres(lettre.Date)).Append("\n\n");
        sb.Append(Salutation).Append("\n\n");
        foreach (var p in lettre.Paragraphes)
        {
            sb.Append(p).Append("\n\n");
        }
        sb.Append(Adieu).Append("\n\n").Append(Signature).Append("\n\n");
        sb.Append(Pied(lettre));
        if (Lien(options) is { } lien)
        {
            sb.Append("\nOuvrir la journée : ").Append(lien);
        }
        sb.Append('\n');
        return sb.ToString();
    }

    public static string Html(LettreDuMatin lettre, LettreOptions options)
    {
        // Les quatre signes qui comptent, et les accents restent des accents : le
        // charset est utf-8, une entité numérique par « é » ne servirait à personne.
        static string E(string s) => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html lang=\"fr\"><head><meta charset=\"utf-8\"><title>")
          .Append(E(lettre.Sujet)).Append("</title></head>");
        sb.Append("<body style=\"margin:0;padding:30px 16px 34px;background:#f2ecbc;color:#545464;")
          .Append("font-family:'Nunito Sans',Helvetica,Arial,sans-serif;\">");
        sb.Append("<div style=\"max-width:560px;margin:0 auto;background:#faf7ed;border-radius:14px;padding:30px 32px;\">");
        sb.Append("<p style=\"margin:0 0 18px;font-family:Fraunces,Georgia,serif;color:#43436c;font-size:15px;\">")
          .Append(E(DateEnToutesLettres(lettre.Date))).Append("</p>");
        sb.Append(Paragraphe(Salutation));
        foreach (var p in lettre.Paragraphes)
        {
            sb.Append(Paragraphe(E(p)));
        }
        sb.Append(Paragraphe(Adieu));
        sb.Append(Paragraphe(E(Signature)));
        sb.Append("</div>");
        sb.Append("<p style=\"color:#8a8980;font-size:12.5px;text-align:center;padding-top:18px;line-height:1.6;\">")
          .Append(E(Pied(lettre)));
        if (Lien(options) is { } lien)
        {
            sb.Append("<br><a href=\"").Append(E(lien)).Append("\" style=\"color:#77713f;\">Ouvrir la journée</a>");
        }
        sb.Append("</p></body></html>");
        return sb.ToString();
    }

    private static string Paragraphe(string html) =>
        $"<p style=\"margin:0 0 15px;font-size:16.5px;line-height:1.62;\">{html}</p>";
}
