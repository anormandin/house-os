using HouseOs.Api.Domaine;
using HouseOs.Api.Features.Documents;
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
    int NbDocuments);

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
    List<DocumentDto> Documents,
    List<EntretienDto> Entretiens);

public static class EquipementsEndpoints
{
    public static IEndpointRouteBuilder MapEquipements(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/equipements", async (HouseOsDbContext db) =>
            await db.Equipements
                .OrderBy(e => e.Nom)
                .Select(e => new EquipementResumeDto(
                    e.Id, e.Nom, e.ZoneId, e.Marque, e.Modele, e.FinGarantie,
                    db.Documents.Count(d => d.EquipementId == e.Id)))
                .ToListAsync());

        app.MapGet("/api/equipements/{id:guid}", async (Guid id, HouseOsDbContext db) =>
        {
            var detail = await ChargerDetailAsync(db, id);
            return detail is null ? Results.NotFound() : Results.Ok(detail);
        });

        app.MapPost("/api/equipements", async (EquipementRequete requete, HouseOsDbContext db) =>
        {
            if (await ValiderAsync(requete, db) is { } erreur)
            {
                return erreur;
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
            if (await ValiderAsync(requete, db) is { } erreur)
            {
                return erreur;
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

        app.MapDelete("/api/equipements/{id:guid}", async (Guid id, HouseOsDbContext db) =>
        {
            var equipement = await db.Equipements.FindAsync(id);
            if (equipement is null)
            {
                return Results.NotFound();
            }

            // Les documents liés survivent (FK en SET NULL) — aucun fichier effacé.
            db.Equipements.Remove(equipement);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        return app;
    }

    /// <summary>Détail complet d'un équipement (partagé REST + MCP), null si introuvable.</summary>
    internal static async Task<EquipementDetailDto?> ChargerDetailAsync(HouseOsDbContext db, Guid id)
    {
        var equipement = await db.Equipements.AsNoTracking()
            .SingleOrDefaultAsync(e => e.Id == id);
        if (equipement is null)
        {
            return null;
        }

        var documents = await db.Documents.AsNoTracking()
            .Where(d => d.EquipementId == id)
            .OrderBy(d => d.CreeLe)
            .Select(d => new DocumentDto(
                d.Id, d.Titre, d.Categorie.ToString(),
                d.EquipementId, equipement.Nom,
                d.ZoneId, db.Zones.Where(z => z.Id == d.ZoneId).Select(z => z.Nom).FirstOrDefault(),
                d.Notes, d.DateDocument, d.Echeance,
                d.NomFichier, d.TypeMime, d.Taille, d.CreeLe))
            .ToListAsync();

        // Historique d'entretien : complétions du journal des tâches actuellement liées
        // à cet équipement. Limite connue : le lien tâche→équipement vit sur la tâche —
        // supprimer une tâche retire donc ses complétions de CET historique (le journal
        // lui-même survit et reste compté dans le bilan).
        var tachesLiees = await db.Taches
            .Where(t => t.EquipementId == id)
            .Select(t => new { t.Id, t.Titre })
            .ToListAsync();
        var idsTaches = tachesLiees.Select(t => t.Id).ToList();
        var titres = tachesLiees.ToDictionary(t => t.Id, t => t.Titre);
        var entretiens = (await db.Journal
                .Where(j => idsTaches.Contains(j.TacheId))
                .OrderByDescending(j => j.CompleteeLe)
                .ThenBy(j => j.Id) // tri secondaire : ordre stable à instants égaux
                .Take(20)
                .Join(db.Utilisateurs, j => j.UtilisateurId, u => u.Id,
                    (j, u) => new { j.TacheId, j.CompleteeLe, u.NomAffichage, j.Notes })
                .ToListAsync())
            .Select(j => new EntretienDto(
                j.CompleteeLe, j.NomAffichage, titres.GetValueOrDefault(j.TacheId), j.Notes))
            .ToList();

        return new EquipementDetailDto(
            equipement.Id, equipement.Nom, equipement.ZoneId, equipement.Marque,
            equipement.Modele, equipement.NumeroSerie, equipement.DateAchat,
            equipement.FinGarantie, equipement.Notes, equipement.Specs,
            documents,
            entretiens);
    }

    /// <summary>
    /// Validations partagées POST/PUT : longueurs de colonnes, zone existante, specs
    /// acceptables par jsonb — sans elles, Postgres répond par un 500.
    /// </summary>
    private static async Task<IResult?> ValiderAsync(EquipementRequete requete, HouseOsDbContext db)
    {
        if (string.IsNullOrWhiteSpace(requete.Nom))
        {
            return Erreur("nom", "Le nom est requis.");
        }
        if (requete.Nom.Trim().Length > 200)
        {
            return Erreur("nom", "Le nom ne peut pas dépasser 200 caractères.");
        }
        if (requete.Marque?.Trim().Length > 100 || requete.Modele?.Trim().Length > 100
            || requete.NumeroSerie?.Trim().Length > 100)
        {
            return Erreur("equipement", "Marque, modèle et numéro de série sont limités à 100 caractères.");
        }
        if (requete.ZoneId is { } zoneId && await db.Zones.AnyAsync(z => z.Id == zoneId) == false)
        {
            return Erreur("zoneId", "Cette pièce n'existe pas (ou plus).");
        }
        // Postgres refuse le caractère nul dans un jsonb (erreur 22P05).
        if (requete.Specs is not null
            && requete.Specs.Any(s => s.Key.Contains('\0') || s.Value.Contains('\0')))
        {
            return Erreur("specs", "Les specs ne peuvent pas contenir de caractère nul.");
        }
        return null;
    }

    internal static void Appliquer(EquipementRequete requete, Equipement equipement)
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
