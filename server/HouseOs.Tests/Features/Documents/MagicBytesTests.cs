using HouseOs.Api.Features.Documents;

namespace HouseOs.Tests.Features.Documents;

/// <summary>
/// Le reniflage des magic bytes de l'upload : chaque type permis reconnaît sa
/// signature et rien d'autre — le Content-Type client ne vaut rien.
/// </summary>
public class MagicBytesTests
{
    private static byte[] Png => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0];
    private static byte[] Jpeg => [0xFF, 0xD8, 0xFF, 0xE0, 0, 0, 0, 0, 0, 0, 0, 0];
    private static byte[] Pdf => "%PDF-1.7 xxx"u8.ToArray();
    // « RIFF » + taille (4 octets) + « WEBP » ; HEIC : taille + « ftyp » + marque.
    private static byte[] Webp => "RIFF\0\0\0\0WEBP"u8.ToArray();
    private static byte[] Heic => "\0\0\0\0ftypheic"u8.ToArray();

    [Fact]
    public void ChaqueTypePermis_ReconnaitSaSignature()
    {
        Assert.True(DocumentsEndpoints.ContenuCorrespondAuType(Png, "image/png"));
        Assert.True(DocumentsEndpoints.ContenuCorrespondAuType(Jpeg, "image/jpeg"));
        Assert.True(DocumentsEndpoints.ContenuCorrespondAuType(Pdf, "application/pdf"));
        Assert.True(DocumentsEndpoints.ContenuCorrespondAuType(Webp, "image/webp"));
        Assert.True(DocumentsEndpoints.ContenuCorrespondAuType(Heic, "image/heic"));
        // Variante de marque HEIF produite par les iPhone récents.
        Assert.True(DocumentsEndpoints.ContenuCorrespondAuType(
            "\0\0\0\0ftypmif1"u8.ToArray(), "image/heic"));
    }

    [Fact]
    public void UnContenuEtranger_EstRefusePourChaqueType()
    {
        var html = "<html>bonjour</html>"u8.ToArray();
        foreach (var type in DocumentsEndpoints.TypesMimePermis)
        {
            Assert.False(DocumentsEndpoints.ContenuCorrespondAuType(html, type));
        }
    }

    [Fact]
    public void LesSignaturesCroisees_NeSeValidentPasEntreElles()
    {
        Assert.False(DocumentsEndpoints.ContenuCorrespondAuType(Png, "application/pdf"));
        Assert.False(DocumentsEndpoints.ContenuCorrespondAuType(Pdf, "image/png"));
        Assert.False(DocumentsEndpoints.ContenuCorrespondAuType(Jpeg, "image/webp"));
        // RIFF sans WEBP (un .wav, par exemple) n'est pas un webp.
        Assert.False(DocumentsEndpoints.ContenuCorrespondAuType(
            "RIFF\0\0\0\0WAVE"u8.ToArray(), "image/webp"));
    }

    [Fact]
    public void UnEnTeteTropCourtOuVide_EstRefuse()
    {
        Assert.False(DocumentsEndpoints.ContenuCorrespondAuType([], "image/png"));
        Assert.False(DocumentsEndpoints.ContenuCorrespondAuType([0x89], "image/png"));
        Assert.False(DocumentsEndpoints.ContenuCorrespondAuType("RIFF"u8, "image/webp"));
    }
}
