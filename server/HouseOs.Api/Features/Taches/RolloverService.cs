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

            await Task.Delay(DelaiProchainPassage(DateTimeOffset.Now), stoppingToken);
        }
    }

    /// <summary>
    /// Délai jusqu'au prochain minuit local + 5 minutes. L'offset est celui de la date
    /// cible (les nuits de changement d'heure, l'offset courant donnerait un passage à
    /// 23 h 05 ou 01 h 05), et le délai est borné à une minute au plancher — un délai
    /// négatif tuerait le service via ArgumentOutOfRangeException.
    /// </summary>
    internal static TimeSpan DelaiProchainPassage(DateTimeOffset maintenant)
    {
        var cible = DateOnly.FromDateTime(maintenant.LocalDateTime).AddDays(1)
            .ToDateTime(new TimeOnly(0, 5));
        var prochainMinuit = new DateTimeOffset(cible, TimeZoneInfo.Local.GetUtcOffset(cible));
        var delai = prochainMinuit - maintenant;
        return delai < TimeSpan.FromMinutes(1) ? TimeSpan.FromMinutes(1) : delai;
    }

    private async Task GlisserOccurrencesManquees(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HouseOsDbContext>();

        var glissees = await GlisserOccurrencesManquees(db, DateOnly.FromDateTime(DateTime.Now), ct);
        if (glissees > 0)
        {
            logger.LogInformation("Rollover : {Nombre} occurrence(s) glissée(s).", glissees);
        }
    }

    /// <summary>
    /// Le cœur du glissement, extrait pour les tests : seules les occurrences fixes,
    /// rollover actif, à échéance strictement passée, glissent vers la prochaine date
    /// planifiée. Fait SaveChanges. Retourne le nombre d'occurrences glissées.
    /// </summary>
    internal static async Task<int> GlisserOccurrencesManquees(
        HouseOsDbContext db,
        DateOnly aujourdhui,
        CancellationToken ct = default)
    {
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
        }
        return glissees;
    }
}
