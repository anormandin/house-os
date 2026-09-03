using HouseOs.Api.Features.Affichage;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace HouseOs.Tests.Features.Affichage;

/// <summary>
/// Pas de Chromium sous test : une image grise à la taille demandée, dont la moitié
/// gauche est sombre — assez pour que le seuillage ait quelque chose à faire.
/// </summary>
public sealed class RenduEcranFictif : IRenduEcran
{
    public int Captures { get; private set; }
    public DemandeCapture? DerniereDemande { get; private set; }

    public Task<byte[]> CapturerAsync(DemandeCapture demande, CancellationToken ct)
    {
        Captures++;
        DerniereDemande = demande;
        var (largeur, hauteur) = (demande.Largeur, demande.Hauteur);
        using var image = new Image<L8>(largeur, hauteur);
        for (var y = 0; y < hauteur; y++)
        {
            for (var x = 0; x < largeur; x++)
            {
                image[x, y] = new L8((byte)(x < largeur / 2 ? 40 : 220));
            }
        }
        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return Task.FromResult(ms.ToArray());
    }
}
