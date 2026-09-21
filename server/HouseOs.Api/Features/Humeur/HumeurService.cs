using HouseOs.Api.Domaine.Humeur;
using HouseOs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HouseOs.Api.Features.Humeur;

/// <summary>
/// Matérialise la phrase du jour aux deux créneaux fixes (matin 5 h 30, soir 17 h,
/// configurables) : état structuré → polissage Haiku si une clé est disponible,
/// sinon banque de gabarits. Au démarrage, rattrape le créneau courant s'il manque.
/// </summary>
public class HumeurService(
    IServiceScopeFactory scopeFactory,
    IOptions<HumeurOptions> options,
    ILogger<HumeurService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (stoppingToken.IsCancellationRequested == false)
        {
            try
            {
                var (date, moment) = CreneauCourant(DateTimeOffset.Now);
                await GenererSiManquante(date, moment, stoppingToken);
            }
            // Filtre sur le jeton : un timeout réseau est aussi une
            // OperationCanceledException et ne doit pas tuer le service.
            catch (Exception ex) when (stoppingToken.IsCancellationRequested == false)
            {
                logger.LogError(ex, "Humeur : échec de génération de la phrase du jour.");
            }

            // Plancher d'une minute : un délai négatif (config d'heures farfelue,
            // horloge reculée) ferait lever Task.Delay — hors du try — et tuerait le
            // service en silence jusqu'au prochain redémarrage.
            var delai = ProchainCreneau(DateTimeOffset.Now) - DateTimeOffset.Now;
            if (delai < TimeSpan.FromMinutes(1))
            {
                delai = TimeSpan.FromMinutes(1);
            }
            await Task.Delay(delai, stoppingToken);
        }
    }

    /// <summary>Le créneau que la phrase actuelle doit couvrir : avant l'heure du
    /// matin, on est encore sur le soir de la veille.</summary>
    internal (DateOnly Date, MomentJournee Moment) CreneauCourant(DateTimeOffset maintenant)
    {
        var heure = TimeOnly.FromDateTime(maintenant.LocalDateTime);
        var date = DateOnly.FromDateTime(maintenant.LocalDateTime);
        if (heure >= options.Value.HeureSoir)
        {
            return (date, MomentJournee.Soir);
        }
        if (heure >= options.Value.HeureMatin)
        {
            return (date, MomentJournee.Matin);
        }
        return (date.AddDays(-1), MomentJournee.Soir);
    }

    internal DateTimeOffset ProchainCreneau(DateTimeOffset maintenant)
    {
        var heure = TimeOnly.FromDateTime(maintenant.LocalDateTime);
        var date = DateOnly.FromDateTime(maintenant.LocalDateTime);
        var prochaine = heure < options.Value.HeureMatin
            ? date.ToDateTime(options.Value.HeureMatin)
            : heure < options.Value.HeureSoir
                ? date.ToDateTime(options.Value.HeureSoir)
                : date.AddDays(1).ToDateTime(options.Value.HeureMatin);
        // Offset de la date CIBLE (les nuits de changement d'heure, l'offset courant
        // viserait une heure trop tôt ou trop tard) ; une minute de marge pour être
        // sûr d'atterrir après le créneau.
        return new DateTimeOffset(prochaine, TimeZoneInfo.Local.GetUtcOffset(prochaine)).AddMinutes(1);
    }

    /// <summary>
    /// Le créneau courant, s'il manque. La génération elle-même vit dans
    /// <see cref="GenerationHumeur"/>, partagée avec la régénération demandée à la
    /// main (outil MCP) — un seul prompt, un seul repli.
    /// </summary>
    private async Task GenererSiManquante(DateOnly date, MomentJournee moment, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HouseOsDbContext>();
        await GenerationHumeur.GenererAsync(db, options.Value, logger, date, moment, remplacer: false, ct);
    }
}
