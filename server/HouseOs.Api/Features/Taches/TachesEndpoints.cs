using System.Security.Claims;
using HouseOs.Api.Domaine;
using HouseOs.Api.Features.Auth;
using HouseOs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HouseOs.Api.Features.Taches;

public record RecurrenceDto(
    string Mode,
    string? FixeType,
    int[]? JoursSemaine,
    int? JourDuMois,
    int? MoisAnnuel,
    int? JourAnnuel,
    int? IntervalleJours,
    int? FenetreDebutMois,
    int? FenetreDebutJour,
    int? FenetreFinMois,
    int? FenetreFinJour,
    bool? Rollover);

public record CreerTacheRequete(
    string Titre,
    string? Description,
    DateOnly? Echeance,
    Guid? AssigneAId,
    Guid? ZoneId,
    Guid? EquipementId,
    string? Strategie,
    RecurrenceDto? Recurrence);

public record ModifierTacheRequete(
    string Titre,
    string? Description,
    DateOnly? Echeance,
    Guid? AssigneAId,
    Guid? ZoneId,
    Guid? EquipementId,
    string? Strategie,
    RecurrenceDto? Recurrence);

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
    DateTimeOffset? CompleteeLe,
    Guid? ZoneId,
    Guid? EquipementId,
    string ModeRecurrence);

public record TacheDto(
    Guid Id,
    string Titre,
    string? Description,
    Guid? AssigneAId,
    Guid? ZoneId,
    Guid? EquipementId,
    string Strategie,
    RecurrenceDto Recurrence);

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
            var (tache, erreur) = OperationsTaches.PreparerTache(
                db, requete, principal.IdUtilisateur(),
                DateTimeOffset.UtcNow, DateOnly.FromDateTime(DateTime.Now));
            if (erreur is not null)
            {
                return Erreur(erreur);
            }

            await db.SaveChangesAsync();
            return Results.Created($"/api/taches/{tache!.Id}", new { tache.Id });
        });

        app.MapGet("/api/taches/{id:guid}", async (Guid id, HouseOsDbContext db) =>
        {
            var tache = await db.Taches.AsNoTracking().SingleOrDefaultAsync(t => t.Id == id);
            return tache is null ? Results.NotFound() : Results.Ok(OperationsTaches.VersTacheDto(tache));
        });

        app.MapPut("/api/taches/{id:guid}", async (
            Guid id,
            ModifierTacheRequete requete,
            HouseOsDbContext db) =>
        {
            var tache = await db.Taches
                .Include(t => t.Occurrences.Where(o => o.Statut == StatutOccurrence.EnAttente))
                .SingleOrDefaultAsync(t => t.Id == id);
            if (tache is null)
            {
                return Results.NotFound();
            }

            var erreur = await OperationsTaches.ModifierTacheAsync(
                db, tache, requete, DateOnly.FromDateTime(DateTime.Now));
            if (erreur is not null)
            {
                return Erreur(erreur);
            }

            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        app.MapGet("/api/occurrences", async (
            string? filtre,
            DateOnly? date,
            DateTimeOffset? de,
            DateTimeOffset? a,
            HouseOsDbContext db) =>
        {
            var aujourdhui = date ?? DateOnly.FromDateTime(DateTime.Now);
            return Results.Ok(await OperationsTaches.ListerOccurrencesAsync(db, filtre, aujourdhui, de, a));
        });

        app.MapPost("/api/occurrences/{id:guid}/completer", async (
            Guid id,
            CompleterRequete? requete,
            ClaimsPrincipal principal,
            HouseOsDbContext db) =>
        {
            var resultat = await OperationsTaches.CompleterAsync(
                db, id, principal.IdUtilisateur(), requete?.Notes, DateTimeOffset.UtcNow);
            return resultat.Statut switch
            {
                StatutCompletion.Introuvable => Results.NotFound(),
                StatutCompletion.DejaCompletee => Results.Conflict(new { message = "Occurrence déjà complétée." }),
                _ => Results.NoContent(),
            };
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

    private static IResult Erreur(ErreurValidation erreur) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { [erreur.Champ] = [erreur.Message] });
}
