using HouseOs.Api.Features.Affichage;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace HouseOs.Tests.Features.Affichage;

public class SeuillageTests
{
    private static byte[] PngGris(params byte[] niveaux)
    {
        using var image = new Image<L8>(niveaux.Length, 1);
        for (var x = 0; x < niveaux.Length; x++)
        {
            image[x, 0] = new L8(niveaux[x]);
        }
        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }

    [Fact]
    public void Chaque_pixel_devient_noir_ou_blanc_a_la_moitie()
    {
        var resultat = Seuillage.EnUnBit(PngGris(0, 100, 127, 128, 200, 255));

        using var image = Image.Load<L8>(resultat);
        var valeurs = Enumerable.Range(0, 6).Select(x => image[x, 0].PackedValue).ToArray();
        Assert.Equal(new byte[] { 0, 0, 0, 255, 255, 255 }, valeurs);
    }

    [Fact]
    public void Le_png_est_encode_en_gris_1_bit()
    {
        var resultat = Seuillage.EnUnBit(PngGris(0, 255));

        var info = Image.Identify(resultat);
        var png = info.Metadata.GetPngMetadata();
        Assert.Equal(PngBitDepth.Bit1, png.BitDepth);
        Assert.Equal(PngColorType.Grayscale, png.ColorType);
    }

    [Fact]
    public void La_signature_est_stable_courte_et_sensible_au_contenu()
    {
        var a = Seuillage.Signature([1, 2, 3]);
        var b = Seuillage.Signature([1, 2, 3]);
        var c = Seuillage.Signature([1, 2, 4]);

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
        Assert.Equal(16, a.Length);
        Assert.Matches("^[0-9a-f]{16}$", a);
    }
}
