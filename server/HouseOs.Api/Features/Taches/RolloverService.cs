using HouseOs.Api.Domaine;
using HouseOs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HouseOs.Api.Features.Taches;

/// <summary>
/// Glisse les occurrences fixes manquées vers leur prochaine date planifiée
/// (flag rollover : jamais d'empilement, jamais de culpabilisation). Les tâches à
/// intervalle ne glissent pas tant que leur fenêtre saisonnière est ouverte —
/// « tondre » reste dû tant que ce n'est pas fait — mais une fois la fenêtre
/// refermée, l'occurrence échue glisse au début de la prochaine fenêtre au lieu
/// de rester « en retard » toute la morte-saison (décision T7, 2026-08-28),
/// indépendamment du flag rollover (réservé au mode fixe).
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
    /// Le cœur du glissement, extrait pour les tests. Glissent, à échéance strictement
    /// passée : les occurrences fixes à rollover actif (vers la prochaine date
    /// planifiée) et les occurrences à intervalle dont la fenêtre saisonnière s'est
    /// refermée (vers le début de la prochaine fenêtre — décision T7, indépendante du
    /// flag rollover). Fait SaveChanges. Retourne le nombre d'occurrences glissées.
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
            if (spec.Mode == ModeRecurrence.Fixe && spec.Rollover)
            {
                occurrence.Echeance = MoteurRecurrence.ProchainePlanifiee(spec, aujourdhui);
                glissees++;
            }
            else if (spec.Mode == ModeRecurrence.Intervalle && spec.AFenetre
                && spec.DansFenetre(aujourdhui) == false)
            {
                // La fenêtre s'est refermée sur une occurrence échue : elle glisse au
                // début de la prochaine fenêtre (décision T7, 2026-08-28) — « en
                // retard » tout l'hiver ne mènerait à rien. Tant que la fenêtre est
                // ouverte, elle reste due.
                occurrence.Echeance = MoteurRecurrence.DebutProchaineFenetre(spec, aujourdhui);
                glissees++;
            }
        }

        if (glissees > 0)
        {
            await db.SaveChangesAsync(ct);
        }
        return glissees;
    }
}
