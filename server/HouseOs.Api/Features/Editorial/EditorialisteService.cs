using HouseOs.Api.Features.Affichage;
using HouseOs.Api.Features.FondsDeTiroir;
using HouseOs.Api.Features.Humeur;
using HouseOs.Api.Features.Meteo;
using HouseOs.Api.Infrastructure;
using Microsoft.Extensions.Options;

namespace HouseOs.Api.Features.Editorial;

/// <summary>
/// L'éditorialiste, au créneau du matin (celui du titre d'humeur), sur le patron de
/// <see cref="HumeurService"/> : rattrapage au démarrage, plancher d'une minute, aucun
/// appel dans le chemin de requête. Il se réveille aussi quand le rendu lui a laissé
/// un gabarit à réécrire (<see cref="SignalDeReedition"/>) — plancher qui change,
/// édition manquante.
///
/// <para>L'édition visée est toujours <b>celle du jour civil</b>, même au rattrapage de
/// trois heures du matin : contrairement à la phrase du soir, il n'y a pas de créneau
/// de la veille à couvrir, et une édition écrite à trois heures avec les faits de trois
/// heures est celle de la journée qui commence. Écrire la veille avec les faits
/// d'aujourd'hui, c'est ce qu'il faut éviter.</para>
/// </summary>
public class EditorialisteService(
    IServiceScopeFactory scopeFactory,
    IRedacteurEdition redacteur,
    SignalDeReedition signal,
    IOptions<HumeurOptions> humeur,
    IOptions<MeteoOptions> meteo,
    IOptions<AffichageOptions> affichage,
    BanqueDuHasard banque,
    ILogger<EditorialisteService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (stoppingToken.IsCancellationRequested == false)
        {
            try
            {
                var maintenant = DateTime.Now;
                await GenererSiNecessaire(DateDEdition(maintenant), maintenant, stoppingToken);
            }
            catch (Exception ex) when (stoppingToken.IsCancellationRequested == false)
            {
                logger.LogError(ex, "Édition : échec de l'éditorialiste.");
            }

            var delai = ProchainCreneau(DateTimeOffset.Now) - DateTimeOffset.Now;
            if (delai < TimeSpan.FromMinutes(1))
            {
                delai = TimeSpan.FromMinutes(1);
            }
            await signal.AttendreAsync(delai, stoppingToken);
        }
    }

    /// <summary>La journée que l'édition couvre : avant l'heure du matin, la journée
    /// éditoriale n'a pas commencé et on est encore sur celle de la veille.</summary>
    internal DateOnly DateDEdition(DateTime maintenant)
    {
        var date = DateOnly.FromDateTime(maintenant);
        return TimeOnly.FromDateTime(maintenant) >= humeur.Value.HeureMatin ? date : date.AddDays(-1);
    }

    internal DateTimeOffset ProchainCreneau(DateTimeOffset maintenant)
    {
        var heure = TimeOnly.FromDateTime(maintenant.LocalDateTime);
        var date = DateOnly.FromDateTime(maintenant.LocalDateTime);
        var prochaine = heure < humeur.Value.HeureMatin
            ? date.ToDateTime(humeur.Value.HeureMatin)
            : date.AddDays(1).ToDateTime(humeur.Value.HeureMatin);
        // Offset de la date CIBLE, comme le titre d'humeur ; une minute de marge.
        return new DateTimeOffset(prochaine, TimeZoneInfo.Local.GetUtcOffset(prochaine)).AddMinutes(1);
    }

    private async Task GenererSiNecessaire(DateOnly date, DateTime maintenant, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HouseOsDbContext>();
        await GenerationEdition.GenererAsync(
            db, redacteur, meteo.Value, banque, affichage.Value.Lieu, logger, date, maintenant,
            remplacer: false, ct);
    }
}
