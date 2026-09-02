using HouseOs.Api.Domaine;
using HouseOs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HouseOs.Api.Features.Documents;

/// <summary>Métadonnées d'un document à créer — tout est facultatif : le titre se
/// déduit du nom de fichier, la catégorie du type MIME.</summary>
public record DocumentDonneesCreation(
    string? Titre = null,
    CategorieDocument? Categorie = null,
    Guid? EquipementId = null,
    Guid? ZoneId = null,
    string? Dossier = null,
    string? Notes = null,
    DateOnly? DateDocument = null,
    DateOnly? Echeance = null,
    bool AClasser = false,
    Guid? ImportCourrielId = null);

public record ErreurDocument(string Champ, string Message);

/// <summary>
/// Le seul chemin qui écrit un document (fichier + ligne) : le téléversement web et
/// l'ingestion par courriel passent tous deux ici, avec les mêmes validations avant
/// tout octet sur disque, le même nom disque (id + extension dérivée du MIME) et le
/// même rollback si la base refuse la ligne.
/// </summary>
public static class EnregistrementDocument
{
    /// <summary>Type d'un courriel archivé tel quel (.eml) — accepté par l'ingestion
    /// seulement, jamais par le téléversement web.</summary>
    public const string TypeMimeCourriel = "message/rfc822";

    public static readonly string[] TypesMimePermisIngestion =
        [.. DocumentsEndpoints.TypesMimePermis, TypeMimeCourriel];

    /// <summary>Type MIME tel qu'on le compare : minuscules, sans paramètres, alias
    /// <c>image/jpg</c> ramené à <c>image/jpeg</c>.</summary>
    public static string NormaliserTypeMime(string? brut)
    {
        var typeMime = (brut ?? "").Split(';')[0].Trim().ToLowerInvariant();
        return typeMime == "image/jpg" ? "image/jpeg" : typeMime;
    }

    /// <summary>
    /// Valide puis écrit un document. <paramref name="ouvrirContenu"/> est appelé deux
    /// fois (reniflage de l'en-tête, puis copie) — un flux frais à chaque appel évite
    /// d'exiger un flux repositionnable. Retourne le document créé, ou l'erreur de
    /// validation (rien n'a été écrit dans ce cas).
    /// </summary>
    internal static async Task<(Document? Document, ErreurDocument? Erreur)> EnregistrerAsync(
        Func<Stream> ouvrirContenu,
        long taille,
        string? nomFichierClient,
        string? typeMimeBrut,
        DocumentDonneesCreation meta,
        HouseOsDbContext db,
        string dossierFichiers,
        IReadOnlyCollection<string> typesPermis,
        ILogger journal,
        CancellationToken ct = default)
    {
        if (taille <= 0 || taille > DocumentsEndpoints.TailleMax)
        {
            return (null, new("fichier", "Fichier manquant, vide ou trop gros (max 50 Mo)."));
        }
        var typeMime = NormaliserTypeMime(typeMimeBrut);
        if (typesPermis.Contains(typeMime) == false)
        {
            return (null, new("fichier", "Type non permis (PDF ou image)."));
        }
        // Un .eml n'a pas de signature binaire : c'est l'ingestion qui l'a produit.
        if (typeMime != TypeMimeCourriel)
        {
            var entete = new byte[12];
            int octetsLus;
            await using (var lecture = ouvrirContenu())
            {
                octetsLus = await lecture.ReadAtLeastAsync(entete, entete.Length, throwOnEndOfStream: false, ct);
            }
            if (DocumentsEndpoints.ContenuCorrespondAuType(entete.AsSpan(0, octetsLus), typeMime) == false)
            {
                return (null, new("fichier",
                    "Le contenu du fichier ne correspond pas à son type annoncé (PDF ou image)."));
            }
        }

        var nomFichier = NettoyerNomFichier(nomFichierClient, typeMime);
        var titre = (meta.Titre ?? "").Trim();
        if (titre.Length == 0)
        {
            titre = Path.GetFileNameWithoutExtension(nomFichier);
        }
        if (titre.Length > 200)
        {
            return (null, new("titre", "Le titre ne peut pas dépasser 200 caractères."));
        }
        var notes = Nettoyer(meta.Notes);
        if (notes?.Length > 2000)
        {
            return (null, new("notes", "Les notes ne peuvent pas dépasser 2000 caractères."));
        }
        var dossier = Nettoyer(meta.Dossier);
        if (dossier?.Length > 100)
        {
            return (null, new("dossier", "Le dossier ne peut pas dépasser 100 caractères."));
        }
        // Valider les liens avant d'écrire quoi que ce soit : une violation de FK
        // après l'écriture laisserait un fichier orphelin permanent sur disque.
        if (meta.EquipementId is { } eq && await db.Equipements.AnyAsync(e => e.Id == eq, ct) == false)
        {
            return (null, new("equipementId", "Cet équipement n'existe pas (ou plus)."));
        }
        if (meta.ZoneId is { } z && await db.Zones.AnyAsync(x => x.Id == z, ct) == false)
        {
            return (null, new("zoneId", "Cette pièce n'existe pas (ou plus)."));
        }

        var document = new Document
        {
            Id = Guid.NewGuid(),
            Titre = titre,
            Categorie = meta.Categorie ?? DocumentsEndpoints.CategorieParDefaut(typeMime),
            EquipementId = meta.EquipementId,
            ZoneId = meta.ZoneId,
            Dossier = dossier,
            Notes = notes,
            DateDocument = meta.DateDocument,
            Echeance = meta.Echeance,
            NomFichier = nomFichier,
            CheminDisque = string.Empty,
            TypeMime = typeMime,
            Taille = taille,
            CreeLe = DateTimeOffset.UtcNow,
            AClasser = meta.AClasser,
            ImportCourrielId = meta.ImportCourrielId,
        };
        // Nom disque = id + extension dérivée du type MIME : jamais le nom (ni
        // l'extension) fourni par le client.
        document.CheminDisque = document.Id.ToString("N") + ExtensionPour(typeMime);

        var chemin = Path.Combine(dossierFichiers, document.CheminDisque);
        await using (var flux = File.Create(chemin))
        await using (var contenu = ouvrirContenu())
        {
            await contenu.CopyToAsync(flux, ct);
        }

        db.Documents.Add(document);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch
        {
            File.Delete(chemin);
            journal.LogError(
                "Document {DocumentId} — écriture DB refusée après copie disque ; fichier {Chemin} effacé.",
                document.Id, document.CheminDisque);
            throw;
        }
        return (document, null);
    }

    /// <summary>
    /// Nom d'affichage sûr : sans séparateurs de chemin (le backslash traverse
    /// Path.GetFileName sous Linux), sans caractères de contrôle (CRLF casserait
    /// l'en-tête Content-Disposition), borné à 255 (colonne varchar).
    /// </summary>
    internal static string NettoyerNomFichier(string? nomBrut, string typeMime)
    {
        var nom = Path.GetFileName((nomBrut ?? "").Replace('\\', '/'));
        nom = new string(nom.Where(c => char.IsControl(c) == false).ToArray()).Trim();
        if (nom.Length == 0)
        {
            nom = "document" + ExtensionPour(typeMime);
        }
        return nom.Length > 255 ? nom[^255..] : nom;
    }

    /// <summary>Extension disque dérivée du type MIME validé — jamais du nom client.</summary>
    internal static string ExtensionPour(string typeMime) => typeMime switch
    {
        "application/pdf" => ".pdf",
        "image/jpeg" => ".jpg",
        "image/png" => ".png",
        "image/webp" => ".webp",
        "image/heic" => ".heic",
        TypeMimeCourriel => ".eml",
        _ => ".bin",
    };

    internal static string? Nettoyer(string? valeur) =>
        string.IsNullOrWhiteSpace(valeur) ? null : valeur.Trim();
}
