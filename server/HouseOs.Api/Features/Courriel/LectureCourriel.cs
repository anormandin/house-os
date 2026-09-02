using System.Text;
using System.Text.RegularExpressions;
using HouseOs.Api.Features.Documents;
using MimeKit;
using MimeKit.Text;

namespace HouseOs.Api.Features.Courriel;

public record PieceJointeLue(string NomFichier, string TypeMime, byte[] Contenu);

public record CourrielLu(
    string? MessageId,
    string Expediteur,
    string Sujet,
    DateTimeOffset Date,
    string Texte,
    IReadOnlyList<PieceJointeLue> PiecesJointes);

/// <summary>
/// Du MIME brut aux faits utiles : qui, quoi, quand, le texte du corps et les pièces
/// qui méritent de devenir des documents. Pur — testable avec des courriels bâtis en
/// mémoire.
/// </summary>
public static partial class LectureCourriel
{
    /// <summary>Sous cette taille, une image inline est un logo de signature.</summary>
    public const int TailleMinImageInline = 10 * 1024;

    [GeneratedRegex(@"cid:([^""'\s>)]+)", RegexOptions.IgnoreCase)]
    private static partial Regex ReferencesCid();

    public static async Task<CourrielLu> LireAsync(byte[] eml, CancellationToken ct)
    {
        using var flux = new MemoryStream(eml, writable: false);
        var message = await MimeMessage.LoadAsync(flux, ct);
        return Lire(message);
    }

    public static CourrielLu Lire(MimeMessage message)
    {
        var html = message.HtmlBody;
        var cids = ReferencesCid().Matches(html ?? "")
            .Select(m => m.Groups[1].Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var pieces = new List<PieceJointeLue>();
        foreach (var part in message.BodyParts.OfType<MimePart>())
        {
            var typeMime = EnregistrementDocument.NormaliserTypeMime(part.ContentType.MimeType);
            if (DocumentsEndpoints.TypesMimePermis.Contains(typeMime) == false || part.Content is null)
            {
                continue;
            }
            using var memoire = new MemoryStream();
            part.Content.DecodeTo(memoire);
            if (memoire.Length == 0 || EstPieceRetenue(part, memoire.Length, cids) == false)
            {
                continue;
            }
            var nom = string.IsNullOrWhiteSpace(part.FileName)
                ? "piece-jointe" + EnregistrementDocument.ExtensionPour(typeMime)
                : part.FileName;
            pieces.Add(new PieceJointeLue(nom, typeMime, memoire.ToArray()));
        }

        var texte = message.TextBody;
        if (string.IsNullOrWhiteSpace(texte) && html is not null)
        {
            texte = HtmlVersTexte(html);
        }
        var date = message.Date == default ? DateTimeOffset.UtcNow : message.Date;
        return new CourrielLu(
            string.IsNullOrWhiteSpace(message.MessageId) ? null : message.MessageId,
            message.From.Mailboxes.FirstOrDefault()?.Address ?? "",
            message.Subject ?? "",
            date,
            (texte ?? "").Trim(),
            pieces);
    }

    /// <summary>
    /// Une pièce est retenue si elle est déclarée « attachment », ou si elle porte un
    /// nom sans être une image incrustée dans le HTML (cid:) — hors petites images
    /// inline, qui sont des logos de signature.
    /// </summary>
    public static bool EstPieceRetenue(MimePart part, long taille, ISet<string> cidsReferences)
    {
        if (part.IsAttachment)
        {
            return true;
        }
        if (string.IsNullOrWhiteSpace(part.FileName))
        {
            return false;
        }
        if (part.ContentId is { Length: > 0 } cid && cidsReferences.Contains(cid))
        {
            return false;
        }
        var estImage = part.ContentType.IsMimeType("image", "*");
        return estImage == false || taille >= TailleMinImageInline;
    }

    /// <summary>
    /// Le texte d'un corps HTML : sans script, style ni en-tête, un saut de ligne par
    /// bloc, entités décodées, blancs compactés. MimeKit ne fournit que l'inverse
    /// (texte → HTML) ; son tokenizer suffit pour ce sens-ci.
    /// </summary>
    public static string HtmlVersTexte(string html)
    {
        var tokenizer = new HtmlTokenizer(new StringReader(html)) { DecodeCharacterReferences = true };
        var texte = new StringBuilder();
        var profondeurIgnoree = 0;
        while (tokenizer.ReadNextToken(out var token))
        {
            switch (token.Kind)
            {
                case HtmlTokenKind.Data:
                    if (profondeurIgnoree == 0)
                    {
                        // Les retours de ligne du HTML source ne sont que des blancs :
                        // seules les balises font les lignes.
                        texte.Append(((HtmlDataToken)token).Data.Replace('\n', ' ').Replace('\r', ' '));
                    }
                    break;
                case HtmlTokenKind.Tag:
                    var tag = (HtmlTagToken)token;
                    if (tag.Id is HtmlTagId.Script or HtmlTagId.Style or HtmlTagId.Head or HtmlTagId.Title)
                    {
                        profondeurIgnoree = Math.Max(0, profondeurIgnoree + (tag.IsEndTag ? -1 : 1));
                    }
                    else if (tag.Id is HtmlTagId.Br or HtmlTagId.HR)
                    {
                        texte.Append('\n');
                    }
                    else if (tag.IsEndTag == false && tag.Id is HtmlTagId.TD or HtmlTagId.TH)
                    {
                        texte.Append(' ');
                    }
                    // Un bloc fait sa ligne à la fermeture : à l'ouverture aussi, deux
                    // paragraphes voisins laisseraient une ligne vide entre eux.
                    else if (tag.IsEndTag && tag.Id is HtmlTagId.P or HtmlTagId.Div or HtmlTagId.LI or HtmlTagId.TR
                        or HtmlTagId.H1 or HtmlTagId.H2 or HtmlTagId.H3 or HtmlTagId.H4 or HtmlTagId.H5 or HtmlTagId.H6
                        or HtmlTagId.Table or HtmlTagId.UL or HtmlTagId.OL or HtmlTagId.BlockQuote)
                    {
                        texte.Append('\n');
                    }
                    break;
            }
        }
        return Compacter(texte.ToString());
    }

    private static string Compacter(string brut)
    {
        var lignes = brut.Replace(' ', ' ').Replace("\r", "").Split('\n');
        var resultat = new StringBuilder();
        var lignesVides = 0;
        foreach (var ligne in lignes)
        {
            var compacte = string.Join(' ', ligne.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries));
            if (compacte.Length == 0)
            {
                lignesVides++;
                continue;
            }
            if (resultat.Length > 0)
            {
                resultat.Append(lignesVides > 0 ? "\n\n" : "\n");
            }
            lignesVides = 0;
            resultat.Append(compacte);
        }
        return resultat.ToString();
    }
}
