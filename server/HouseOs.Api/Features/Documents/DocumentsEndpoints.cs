using System.Globalization;
using HouseOs.Api.Domaine;
using HouseOs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace HouseOs.Api.Features.Documents;

public record DocumentDto(
    Guid Id,
    string Titre,
    string Categorie,
    Guid? EquipementId,
    string? NomEquipement,
    Guid? ZoneId,
    string? NomZone,
    string? Notes,
    DateOnly? DateDocument,
    DateOnly? Echeance,
    string NomFichier,
    string TypeMime,
    long Taille,
    DateTimeOffset CreeLe);

public record DocumentRequete(
    string Titre,
    string Categorie,
    Guid? EquipementId,
    Guid? ZoneId,
    string? Notes,
    DateOnly? DateDocument,
    DateOnly? Echeance);

public static class DocumentsEndpoints
{
    public const long TailleMax = 50 * 1024 * 1024;
    public static readonly string[] TypesMimePermis =
        ["application/pdf", "image/jpeg", "image/png", "image/webp", "image/heic"];

    /// <summary>Côté maximal (px) des miniatures servies aux listes et fiches.</summary>
    public const int CoteMiniature = 512;

    /// <summary>Dossier des fichiers téléversés (config Fichiers:Chemin, créé au besoin).</summary>
    public static string DossierFichiers(IConfiguration config, IWebHostEnvironment env)
    {
        var chemin = config["Fichiers:Chemin"] ?? Path.Combine(env.ContentRootPath, "donnees", "fichiers");
        Directory.CreateDirectory(chemin);
        return chemin;
    }

    /// <summary>Cache disque des miniatures, sous-dossier du dossier des fichiers.</summary>
    public static string DossierMiniatures(IConfiguration config, IWebHostEnvironment env)
    {
        var chemin = Path.Combine(DossierFichiers(config, env), "miniatures");
        Directory.CreateDirectory(chemin);
        return chemin;
    }

    /// <summary>
    /// Génère la miniature WebP d'une image (EXIF redressé, réduite à <see cref="CoteMiniature"/>,
    /// jamais agrandie). False si le fichier n'est pas décodable (HEIC, corrompu…).
    /// </summary>
    public static async Task<bool> GenererMiniatureAsync(string source, string destination)
    {
        try
        {
            // Bombe de décompression : un PNG de 30000×30000 tient dans quelques
            // centaines de Ko compressés mais exige des Go une fois décodé — on lit
            // les dimensions dans l'en-tête avant de décoder quoi que ce soit.
            var info = await Image.IdentifyAsync(source);
            if ((long)info.Width * info.Height > 64_000_000)
            {
                return false;
            }

            using var image = await Image.LoadAsync(source);
            image.Mutate(op => op.AutoOrient());
            if (image.Width > CoteMiniature || image.Height > CoteMiniature)
            {
                image.Mutate(op => op.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(CoteMiniature, CoteMiniature),
                }));
            }
            var temporaire = $"{destination}.{Guid.NewGuid():N}.tmp";
            try
            {
                await image.SaveAsWebpAsync(temporaire);
                File.Move(temporaire, destination, overwrite: true);
            }
            catch
            {
                // Ne jamais laisser de .tmp orphelin dans le cache (rien ne le balaie).
                File.Delete(temporaire);
                throw;
            }
            return true;
        }
        catch (Exception e) when (
            e is ImageFormatException
                or NotSupportedException
                or SixLabors.ImageSharp.Memory.InvalidMemoryOperationException)
        {
            return false;
        }
    }

    /// <summary>Catégorie déduite du type MIME quand l'utilisateur n'en fournit pas.</summary>
    public static CategorieDocument CategorieParDefaut(string typeMime) =>
        typeMime switch
        {
            "application/pdf" => CategorieDocument.Manuel,
            _ when typeMime.StartsWith("image/") => CategorieDocument.Photo,
            _ => CategorieDocument.Autre,
        };

    public static IEndpointRouteBuilder MapDocuments(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/documents", async (string? categorie, Guid? equipementId, HouseOsDbContext db) =>
        {
            var documents = db.Documents.AsNoTracking();
            if (string.IsNullOrWhiteSpace(categorie) == false)
            {
                if (Enum.TryParse<CategorieDocument>(categorie, out var cat) == false)
                {
                    return Erreur("categorie", "Catégorie inconnue.");
                }
                documents = documents.Where(d => d.Categorie == cat);
            }
            if (equipementId is not null)
            {
                documents = documents.Where(d => d.EquipementId == equipementId);
            }

            var liste = await documents
                .OrderByDescending(d => d.CreeLe)
                .Select(d => new DocumentDto(
                    d.Id, d.Titre, d.Categorie.ToString(),
                    d.EquipementId,
                    db.Equipements.Where(e => e.Id == d.EquipementId).Select(e => e.Nom).FirstOrDefault(),
                    d.ZoneId,
                    db.Zones.Where(z => z.Id == d.ZoneId).Select(z => z.Nom).FirstOrDefault(),
                    d.Notes, d.DateDocument, d.Echeance,
                    d.NomFichier, d.TypeMime, d.Taille, d.CreeLe))
                .ToListAsync();
            return Results.Ok(liste);
        });

        app.MapPost("/api/documents", async (
            HttpRequest requete,
            HouseOsDbContext db,
            IConfiguration config,
            IWebHostEnvironment env) =>
        {
            var formulaire = await requete.ReadFormAsync();
            var fichier = formulaire.Files.GetFile("fichier");
            if (fichier is null || fichier.Length == 0 || fichier.Length > TailleMax)
            {
                return Erreur("fichier", "Fichier manquant, vide ou trop gros (max 50 Mo).");
            }
            // Les types MIME sont insensibles à la casse et peuvent porter des paramètres
            // (« ; charset=… ») ; certains clients envoient image/jpg.
            var typeMime = (fichier.ContentType ?? "").Split(';')[0].Trim().ToLowerInvariant();
            if (typeMime == "image/jpg")
            {
                typeMime = "image/jpeg";
            }
            if (TypesMimePermis.Contains(typeMime) == false)
            {
                return Erreur("fichier", "Type non permis (PDF ou image).");
            }

            var categorie = CategorieParDefaut(typeMime);
            var categorieBrute = formulaire["categorie"].ToString();
            if (string.IsNullOrWhiteSpace(categorieBrute) == false
                && Enum.TryParse(categorieBrute, out categorie) == false)
            {
                return Erreur("categorie", "Catégorie inconnue.");
            }

            var nomFichier = NettoyerNomFichier(fichier.FileName, typeMime);
            var titre = formulaire["titre"].ToString().Trim();
            if (titre.Length == 0)
            {
                titre = Path.GetFileNameWithoutExtension(nomFichier);
            }
            if (titre.Length > 200)
            {
                return Erreur("titre", "Le titre ne peut pas dépasser 200 caractères.");
            }
            var notes = Nettoyer(formulaire["notes"]);
            if (notes?.Length > 2000)
            {
                return Erreur("notes", "Les notes ne peuvent pas dépasser 2000 caractères.");
            }
            // Valider les liens avant d'écrire quoi que ce soit : une violation de FK
            // après l'écriture laisserait un fichier orphelin permanent sur disque.
            var equipementId = LireGuid(formulaire["equipementId"]);
            if (equipementId is { } eq && await db.Equipements.AnyAsync(e => e.Id == eq) == false)
            {
                return Erreur("equipementId", "Cet équipement n'existe pas (ou plus).");
            }
            var zoneId = LireGuid(formulaire["zoneId"]);
            if (zoneId is { } z && await db.Zones.AnyAsync(x => x.Id == z) == false)
            {
                return Erreur("zoneId", "Cette pièce n'existe pas (ou plus).");
            }

            var document = new Document
            {
                Id = Guid.NewGuid(),
                Titre = titre,
                Categorie = categorie,
                EquipementId = equipementId,
                ZoneId = zoneId,
                Notes = notes,
                DateDocument = LireDate(formulaire["dateDocument"]),
                Echeance = LireDate(formulaire["echeance"]),
                NomFichier = nomFichier,
                CheminDisque = string.Empty,
                TypeMime = typeMime,
                Taille = fichier.Length,
                CreeLe = DateTimeOffset.UtcNow,
            };
            // Nom disque = id + extension dérivée du type MIME : jamais le nom (ni
            // l'extension) fourni par le client.
            document.CheminDisque = document.Id.ToString("N") + ExtensionPour(typeMime);

            var chemin = Path.Combine(DossierFichiers(config, env), document.CheminDisque);
            await using (var flux = File.Create(chemin))
            {
                await fichier.CopyToAsync(flux);
            }

            db.Documents.Add(document);
            try
            {
                await db.SaveChangesAsync();
            }
            catch
            {
                File.Delete(chemin);
                throw;
            }
            return Results.Created($"/api/documents/{document.Id}", new { document.Id });
        }).DisableAntiforgery();

        app.MapPut("/api/documents/{id:guid}", async (Guid id, DocumentRequete requete, HouseOsDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(requete.Titre))
            {
                return Erreur("titre", "Le titre est requis.");
            }
            if (Enum.TryParse<CategorieDocument>(requete.Categorie, out var categorie) == false)
            {
                return Erreur("categorie", "Catégorie inconnue.");
            }
            var document = await db.Documents.FindAsync(id);
            if (document is null)
            {
                return Results.NotFound();
            }

            document.Titre = requete.Titre.Trim();
            document.Categorie = categorie;
            document.EquipementId = requete.EquipementId;
            document.ZoneId = requete.ZoneId;
            document.Notes = Nettoyer(requete.Notes);
            document.DateDocument = requete.DateDocument;
            document.Echeance = requete.Echeance;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        app.MapGet("/api/documents/{id:guid}/fichier", async (
            Guid id,
            HouseOsDbContext db,
            IConfiguration config,
            IWebHostEnvironment env) =>
        {
            var document = await db.Documents.FindAsync(id);
            if (document is null)
            {
                return Results.NotFound();
            }
            var chemin = Path.Combine(DossierFichiers(config, env), document.CheminDisque);
            if (File.Exists(chemin) == false)
            {
                return Results.NotFound();
            }
            return Results.File(chemin, document.TypeMime, document.NomFichier);
        });

        app.MapGet("/api/documents/{id:guid}/miniature", async (
            Guid id,
            HouseOsDbContext db,
            IConfiguration config,
            IWebHostEnvironment env) =>
        {
            var document = await db.Documents.FindAsync(id);
            if (document is null || document.TypeMime.StartsWith("image/") == false)
            {
                return Results.NotFound();
            }
            var miniature = Path.Combine(
                DossierMiniatures(config, env),
                Path.GetFileNameWithoutExtension(document.CheminDisque) + ".webp");
            if (File.Exists(miniature) == false)
            {
                var original = Path.Combine(DossierFichiers(config, env), document.CheminDisque);
                if (File.Exists(original) == false
                    || await GenererMiniatureAsync(original, miniature) == false)
                {
                    return Results.NotFound();
                }
            }
            return Results.File(miniature, "image/webp");
        });

        app.MapDelete("/api/documents/{id:guid}", async (
            Guid id,
            HouseOsDbContext db,
            IConfiguration config,
            IWebHostEnvironment env) =>
                await SupprimerAsync(db, id, config, env) ? Results.NoContent() : Results.NotFound());

        return app;
    }

    /// <summary>Supprime un document et son fichier disque (partagé REST + MCP), false si introuvable.</summary>
    internal static async Task<bool> SupprimerAsync(
        HouseOsDbContext db, Guid id, IConfiguration config, IWebHostEnvironment env)
    {
        var document = await db.Documents.FindAsync(id);
        if (document is null)
        {
            return false;
        }
        var chemin = Path.Combine(DossierFichiers(config, env), document.CheminDisque);
        var miniature = Path.Combine(
            DossierMiniatures(config, env),
            Path.GetFileNameWithoutExtension(document.CheminDisque) + ".webp");
        db.Documents.Remove(document);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Deux suppressions concurrentes : l'autre a gagné — introuvable, pas 500.
            return false;
        }
        if (File.Exists(chemin))
        {
            File.Delete(chemin);
        }
        if (File.Exists(miniature))
        {
            File.Delete(miniature);
        }
        return true;
    }

    /// <summary>
    /// Nom d'affichage sûr : sans séparateurs de chemin (le backslash traverse
    /// Path.GetFileName sous Linux), sans caractères de contrôle (CRLF casserait
    /// l'en-tête Content-Disposition), borné à 255 (colonne varchar).
    /// </summary>
    private static string NettoyerNomFichier(string? nomBrut, string typeMime)
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
    private static string ExtensionPour(string typeMime) => typeMime switch
    {
        "application/pdf" => ".pdf",
        "image/jpeg" => ".jpg",
        "image/png" => ".png",
        "image/webp" => ".webp",
        "image/heic" => ".heic",
        _ => ".bin",
    };

    private static Guid? LireGuid(string? valeur) =>
        Guid.TryParse(valeur, out var id) ? id : null;

    private static DateOnly? LireDate(string? valeur) =>
        DateOnly.TryParseExact(valeur, "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var date) ? date : null;

    private static string? Nettoyer(string? valeur) =>
        string.IsNullOrWhiteSpace(valeur) ? null : valeur.Trim();

    private static IResult Erreur(string champ, string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { [champ] = [message] });
}
