using HouseOs.Api.Domaine;
using HouseOs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HouseOs.Api.Features.Taches;

/// <summary>
/// Glisse les occurrences fixes manquées vers leur prochaine date planifiée
/// (flag rollover : jamais d'empilement, jamais de culpabilisation). Les tâches à
/// intervalle ne glissent pas — « tondre » reste dû tant que ce n'est pas fait.
/// Tourne au démarrage puis une fois par jour.
/// </summary>
public class RolloverService(IServiceScopeFactory scopeFactory, ILogger<RolloverService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (stoppingToken.IsCancellationRequested == false)
        {
            try
            {
                await GlisserOccurrencesManquees(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Rollover : échec du glissement quotidien.");
            }

            // Prochain passage : minuit local + 5 minutes.
            var maintenant = DateTimeOffset.Now;
            var prochainMinuit = new DateTimeOffset(
                DateOnly.FromDateTime(maintenant.LocalDateTime).AddDays(1)
                    .ToDateTime(new TimeOnly(0, 5)),
                maintenant.Offset);
            await Task.Delay(prochainMinuit - maintenant, stoppingToken);
        }
    }

    private async Task GlisserOccurrencesManquees(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HouseOsDbContext>();
        var aujourdhui = DateOnly.FromDateTime(DateTime.Now);

        var manquees = await db.Occurrences
            .Include(o => o.Tache)
            .Where(o => o.Statut == StatutOccurrence.EnAttente
                && o.Echeance != null && o.Echeance < aujourdhui)
            .ToListAsync(ct);

        var glissees = 0;
        foreach (var occurrence in manquees)
        {
            var spec = occurrence.Tache!.Recurrence;
            if (spec.Mode != ModeRecurrence.Fixe || spec.Rollover == false)
            {
                continue;
            }
            occurrence.Echeance = MoteurRecurrence.ProchainePlanifiee(spec, aujourdhui);
            glissees++;
        }

        if (glissees > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Rollover : {Nombre} occurrence(s) glissée(s).", glissees);
        }
    }
}
