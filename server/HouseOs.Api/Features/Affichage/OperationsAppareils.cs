using System.Security.Cryptography;
using System.Text;
using HouseOs.Api.Domaine;
using HouseOs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HouseOs.Api.Features.Affichage;

/// <summary>Ce qu'un humain voit d'un écran enrôlé (REST et MCP).</summary>
public record AppareilAffichageDto(
    Guid Id,
    string Identifiant,
    string AdresseMac,
    string? Nom,
    string? Modele,
    int? Largeur,
    int? Hauteur,
    string? VersionFirmware,
    double? TensionPile,
    int? PilePourcent,
    int? Rssi,
    DateTimeOffset EnroleLe,
    DateTimeOffset? DernierContact,
    string? DernierFichier);

/// <summary>
/// Le registre des appareils, partagé par le protocole (enrôlement, authentification)
/// et par les humains (liste, renommage, révocation) — REST et MCP appellent ici.
/// </summary>
public static class OperationsAppareils
{
    public const int LongueurIdentifiant = 6;

    public static async Task<AppareilAffichage> EnrolerOuRetrouverAsync(
        HouseOsDbContext db, string adresseMac, DateTimeOffset maintenant)
    {
        var mac = adresseMac.Trim().ToUpperInvariant();
        var existant = await db.AppareilsAffichage.SingleOrDefaultAsync(a => a.AdresseMac == mac);
        if (existant is not null)
        {
            return existant;
        }

        var appareil = new AppareilAffichage
        {
            Id = Guid.NewGuid(),
            AdresseMac = mac,
            Identifiant = await IdentifiantLibreAsync(db),
            // 22 caractères base64url, comme les clés que le firmware connaît déjà.
            Cle = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_'),
            EnroleLe = maintenant,
        };
        db.AppareilsAffichage.Add(appareil);
        await db.SaveChangesAsync();
        return appareil;
    }

    /// <summary>L'appareil désigné par la MAC dont la clé correspond — sinon null.</summary>
    public static async Task<AppareilAffichage?> AuthentifierAsync(
        HouseOsDbContext db, string? adresseMac, string? cle)
    {
        if (string.IsNullOrWhiteSpace(adresseMac) || string.IsNullOrWhiteSpace(cle))
        {
            return null;
        }
        var mac = adresseMac.Trim().ToUpperInvariant();
        var appareil = await db.AppareilsAffichage.SingleOrDefaultAsync(a => a.AdresseMac == mac);
        return appareil is not null && MemesSecrets(cle, appareil.Cle) ? appareil : null;
    }

    /// <summary>Le jeton qui rend l'URL d'une image non devinable : HMAC du nom de
    /// fichier avec la clé de l'appareil, tronqué à 32 hex.</summary>
    public static string JetonImage(AppareilAffichage appareil, string fichier) =>
        Convert.ToHexString(HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(appareil.Cle), Encoding.UTF8.GetBytes(fichier)))[..32].ToLowerInvariant();

    public static bool JetonImageValide(AppareilAffichage appareil, string fichier, string jeton) =>
        MemesSecrets(jeton, JetonImage(appareil, fichier));

    public static Task<List<AppareilAffichageDto>> ListerAsync(HouseOsDbContext db) =>
        db.AppareilsAffichage.AsNoTracking()
            .OrderBy(a => a.EnroleLe)
            .Select(a => VersDto(a))
            .ToListAsync();

    public static async Task<AppareilAffichageDto?> RenommerAsync(HouseOsDbContext db, Guid id, string? nom)
    {
        var appareil = await db.AppareilsAffichage.FindAsync(id);
        if (appareil is null)
        {
            return null;
        }
        appareil.Nom = string.IsNullOrWhiteSpace(nom) ? null : nom.Trim();
        await db.SaveChangesAsync();
        return VersDto(appareil);
    }

    /// <summary>Révoquer = supprimer : au prochain réveil l'appareil est inconnu et
    /// devra se ré-enrôler (nouvelle clé).</summary>
    public static async Task<bool> SupprimerAsync(HouseOsDbContext db, Guid id)
    {
        var appareil = await db.AppareilsAffichage.FindAsync(id);
        if (appareil is null)
        {
            return false;
        }
        db.AppareilsAffichage.Remove(appareil);
        await db.SaveChangesAsync();
        return true;
    }

    public static AppareilAffichageDto VersDto(AppareilAffichage a) => new(
        a.Id, a.Identifiant, a.AdresseMac, a.Nom, a.Modele, a.Largeur, a.Hauteur, a.VersionFirmware,
        a.TensionPile, Telemetrie.PileEnPourcent(a.TensionPile), a.Rssi, a.EnroleLe, a.DernierContact,
        a.DernierFichier);

    private static async Task<string> IdentifiantLibreAsync(HouseOsDbContext db)
    {
        while (true)
        {
            var candidat = Convert.ToHexString(RandomNumberGenerator.GetBytes(LongueurIdentifiant / 2));
            if (await db.AppareilsAffichage.AnyAsync(a => a.Identifiant == candidat) == false)
            {
                return candidat;
            }
        }
    }

    private static bool MemesSecrets(string fourni, string attendu)
    {
        var a = Encoding.UTF8.GetBytes(fourni);
        var b = Encoding.UTF8.GetBytes(attendu);
        return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
    }
}
