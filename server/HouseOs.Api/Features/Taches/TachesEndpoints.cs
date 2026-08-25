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

public record ReporterRequete(DateOnly Echeance);

public record NotesRequete(string? Notes);

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
    string? Notes,
    Guid? ZoneId,
    Guid? EquipementId,
    string ModeRecurrence);

public record TacheDto(
    Guid Id,
    string Titre,
    string? Description,
    DateOnly? Echeance,
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
            var (tache, erreur) = await OperationsTaches.PreparerTacheAsync(
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
            var tache = await db.Taches.AsNoTracking()
                .Include(t => t.Occurrences.Where(o => o.Statut == StatutOccurrence.EnAttente))
                .SingleOrDefaultAsync(t => t.Id == id);
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
            if (OperationsTaches.ValiderFiltre(filtre, de, a) is { } erreurFiltre)
            {
                return Erreur(erreurFiltre);
            }
            var aujourdhui = date ?? DateOnly.FromDateTime(DateTime.Now);
            return Results.Ok(await OperationsTaches.ListerOccurrencesAsync(db, filtre, aujourdhui, de, a));
        });

        app.MapGet("/api/journal/bilan", async (
            DateTimeOffset de,
            DateTimeOffset a,
            HouseOsDbContext db) =>
            Results.Ok(await OperationsTaches.BilanCompletionsAsync(db, de, a)));

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

        app.MapPost("/api/occurrences/{id:guid}/annuler-completion", async (
            Guid id,
            HouseOsDbContext db) =>
        {
            var statut = await OperationsTaches.AnnulerCompletionAsync(db, id);
            return statut switch
            {
                StatutAnnulation.Introuvable => Results.NotFound(),
                StatutAnnulation.PasCompletee => Results.Conflict(new { message = "L'occurrence n'est pas complétée." }),
                StatutAnnulation.PasLaDerniere => Results.Conflict(new { message = "Cette complétion n'est pas la plus récente." }),
                StatutAnnulation.ProchaineDejaTraitee => Results.Conflict(new { message = "La prochaine occurrence a déjà été traitée." }),
                StatutAnnulation.JournalManquant => Results.Conflict(new { message = "Aucune entrée de journal pour cette complétion — rien à annuler." }),
                _ => Results.NoContent(),
            };
        });

        app.MapPost("/api/occurrences/{id:guid}/passer", async (
            Guid id,
            ClaimsPrincipal principal,
            HouseOsDbContext db) =>
        {
            var resultat = await OperationsTaches.PasserAsync(
                db, id, principal.IdUtilisateur(), DateTimeOffset.UtcNow);
            return resultat.Statut switch
            {
                StatutPasse.Introuvable => Results.NotFound(),
                StatutPasse.DejaTraitee => Results.Conflict(new { message = "Occurrence déjà traitée." }),
                StatutPasse.TachePonctuelle => Erreur(new ErreurValidation(
                    "occurrence", "Une tâche ponctuelle ne se passe pas.")),
                _ => Results.NoContent(),
            };
        });

        app.MapPost("/api/occurrences/{id:guid}/reporter", async (
            Guid id,
            ReporterRequete requete,
            HouseOsDbContext db) =>
        {
            var statut = await OperationsTaches.ReporterAsync(
                db, id, requete.Echeance, DateOnly.FromDateTime(DateTime.Now));
            return statut switch
            {
                StatutReport.Introuvable => Results.NotFound(),
                StatutReport.DejaTraitee => Results.Conflict(new { message = "Occurrence déjà traitée." }),
                StatutReport.DateInvalide => Erreur(new ErreurValidation(
                    "echeance", "L'échéance reportée ne peut pas être dans le passé.")),
                _ => Results.NoContent(),
            };
        });

        app.MapPut("/api/occurrences/{id:guid}/notes", async (
            Guid id,
            NotesRequete requete,
            HouseOsDbContext db) =>
        {
            var statut = await OperationsTaches.AjouterNotesAsync(db, id, requete.Notes);
            return statut switch
            {
                StatutNotes.Introuvable => Results.NotFound(),
                StatutNotes.PasCompletee => Results.Conflict(new { message = "L'occurrence n'est pas complétée." }),
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
