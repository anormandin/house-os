using HouseOs.Api.Domaine;
using HouseOs.Api.Features.Taches;
using HouseOs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HouseOs.Tests.Integration;

/// <summary>
/// Contrat d'erreur des courses d'écriture, joué sur le vrai Postgres (le filtre
/// PostgresException 23505 ne se déclenche pas sur Sqlite). L'écriture concurrente
/// est simulée en attachant au contexte une occurrence EnAttente conflictuelle :
/// le SaveChanges de l'opération la flushe et l'index unique « une seule occurrence
/// en attente par tâche » refuse, exactement comme si l'autre requête avait gagné.
/// </summary>
[Collection("integration")]
public class CoursesEcritureTests(HouseOsFactory factory)
{
    /// <summary>Une tâche à intervalle fraîche ; retourne le contexte du scope appelant.</summary>
    private static async Task<(Utilisateur Alain, Occurrence EnAttente)> CreerTondre(HouseOsDbContext db)
    {
        var alain = await db.Utilisateurs.SingleAsync(u => u.NomUtilisateur == "alain");
        var spec = new SpecRecurrence { Mode = ModeRecurrence.Intervalle, IntervalleJours = 7 };
        var tache = Tache.CreerRecurrente(
            $"Course {Guid.NewGuid():N}", null, spec, StrategieAssignation.Fixe, alain.Id,
            DateOnly.FromDateTime(DateTime.Now), alain.Id, DateTimeOffset.UtcNow,
            DateOnly.FromDateTime(DateTime.Now));
        db.Taches.Add(tache);
        await db.SaveChangesAsync();
        return (alain, tache.Occurrences.Single());
    }

    private static Occurrence OccurrenceConflictuelle(Guid tacheId) => new()
    {
        Id = Guid.NewGuid(),
        TacheId = tacheId,
        Echeance = DateOnly.FromDateTime(DateTime.Now),
    };

    [Fact]
    public async Task Completer_perdant_de_la_course_repond_deja_completee()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HouseOsDbContext>();
        var (alain, enAttente) = await CreerTondre(db);
        db.Occurrences.Add(OccurrenceConflictuelle(enAttente.TacheId));

        var resultat = await OperationsTaches.CompleterAsync(
            db, enAttente.Id, alain.Id, null, DateTimeOffset.UtcNow);

        Assert.Equal(StatutCompletion.DejaCompletee, resultat.Statut);
        // Rien n'a été sauvegardé : l'occurrence est toujours en attente, sans journal.
        Assert.Equal(StatutOccurrence.EnAttente, await db.Occurrences.AsNoTracking()
            .Where(o => o.Id == enAttente.Id).Select(o => o.Statut).SingleAsync());
        Assert.False(await db.Journal.AsNoTracking().AnyAsync(j => j.TacheId == enAttente.TacheId));
    }

    [Fact]
    public async Task Completer_dont_l_echec_n_est_pas_une_violation_d_unicite_relance_l_erreur()
    {
        // T5 (issue #53) : une erreur de sauvegarde étrangère à la course (ici une FK
        // inexistante) ne doit pas être maquillée en « déjà complétée ».
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HouseOsDbContext>();
        var (alain, enAttente) = await CreerTondre(db);
        db.Occurrences.Add(OccurrenceConflictuelle(Guid.NewGuid()));

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            OperationsTaches.CompleterAsync(db, enAttente.Id, alain.Id, null, DateTimeOffset.UtcNow));
    }

    [Fact]
    public async Task Annuler_perdant_de_la_course_repond_conflit_au_lieu_de_500()
    {
        // Issue #32 : AnnulerCompletionAsync sauvegardait sans catch — un 500 brut là
        // où Completer/Passer répondent 409.
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HouseOsDbContext>();
        var (alain, enAttente) = await CreerTondre(db);
        await OperationsTaches.CompleterAsync(db, enAttente.Id, alain.Id, null, DateTimeOffset.UtcNow);
        // La complétion concurrente de la suivante matérialise une nouvelle EnAttente
        // pendant que l'annulation réactive l'ancienne occurrence.
        db.Occurrences.Add(OccurrenceConflictuelle(enAttente.TacheId));

        var statut = await OperationsTaches.AnnulerCompletionAsync(db, enAttente.Id);

        Assert.Equal(StatutAnnulation.ProchaineDejaTraitee, statut);
        // L'annulation n'a rien écrit : la complétion et son journal sont intacts.
        Assert.Equal(StatutOccurrence.Completee, await db.Occurrences.AsNoTracking()
            .Where(o => o.Id == enAttente.Id).Select(o => o.Statut).SingleAsync());
        Assert.True(await db.Journal.AsNoTracking().AnyAsync(j => j.OccurrenceId == enAttente.Id));
    }
}
