using System.Collections.Concurrent;

namespace HouseOs.Api.Features.Affichage;

/// <summary>
/// Les derniers bitmaps rendus, par appareil, en mémoire : l'appareil demande l'image
/// une seconde après avoir reçu son URL, inutile de la rendre deux fois. Trois par
/// appareil suffisent (un réveil raté, un bouton pressé). Vide après un redémarrage :
/// la route d'image re-rend alors.
/// </summary>
public sealed class CacheImages
{
    private const int ParAppareil = 3;
    private readonly ConcurrentDictionary<Guid, List<(string Fichier, byte[] Octets)>> _images = new();

    public void Deposer(Guid appareilId, string fichier, byte[] octets)
    {
        var liste = _images.GetOrAdd(appareilId, _ => []);
        lock (liste)
        {
            liste.RemoveAll(i => i.Fichier == fichier);
            liste.Insert(0, (fichier, octets));
            if (liste.Count > ParAppareil)
            {
                liste.RemoveRange(ParAppareil, liste.Count - ParAppareil);
            }
        }
    }

    public byte[]? Lire(Guid appareilId, string fichier)
    {
        if (_images.TryGetValue(appareilId, out var liste) == false)
        {
            return null;
        }
        lock (liste)
        {
            return liste.FirstOrDefault(i => i.Fichier == fichier).Octets;
        }
    }

    public void Oublier(Guid appareilId) => _images.TryRemove(appareilId, out _);
}
