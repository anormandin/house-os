using HouseOs.Api.Domaine;
using HouseOs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HouseOs.Api.Features.Equipements;

public record EquipementRequete(
    string Nom,
    Guid? ZoneId,
    string? Marque,
    string? Modele,
    string? NumeroSerie,
    DateOnly? DateAchat,
    DateOnly? FinGarantie,
    string? Notes,
    Dictionary<string, string>? Specs);

public record EquipementResumeDto(
    Guid Id,
    string Nom,
    Guid? ZoneId,
    string? Marque,
    string? Modele,
    DateOnly? FinGarantie,
    int NbPiecesJointes);

public record PieceJointeDto(Guid Id, string NomFichier, string TypeMime, long Taille, DateTimeOffset CreeLe);

public record EntretienDto(DateTimeOffset CompleteeLe, string Utilisateur, string? TitreTache, string? Notes);

public record EquipementDetailDto(
    Guid Id,
    string Nom,
    Guid? ZoneId,
    string? Marque,
    string? Modele,
    string? NumeroSerie,
    DateOnly? DateAchat,
    DateOnly? FinGarantie,
    string? Notes,
    Dictionary<string, string> Specs,
    List<PieceJointeDto> PiecesJointes,
    List<EntretienDto> Entretiens);

public static class EquipementsEndpoints
{
    private const long TailleMax = 50 * 1024 * 1024;
    private static readonly string[] TypesMimePermis =
        ["application/pdf", "image/jpeg", "image/png", "image/webp", "image/heic"];

    /// <summary>Dossier des fichiers téléversés (config Fichiers:Chemin, créé au besoin).</summary>
    public static string DossierFichiers(IConfiguration config, IWebHostEnvironment env)
    {
        var chemin = config["Fichiers:Chemin"] ?? Path.Combine(env.ContentRootPath, "donnees", "fichiers");
        Directory.CreateDirectory(chemin);
        return chemin;
    }

    public static IEndpointRouteBuilder MapEquipements(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/equipements", async (HouseOsDbContext db) =>
            await db.Equipements
                .OrderBy(e => e.Nom)
                .Select(e => new EquipementResumeDto(
                    e.Id, e.Nom, e.ZoneId, e.Marque, e.Modele, e.FinGarantie, e.PiecesJointes.Count))
                .ToListAsync());

        app.MapGet("/api/equipements/{id:guid}", async (Guid id, HouseOsDbContext db) =>
        {
            var equipement = await db.Equipements.AsNoTracking()
                .Include(e => e.PiecesJointes)
                .SingleOrDefaultAsync(e => e.Id == id);
            if (equipement is null)
            {
                return Results.NotFound();
            }

            // Historique d'entretien : complétions du journal des tâches liées à cet
            // équipement (le journal survit à la suppression des tâches — jointure lâche).
            var tachesLiees = await db.Taches
                .Where(t => t.EquipementId == id)
                .Select(t => new { t.Id, t.Titre })
                .ToListAsync();
            var idsTaches = tachesLiees.Select(t => t.Id).ToList();
            var titres = tachesLiees.ToDictionary(t => t.Id, t => t.Titre);
            var entretiens = (await db.Journal
                    .Where(j => idsTaches.Contains(j.TacheId))
                    .OrderByDescending(j => j.CompleteeLe)
                    .Take(20)
                    .Join(db.Utilisateurs, j => j.UtilisateurId, u => u.Id,
                        (j, u) => new { j.TacheId, j.CompleteeLe, u.NomAffichage, j.Notes })
                    .ToListAsync())
                .Select(j => new EntretienDto(
                    j.CompleteeLe, j.NomAffichage, titres.GetValueOrDefault(j.TacheId), j.Notes))
                .ToList();

            return Results.Ok(new EquipementDetailDto(
                equipement.Id, equipement.Nom, equipement.ZoneId, equipement.Marque,
                equipement.Modele, equipement.NumeroSerie, equipement.DateAchat,
                equipement.FinGarantie, equipement.Notes, equipement.Specs,
                equipement.PiecesJointes
                    .OrderBy(p => p.CreeLe)
                    .Select(p => new PieceJointeDto(p.Id, p.NomFichier, p.TypeMime, p.Taille, p.CreeLe))
                    .ToList(),
                entretiens));
        });

        app.MapPost("/api/equipements", async (EquipementRequete requete, HouseOsDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(requete.Nom))
            {
                return Erreur("nom", "Le nom est requis.");
            }

            var equipement = new Equipement
            {
                Id = Guid.NewGuid(),
                Nom = string.Empty,
                CreeLe = DateTimeOffset.UtcNow,
            };
            Appliquer(requete, equipement);
            db.Equipements.Add(equipement);
            await db.SaveChangesAsync();
            return Results.Created($"/api/equipements/{equipement.Id}", new { equipement.Id });
        });

        app.MapPut("/api/equipements/{id:guid}", async (Guid id, EquipementRequete requete, HouseOsDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(requete.Nom))
            {
                return Erreur("nom", "Le nom est requis.");
            }
            var equipement = await db.Equipements.FindAsync(id);
            if (equipement is null)
            {
                return Results.NotFound();
            }

            Appliquer(requete, equipement);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        app.MapDelete("/api/equipements/{id:guid}", async (
            Guid id,
            HouseOsDbContext db,
            IConfiguration config,
            IWebHostEnvironment env) =>
        {
            var equipement = await db.Equipements
                .Include(e => e.PiecesJointes)
                .SingleOrDefaultAsync(e => e.Id == id);
            if (equipement is null)
            {
                return Results.NotFound();
            }

            var dossier = DossierFichiers(config, env);
            var fichiers = equipement.PiecesJointes.Select(p => Path.Combine(dossier, p.CheminDisque)).ToList();
            db.Equipements.Remove(equipement);
            await db.SaveChangesAsync();
            foreach (var fichier in fichiers.Where(File.Exists))
            {
                File.Delete(fichier);
            }
            return Results.NoContent();
        });

        app.MapPost("/api/equipements/{id:guid}/pieces-jointes", async (
            Guid id,
            IFormFile fichier,
            HouseOsDbContext db,
            IConfiguration config,
            IWebHostEnvironment env) =>
        {
            var equipement = await db.Equipements.FindAsync(id);
            if (equipement is null)
            {
                return Results.NotFound();
            }
            if (fichier.Length == 0 || fichier.Length > TailleMax)
            {
                return Erreur("fichier", "Fichier vide ou trop gros (max 50 Mo).");
            }
            if (TypesMimePermis.Contains(fichier.ContentType) == false)
            {
                return Erreur("fichier", "Type non permis (PDF ou image).");
            }

            var pieceJointe = new PieceJointe
            {
                Id = Guid.NewGuid(),
                EquipementId = id,
                NomFichier = Path.GetFileName(fichier.FileName),
                CheminDisque = string.Empty,
                TypeMime = fichier.ContentType,
                Taille = fichier.Length,
                CreeLe = DateTimeOffset.UtcNow,
            };
            // Nom disque = id + extension d'origine : jamais le nom fourni par le client.
            pieceJointe.CheminDisque = pieceJointe.Id.ToString("N") + Path.GetExtension(fichier.FileName).ToLowerInvariant();

            var chemin = Path.Combine(DossierFichiers(config, env), pieceJointe.CheminDisque);
            await using (var flux = File.Create(chemin))
            {
                await fichier.CopyToAsync(flux);
            }

            db.PiecesJointes.Add(pieceJointe);
            await db.SaveChangesAsync();
            return Results.Created($"/api/pieces-jointes/{pieceJointe.Id}",
                new PieceJointeDto(pieceJointe.Id, pieceJointe.NomFichier, pieceJointe.TypeMime,
                    pieceJointe.Taille, pieceJointe.CreeLe));
        }).DisableAntiforgery();

        app.MapGet("/api/pieces-jointes/{id:guid}", async (
            Guid id,
            HouseOsDbContext db,
            IConfiguration config,
            IWebHostEnvironment env) =>
        {
            var pieceJointe = await db.PiecesJointes.FindAsync(id);
            if (pieceJointe is null)
            {
                return Results.NotFound();
            }
            var chemin = Path.Combine(DossierFichiers(config, env), pieceJointe.CheminDisque);
            if (File.Exists(chemin) == false)
            {
                return Results.NotFound();
            }
            return Results.File(chemin, pieceJointe.TypeMime, pieceJointe.NomFichier);
        });

        app.MapDelete("/api/pieces-jointes/{id:guid}", async (
            Guid id,
            HouseOsDbContext db,
            IConfiguration config,
            IWebHostEnvironment env) =>
        {
            var pieceJointe = await db.PiecesJointes.FindAsync(id);
            if (pieceJointe is null)
            {
                return Results.NotFound();
            }

            var chemin = Path.Combine(DossierFichiers(config, env), pieceJointe.CheminDisque);
            db.PiecesJointes.Remove(pieceJointe);
            await db.SaveChangesAsync();
            if (File.Exists(chemin))
            {
                File.Delete(chemin);
            }
            return Results.NoContent();
        });

        return app;
    }

    private static void Appliquer(EquipementRequete requete, Equipement equipement)
    {
        equipement.Nom = requete.Nom.Trim();
        equipement.ZoneId = requete.ZoneId;
        equipement.Marque = Nettoyer(requete.Marque);
        equipement.Modele = Nettoyer(requete.Modele);
        equipement.NumeroSerie = Nettoyer(requete.NumeroSerie);
        equipement.DateAchat = requete.DateAchat;
        equipement.FinGarantie = requete.FinGarantie;
        equipement.Notes = Nettoyer(requete.Notes);
        equipement.Specs = requete.Specs ?? [];
    }

    private static string? Nettoyer(string? valeur) =>
        string.IsNullOrWhiteSpace(valeur) ? null : valeur.Trim();

    private static IResult Erreur(string champ, string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { [champ] = [message] });
}
