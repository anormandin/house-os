using HouseOs.Api.Domaine.Meteo;
using HouseOs.Api.Infrastructure;
using Microsoft.Extensions.Options;

namespace HouseOs.Api.Features.Meteo;

/// <summary>
/// Le tirage des normales climatiques : une dizaine d'années de l'archive Open-Meteo
/// (réanalyse ERA5) pour les coordonnées du `.env`, dont on déduit les normales, qu'on
/// matérialise (vault : D-2026-09-20 Normales Climatiques Depuis L'archive Open-Meteo).
///
/// <para><b>Un tirage par an, mais pas à date fixe.</b> Le service vérifie au démarrage
/// puis toutes les quelques heures, et ne tire que si les normales en base manquent,
/// décrivent un autre lieu, ou ont pris de l'âge. Une date fixe serait ratée chaque
/// année où la machine est éteinte ce jour-là, et surtout elle ferait attendre le
/// déménagement jusqu'au prochain anniversaire : ici, changer
/// `METEO_LATITUDE`/`METEO_LONGITUDE` et redémarrer suffit.</para>
///
/// <para><b>Le journal ne dépend jamais de ce réseau.</b> Une archive injoignable
/// laisse les normales précédentes en place, et une installation neuve sans réseau
/// n'en a tout simplement pas : la famille « le climat » se tait, l'édition sort.</para>
/// </summary>
public class NormalesIngestionService(
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory httpFactory,
    IOptions<MeteoOptions> options,
    ILogger<NormalesIngestionService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (stoppingToken.IsCancellationRequested == false)
        {
            try
            {
                await Verifier(stoppingToken);
            }
            // Filtre sur le jeton : un timeout HTTP est aussi une
            // OperationCanceledException et ne doit pas tuer le service.
            catch (Exception ex) when (stoppingToken.IsCancellationRequested == false)
            {
                logger.LogError(
                    ex, "Normales : échec du tirage de l'archive — les normales connues restent servies.");
            }

            // Plancher d'une heure : une cadence nulle ou négative (mauvaise config)
            // ferait lever Task.Delay et tuerait le service en silence.
            var cadence = TimeSpan.FromHours(Math.Max(1, options.Value.NormalesCadenceHeures));
            await Task.Delay(cadence, stoppingToken);
        }
    }

    private async Task Verifier(CancellationToken ct)
    {
        var reglages = options.Value;
        var maintenant = DateTimeOffset.UtcNow;
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HouseOsDbContext>();

        var connues = await OperationsNormales.LireAsync(db, reglages.CleCoordonnees, ct);
        if (connues is not null
            && (maintenant - connues.CalculeesLe).TotalDays < reglages.NormalesAgeMaxJours)
        {
            return;
        }

        var chrono = System.Diagnostics.Stopwatch.StartNew();
        // La date du foyer, pas celle de la machine : un conteneur en UTC est déjà au
        // lendemain chaque soir après vingt heures locales, et mangerait en silence la
        // marge que NormalesAgeMaxJours suppose.
        var fin = OpenMeteoArchive.FinDeLArchive(
            DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(maintenant, reglages.Fuseau()).DateTime));
        var debut = fin.AddYears(-Math.Max(1, reglages.NormalesAnnees)).AddDays(1);

        var client = httpFactory.CreateClient();
        var json = await client.GetStringAsync(
            OpenMeteoArchive.Url(reglages.Latitude, reglages.Longitude, debut, fin, reglages.FuseauHoraire), ct);
        var jours = OpenMeteoArchive.Normaliser(json, reglages.CleCoordonnees);
        var normales = CalculDesNormales.Calculer(reglages.CleCoordonnees, jours, maintenant);

        // Un tirage maigre remplacerait dix ans d'archive par trois mois, avec un
        // horodatage tout neuf qui interdirait de réessayer avant un an. On garde ce
        // qu'on a et on repasse à la prochaine vérification, dans quelques heures.
        if (OperationsNormales.PourquoiRefuser(
                jours.Count, fin.DayNumber - debut.DayNumber + 1, normales, connues) is { } refus)
        {
            logger.LogWarning(
                "Normales : tirage écarté pour {Coordonnees} — {Refus}. Rien n'est remplacé.",
                reglages.CleCoordonnees, refus);
            return;
        }

        await OperationsNormales.RemplacerAsync(db, jours, normales, ct);

        logger.LogInformation(
            "Normales : {Jours} journées d'archive pour {Coordonnees} ({Debut} → {Fin}), "
            + "{Saisons} saisons complètes, premier gel {Gel}, première neige {Neige}, en {DureeMs} ms.",
            jours.Count, reglages.CleCoordonnees, debut, fin,
            normales.SaisonsCompletes, Dire(normales.PremierGel), Dire(normales.PremiereNeige),
            chrono.ElapsedMilliseconds);
    }

    private static string Dire(Domaine.Meteo.DateNormale? normale) =>
        normale is null ? "—" : $"{normale.Jour:00}/{normale.Mois:00} ± {normale.EcartJours} j";
}
