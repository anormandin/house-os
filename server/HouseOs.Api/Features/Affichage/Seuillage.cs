using System.Security.Cryptography;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace HouseOs.Api.Features.Affichage;

/// <summary>
/// De la capture anti-aliasée au bitmap de l'appareil : noir ou blanc, rien entre
/// (la grammaire e-ink du vault — aucun tramage, qui donnerait de la boue).
/// </summary>
public static class Seuillage
{
    /// <summary>PNG niveaux de gris 1 bit : chaque pixel &lt; 50 % de luminance devient noir.</summary>
    public static byte[] EnUnBit(byte[] pngBrut)
    {
        using var image = Image.Load<L8>(pngBrut);
        image.Mutate(x => x.BinaryThreshold(0.5f));
        using var sortie = new MemoryStream();
        image.Save(sortie, new PngEncoder
        {
            ColorType = PngColorType.Grayscale,
            BitDepth = PngBitDepth.Bit1,
        });
        return sortie.ToArray();
    }

    /// <summary>
    /// Le nom du fichier que l'appareil compare pour décider de redessiner : même
    /// contenu → même nom → pas de rafraîchissement (et pas de clignotement).
    /// </summary>
    public static string Signature(byte[] octets) =>
        Convert.ToHexString(SHA256.HashData(octets))[..16].ToLowerInvariant();
}
