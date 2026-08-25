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
}
