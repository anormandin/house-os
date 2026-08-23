using System.Security.Claims;
using HouseOs.Api.Domaine;
using HouseOs.Api.Features.Auth;
using HouseOs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HouseOs.Api.Features.Taches;

public record CreerTacheRequete(string Titre, string? Description, DateOnly? Echeance, Guid? AssigneAId);
public record CompleterRequete(string? Notes);

public record OccurrenceDto(
    Guid Id,
    Guid TacheId,
    string Titre,
    string? Description,
    DateOnly? Echeance,
    string Statut,
    UtilisateurDto? AssigneA,
    UtilisateurDto? CompleteePar,
    DateTimeOffset? CompleteeLe);

public static class TachesEndpoints
{
    public static IEndpointRouteBuilder MapTaches(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/utilisateurs", async (HouseOsDbContext db) =>
            await db.Utilisateurs
                .OrderBy(u => u.NomAffichage)
                .Select(u => new UtilisateurDto(u.Id, u.NomUtilisateur, u.NomAffichage))
                .ToListAsync());

        app.MapPost("/api/taches", async (
            CreerTacheRequete requete,
            ClaimsPrincipal principal,
            HouseOsDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(requete.Titre))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["titre"] = ["Le titre est requis."],
                });
            }

            var tache = Tache.CreerPonctuelle(
                requete.Titre.Trim(),
                string.IsNullOrWhiteSpace(requete.Description) ? null : requete.Description.Trim(),
                requete.Echeance,
                requete.AssigneAId,
                principal.IdUtilisateur(),
                DateTimeOffset.UtcNow);

            db.Taches.Add(tache);
            await db.SaveChangesAsync();
            return Results.Created($"/api/taches/{tache.Id}", new { tache.Id });
        });

        app.MapGet("/api/occurrences", async (
            string? filtre,
            DateOnly? date,
            HouseOsDbContext db) =>
        {
            var aujourdhui = date ?? DateOnly.FromDateTime(DateTime.Now);
            var requete = db.Occurrences.AsNoTracking()
                .Include(o => o.Tache!).ThenInclude(t => t.AssigneA)
                .Include(o => o.CompleteePar)
                .AsQueryable();

            requete = filtre switch
            {
                "aujourdhui" => requete.Where(o =>
                    o.Statut == StatutOccurrence.EnAttente && o.Echeance != null && o.Echeance <= aujourdhui),
                "avenir" => requete.Where(o =>
                    o.Statut == StatutOccurrence.EnAttente && (o.Echeance == null || o.Echeance > aujourdhui)),
                "en-attente" => requete.Where(o => o.Statut == StatutOccurrence.EnAttente),
                "completees" => requete.Where(o => o.Statut == StatutOccurrence.Completee),
                _ => requete,
            };

            var occurrences = await requete
                .OrderBy(o => o.Echeance == null)
                .ThenBy(o => o.Echeance)
                .ThenByDescending(o => o.CompleteeLe)
                .Take(200)
                .Select(o => new OccurrenceDto(
                    o.Id,
                    o.TacheId,
                    o.Tache!.Titre,
                    o.Tache.Description,
                    o.Echeance,
                    o.Statut.ToString(),
                    o.Tache.AssigneA == null
                        ? null
                        : new UtilisateurDto(o.Tache.AssigneA.Id, o.Tache.AssigneA.NomUtilisateur, o.Tache.AssigneA.NomAffichage),
                    o.CompleteePar == null
                        ? null
                        : new UtilisateurDto(o.CompleteePar.Id, o.CompleteePar.NomUtilisateur, o.CompleteePar.NomAffichage),
                    o.CompleteeLe))
                .ToListAsync();

            return Results.Ok(occurrences);
        });

        app.MapPost("/api/occurrences/{id:guid}/completer", async (
            Guid id,
            CompleterRequete? requete,
            ClaimsPrincipal principal,
            HouseOsDbContext db) =>
        {
            var occurrence = await db.Occurrences
                .Include(o => o.Tache)
                .SingleOrDefaultAsync(o => o.Id == id);
            if (occurrence is null)
            {
                return Results.NotFound();
            }
            if (occurrence.Statut == StatutOccurrence.Completee)
            {
                return Results.Conflict(new { message = "Occurrence déjà complétée." });
            }

            var maintenant = DateTimeOffset.UtcNow;
            var entree = occurrence.Completer(principal.IdUtilisateur(), maintenant, requete?.Notes);
            db.Journal.Add(entree);

            var prochaine = occurrence.Tache!.GenererProchaineOccurrence(DateOnly.FromDateTime(maintenant.LocalDateTime));
            if (prochaine is not null)
            {
                db.Occurrences.Add(prochaine);
            }

            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        app.MapDelete("/api/taches/{id:guid}", async (Guid id, HouseOsDbContext db) =>
        {
            var tache = await db.Taches.FindAsync(id);
            if (tache is null)
            {
                return Results.NotFound();
            }

            db.Taches.Remove(tache);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        return app;
    }
}
