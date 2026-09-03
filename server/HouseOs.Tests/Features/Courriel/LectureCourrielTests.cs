using HouseOs.Api.Features.Courriel;
using MimeKit;
using static HouseOs.Tests.Features.Courriel.FabriqueCourriels;

namespace HouseOs.Tests.Features.Courriel;

/// <summary>
/// La règle « quelle pièce devient un document » et l'extraction du texte : ce qui
/// décide si un reçu HTML devient un .eml ou si un logo de signature devient une photo.
/// </summary>
public class LectureCourrielTests
{
    [Fact]
    public async Task Un_pdf_joint_est_retenu_avec_ses_faits()
    {
        var lu = await LectureCourriel.LireAsync(AvecPdf(), CancellationToken.None);

        var piece = Assert.Single(lu.PiecesJointes);
        Assert.Equal("Facture_48211.pdf", piece.NomFichier);
        Assert.Equal("application/pdf", piece.TypeMime);
        Assert.Equal(PetitPdf(), piece.Contenu);
        Assert.Equal("recu-1@exemple.test", lu.MessageId);
        Assert.Equal("camille@exemple.com", lu.Expediteur);
        Assert.Equal("Votre reçu IKEA", lu.Sujet);
        Assert.Equal(new DateOnly(2026, 9, 1), DateOnly.FromDateTime(lu.Date.DateTime));
        Assert.Equal("Merci pour votre achat.", lu.Texte);
    }

    [Fact]
    public async Task Un_courriel_html_seul_n_a_aucune_piece_et_un_texte_propre()
    {
        var lu = await LectureCourriel.LireAsync(HtmlSeul(), CancellationToken.None);

        Assert.Empty(lu.PiecesJointes);
        Assert.DoesNotContain("alert", lu.Texte);
        Assert.DoesNotContain("color", lu.Texte);
        Assert.DoesNotContain("<", lu.Texte);
        Assert.Contains("Bonjour Alain,", lu.Texte);
        Assert.Contains("Total : 129,95 $", lu.Texte);
        Assert.Contains("BILLY 1", lu.Texte);
    }

    [Fact]
    public async Task Un_logo_de_signature_reference_en_cid_est_ignore()
    {
        var message = Message(html: "<p>Cordialement</p><img src=\"cid:logo123\">", texte: null, corps: b =>
        {
            var logo = b.LinkedResources.Add("logo.png", PetitPng(), ContentType.Parse("image/png"));
            logo.ContentId = "logo123";
        });

        var lu = await LectureCourriel.LireAsync(Octets(message), CancellationToken.None);

        Assert.Empty(lu.PiecesJointes);
    }

    [Fact]
    public async Task Une_grosse_image_inline_non_referencee_est_retenue()
    {
        // Une photo collée mais pas incrustée dans le HTML : c'est une pièce.
        var message = Message(html: "<p>Voici la plaque</p>", texte: null, corps: b =>
        {
            var photo = b.LinkedResources.Add("plaque.png", GrosPng(), ContentType.Parse("image/png"));
            photo.ContentId = "autre";
        });

        var lu = await LectureCourriel.LireAsync(Octets(message), CancellationToken.None);

        var piece = Assert.Single(lu.PiecesJointes);
        Assert.Equal("plaque.png", piece.NomFichier);
    }

    [Fact]
    public async Task Une_banniere_transferee_par_mail_app_est_ignoree_mais_pas_le_pdf()
    {
        // Mail.app transfère les images du corps en parties « inline » nommées, sans cid,
        // entre deux morceaux de HTML — la bannière Zoho Sign faisait 16 Ko en PNG. Elle ne
        // doit pas devenir un document ; le PDF, lui aussi « inline » chez Mail.app, si.
        var message = Message(html: "<p>Document terminé</p>", texte: null, corps: b =>
        {
            var banniere = (MimePart)b.Attachments.Add("zs_branding.png", PngBruit(64), ContentType.Parse("image/png"));
            banniere.ContentDisposition!.Disposition = ContentDisposition.Inline;
            var pdf = (MimePart)b.Attachments.Add("SO-948157.pdf", PetitPdf(), ContentType.Parse("application/pdf"));
            pdf.ContentDisposition!.Disposition = ContentDisposition.Inline;
        });

        var lu = await LectureCourriel.LireAsync(Octets(message), CancellationToken.None);

        var piece = Assert.Single(lu.PiecesJointes);
        Assert.Equal("SO-948157.pdf", piece.NomFichier);
        Assert.True(PngBruit(64).Length > 10 * 1024, "la bannière de test doit dépasser l'ancien seuil de 10 Ko");
    }

    [Fact]
    public async Task Le_type_image_jpg_est_normalise_et_un_docx_est_ecarte()
    {
        var message = Message(corps: b =>
        {
            b.Attachments.Add("photo.jpg", [0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3], ContentType.Parse("image/jpg"));
            b.Attachments.Add("contrat.docx", [1, 2, 3],
                ContentType.Parse("application/vnd.openxmlformats-officedocument.wordprocessingml.document"));
        });

        var lu = await LectureCourriel.LireAsync(Octets(message), CancellationToken.None);

        var piece = Assert.Single(lu.PiecesJointes);
        Assert.Equal("image/jpeg", piece.TypeMime);
    }

    [Fact]
    public async Task Un_nom_encode_rfc2047_est_decode()
    {
        var message = Message(corps: b =>
            b.Attachments.Add("Reçu d'été — été.pdf", PetitPdf(), ContentType.Parse("application/pdf")));
        // Aller-retour par les octets : c'est la forme encodée sur le fil qui est relue.
        var lu = await LectureCourriel.LireAsync(Octets(message), CancellationToken.None);

        Assert.Equal("Reçu d'été — été.pdf", Assert.Single(lu.PiecesJointes).NomFichier);
    }

    [Fact]
    public async Task Sans_message_id_le_champ_est_null()
    {
        var lu = await LectureCourriel.LireAsync(Octets(Message(messageId: null)), CancellationToken.None);

        Assert.Null(lu.MessageId);
    }

    [Theory]
    [InlineData("<p>Un</p><p>Deux</p>", "Un\nDeux")]
    [InlineData("Ligne<br>suivante", "Ligne\nsuivante")]
    [InlineData("<div>a   b\n   c</div>", "a b c")]
    [InlineData("caf&eacute; &amp; th&#233;", "café & thé")]
    [InlineData("<script>x=1</script>visible<style>p{}</style>", "visible")]
    [InlineData("<p>a</p><p></p><p></p><p>b</p>", "a\n\nb")]
    public void HtmlVersTexte_nettoie(string html, string attendu)
    {
        Assert.Equal(attendu, LectureCourriel.HtmlVersTexte(html));
    }
}
