using HouseOs.Api.Domaine.Editorial;
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
///
/// <para>Quand le modèle n'a pas répondu (API surchargée), l'édition est restée un
/// gabarit : le service repasse <b>une fois</b>, <see cref="DelaiDeReessai"/> plus tard
/// (D-2026-09-21 Réédition En Deux Temps). Une fois, pas à chaque réveil — et jamais
/// sans clé, où le gabarit est le fonctionnement normal.</para>
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
    /// <summary>Le temps qu'on laisse à une API surchargée avant le second essai — et
    /// la fenêtre pendant laquelle il vaut encore la peine : passé deux fois ce délai,
    /// on ne réessaie plus (un redémarrage à midi ne rappelle pas le modèle pour le
    /// gabarit de 5 h 31).</summary>
    public static readonly TimeSpan DelaiDeReessai = TimeSpan.FromHours(1);

    /// <summary>La journée dont le second essai a eu lieu : un seul par jour, posé
    /// <b>après</b> l'essai — une base indisponible pendant l'essai ne le consomme pas.</summary>
    private DateOnly? _reessaiFaitPour;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (stoppingToken.IsCancellationRequested == false)
        {
            DateTimeOffset? reessaiA = null;
            try
            {
                var maintenant = DateTime.Now;
                var date = DateDEdition(maintenant);
                var edition = await GenererSiNecessaire(date, maintenant, stoppingToken);
                reessaiA = MomentDuReessai(edition, DateTimeOffset.Now);
                if (reessaiA is { } a && a <= DateTimeOffset.Now)
                {
                    reessaiA = null;
                    await Reessayer(date, DateTime.Now, stoppingToken);
                    _reessaiFaitPour = date;
                }
            }
            catch (Exception ex) when (stoppingToken.IsCancellationRequested == false)
            {
                logger.LogError(ex, "Édition : échec de l'éditorialiste.");
            }

            var delai = ProchainReveil(DateTimeOffset.Now, reessaiA) - DateTimeOffset.Now;
            if (delai < TimeSpan.FromMinutes(1))
            {
                delai = TimeSpan.FromMinutes(1);
            }
            await signal.AttendreAsync(delai, stoppingToken);
        }
    }

    /// <summary>La journée que l'édition couvre : le jour civil, toujours.</summary>
    internal static DateOnly DateDEdition(DateTime maintenant) => DateOnly.FromDateTime(maintenant);

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

    /// <summary>Le prochain réveil : le créneau du matin, ou le second essai s'il vient avant.</summary>
    internal DateTimeOffset ProchainReveil(DateTimeOffset maintenant, DateTimeOffset? reessaiA)
    {
        var creneau = ProchainCreneau(maintenant);
        return reessaiA is { } a && a < creneau ? a : creneau;
    }

    /// <summary>
    /// Quand réessayer, si l'édition le mérite : un gabarit écrit alors qu'une clé est
    /// là, c'est un modèle qui n'a pas répondu — une fois par jour, une heure après,
    /// et seulement dans la fenêtre qui suit (deux fois le délai) : le gabarit garde
    /// l'heure de son premier échec, et un redémarrage bien plus tard ne le rappelle
    /// pas. Null quand il n'y a rien à réessayer.
    /// </summary>
    internal DateTimeOffset? MomentDuReessai(Edition edition, DateTimeOffset maintenant)
    {
        if (edition.Source != SourceEdition.Gabarit || redacteur.PeutEcrire == false
            || _reessaiFaitPour == edition.Date
            || maintenant >= edition.GenereLe + DelaiDeReessai + DelaiDeReessai)
        {
            return null;
        }
        return edition.GenereLe + DelaiDeReessai;
    }

    private async Task<Edition> GenererSiNecessaire(DateOnly date, DateTime maintenant, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HouseOsDbContext>();
        var (edition, _) = await GenerationEdition.GenererAsync(
            db, redacteur, meteo.Value, banque, affichage.Value.Lieu, logger, date, maintenant,
            remplacer: false, ct,
            avantLeCreneau: TimeOnly.FromDateTime(maintenant) < humeur.Value.HeureMatin);
        return edition;
    }

    private async Task Reessayer(DateOnly date, DateTime maintenant, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HouseOsDbContext>();
        await GenerationEdition.ReessayerAsync(
            db, redacteur, meteo.Value, banque, affichage.Value.Lieu, logger, date, maintenant, ct);
    }
}
