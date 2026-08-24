using System.Globalization;
using HouseOs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HouseOs.Api.Features.Meteo;

/// <summary>
/// Ingestion Open-Meteo : toutes les heures, récupère les prévisions de la maison
/// (horaire + quotidien, hier inclus pour les règles « pas de pluie depuis 12 h »),
/// remplace les tables normalisées et archive le payload brut. L'UI ne dépend
/// jamais de ce réseau : elle lit les tables (D-2026-08-24 Tables Météo Normalisées).
/// </summary>
public class MeteoIngestionService(
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory httpFactory,
    IOptions<MeteoOptions> options,
    ILogger<MeteoIngestionService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (stoppingToken.IsCancellationRequested == false)
        {
            try
            {
                await Ingerer(stoppingToken);
            }
            // Filtre sur le jeton : un timeout HTTP est aussi une
            // OperationCanceledException et ne doit pas tuer le service.
            catch (Exception ex) when (stoppingToken.IsCancellationRequested == false)
            {
                logger.LogError(ex, "Météo : échec de l'ingestion — les dernières prévisions connues restent servies.");
            }

            await Task.Delay(TimeSpan.FromMinutes(options.Value.CadenceMinutes), stoppingToken);
        }
    }

    private async Task Ingerer(CancellationToken ct)
    {
        var client = httpFactory.CreateClient();
        var json = await client.GetStringAsync(UrlPrevisions(options.Value), ct);
        var previsions = OpenMeteoNormalisation.Normaliser(json);

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HouseOsDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        await db.PrevisionsHoraires.ExecuteDeleteAsync(ct);
        await db.PrevisionsQuotidiennes.ExecuteDeleteAsync(ct);
        db.PrevisionsHoraires.AddRange(previsions.Heures);
        db.PrevisionsQuotidiennes.AddRange(previsions.Jours);

        db.RelevesMeteo.Add(new Domaine.Meteo.ReleveMeteo
        {
            RecupereLe = DateTimeOffset.UtcNow,
            Payload = json,
        });
        var seuilPurge = DateTimeOffset.UtcNow.AddDays(-options.Value.RetentionRelevesJours);
        await db.RelevesMeteo.Where(r => r.RecupereLe < seuilPurge).ExecuteDeleteAsync(ct);

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        logger.LogInformation("Météo : {Heures} heures et {Jours} jours de prévisions ingérés.",
            previsions.Heures.Count, previsions.Jours.Count);
    }

    private static string UrlPrevisions(MeteoOptions options)
    {
        var latitude = options.Latitude.ToString(CultureInfo.InvariantCulture);
        var longitude = options.Longitude.ToString(CultureInfo.InvariantCulture);
        return "https://api.open-meteo.com/v1/forecast"
            + $"?latitude={latitude}&longitude={longitude}"
            + "&hourly=temperature_2m,precipitation,precipitation_probability,wind_speed_10m,wind_gusts_10m,"
            + "relative_humidity_2m,soil_moisture_0_to_1cm,cloud_cover,uv_index"
            + "&daily=temperature_2m_min,temperature_2m_max,precipitation_sum,precipitation_probability_max,"
            + "wind_speed_10m_max,wind_gusts_10m_max,uv_index_max,weather_code,sunrise,sunset"
            + $"&timezone={Uri.EscapeDataString(options.FuseauHoraire)}"
            + "&past_days=1&forecast_days=7&wind_speed_unit=kmh";
    }
}
