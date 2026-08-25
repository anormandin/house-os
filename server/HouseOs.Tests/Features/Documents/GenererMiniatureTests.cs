using HouseOs.Api.Features.Documents;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace HouseOs.Tests.Features.Documents;

public class GenererMiniatureTests : IDisposable
{
    private readonly string _dossier =
        Directory.CreateTempSubdirectory("houseos-miniatures-").FullName;

    public void Dispose() => Directory.Delete(_dossier, recursive: true);

    private string Chemin(string nom) => Path.Combine(_dossier, nom);

    private string CreerImage(int largeur, int hauteur)
    {
        var chemin = Chemin($"{largeur}x{hauteur}.png");
        using var image = new Image<Rgba32>(largeur, hauteur);
        image.SaveAsPng(chemin);
        return chemin;
    }

    [Fact]
    public async Task Reduit_une_grande_image_au_cote_maximal()
    {
        var source = CreerImage(1600, 800);
        var destination = Chemin("grande.webp");

        Assert.True(await DocumentsEndpoints.GenererMiniatureAsync(source, destination));

        using var miniature = await Image.LoadAsync(destination);
        Assert.Equal(DocumentsEndpoints.CoteMiniature, miniature.Width);
        Assert.Equal(DocumentsEndpoints.CoteMiniature / 2, miniature.Height);
    }

    [Fact]
    public async Task N_agrandit_pas_une_petite_image()
    {
        var source = CreerImage(200, 100);
        var destination = Chemin("petite.webp");

        Assert.True(await DocumentsEndpoints.GenererMiniatureAsync(source, destination));

        using var miniature = await Image.LoadAsync(destination);
        Assert.Equal(200, miniature.Width);
        Assert.Equal(100, miniature.Height);
    }

    [Fact]
    public async Task Retourne_false_pour_un_fichier_non_decodable()
    {
        var source = Chemin("photo.heic");
        await File.WriteAllBytesAsync(source, [0x00, 0x01, 0x02, 0x03]);
        var destination = Chemin("photo.webp");

        Assert.False(await DocumentsEndpoints.GenererMiniatureAsync(source, destination));
        Assert.False(File.Exists(destination));
    }

    [Fact]
    public async Task Retourne_false_pour_une_image_aux_dimensions_demesurees()
    {
        // Bombe de décompression : un en-tête PNG annonçant 30000×30000 (des Go une
        // fois décodés) dans un fichier minuscule — refusé sur l'en-tête, sans décoder.
        var source = Chemin("bombe.png");
        await File.WriteAllBytesAsync(source, EnTetePng(30000, 30000));
        var destination = Chemin("bombe.webp");

        Assert.False(await DocumentsEndpoints.GenererMiniatureAsync(source, destination));
        Assert.False(File.Exists(destination));
    }

    [Fact]
    public async Task Ne_laisse_aucun_tmp_quand_le_deplacement_echoue()
    {
        var source = CreerImage(100, 100);
        // Un dossier occupe le chemin de destination : File.Move échoue après
        // l'écriture du temporaire — le .tmp doit être nettoyé, rien ne balaie le cache.
        var destination = Chemin("prise.webp");
        Directory.CreateDirectory(destination);

        await Assert.ThrowsAnyAsync<IOException>(() =>
            DocumentsEndpoints.GenererMiniatureAsync(source, destination));

        Assert.Empty(Directory.GetFiles(_dossier, "*.tmp"));
    }

    /// <summary>Signature PNG + chunk IHDR valide (CRC comprise) annonçant les
    /// dimensions voulues — assez pour Image.Identify, rien à décoder.</summary>
    private static byte[] EnTetePng(int largeur, int hauteur)
    {
        var ihdr = new byte[13];
        EcrireEntier(ihdr, 0, largeur);
        EcrireEntier(ihdr, 4, hauteur);
        ihdr[8] = 8;  // profondeur de bits
        ihdr[9] = 6;  // couleur RGBA
        var chunk = "IHDR"u8.ToArray().Concat(ihdr).ToArray();

        var png = new List<byte>([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        var longueur = new byte[4];
        EcrireEntier(longueur, 0, 13);
        png.AddRange(longueur);
        png.AddRange(chunk);
        var crc = new byte[4];
        EcrireEntier(crc, 0, unchecked((int)Crc32(chunk)));
        png.AddRange(crc);
        return [.. png];
    }

    private static void EcrireEntier(byte[] cible, int position, int valeur)
    {
        cible[position] = (byte)(valeur >> 24);
        cible[position + 1] = (byte)(valeur >> 16);
        cible[position + 2] = (byte)(valeur >> 8);
        cible[position + 3] = (byte)valeur;
    }

    private static uint Crc32(byte[] donnees)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var octet in donnees)
        {
            crc ^= octet;
            for (var i = 0; i < 8; i++)
            {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
            }
        }
        return crc ^ 0xFFFFFFFFu;
    }
}
