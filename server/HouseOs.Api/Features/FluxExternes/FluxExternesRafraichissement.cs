using HouseOs.Api.Domaine;
using HouseOs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HouseOs.Api.Features.FluxExternes;

/// <summary>
/// Rafraîchit les flux ICS actifs au démarrage puis toutes les 6 heures
/// (D-2026-08-24 Tables Flux Externes) : téléchargement, normalisation sur
/// hier → +60 jours, remplacement des événements du flux en transaction.
/// Échec → derniers événements conservés, erreur notée sur le flux.
/// </summary>
public class FluxExternesRafraichissement(
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory httpFactory,
    ILogger<FluxExternesRafraichissement> logger)
    : BackgroundService
{
    public const int FenetreJours = 60;
    private static readonly TimeSpan Cadence = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (stoppingToken.IsCancellationRequested == false)
        {
            try
            {
                await RafraichirTous(stoppingToken);
            }
            // Filtre sur le jeton, pas sur le type : un timeout HTTP est aussi une
            // OperationCanceledException et ne doit pas tuer le service.
            catch (Exception ex) when (stoppingToken.IsCancellationRequested == false)
            {
                logger.LogError(ex, "Flux externes : échec du passage de rafraîchissement.");
            }

            await Task.Delay(Cadence, stoppingToken);
        }
    }

    private async Task RafraichirTous(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HouseOsDbContext>();
        var flux = await db.FluxExternes.Where(f => f.Actif).ToListAsync(ct);

        foreach (var abonnement in flux)
        {
            await Rafraichir(db, abonnement, httpFactory.CreateClient(), ct);
        }
    }

    /// <summary>Rafraîchit un flux ; partagé avec la validation à la création.</summary>
    public static async Task Rafraichir(HouseOsDbContext db, FluxExterne flux, HttpClient client, CancellationToken ct)
    {
        var aujourdhui = DateOnly.FromDateTime(DateTime.Now);
        try
        {
            var ics = await client.GetStringAsync(flux.Url, ct);
            var evenements = LectureIcs.Normaliser(ics, aujourdhui.AddDays(-1), aujourdhui.AddDays(FenetreJours));

            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            await db.EvenementsExternes.Where(e => e.FluxExterneId == flux.Id).ExecuteDeleteAsync(ct);
            foreach (var evenement in evenements)
            {
                evenement.FluxExterneId = flux.Id;
            }
            db.EvenementsExternes.AddRange(evenements);
            flux.DernierRafraichissementLe = DateTimeOffset.UtcNow;
            flux.DerniereErreur = null;
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        // Un flux mort, un timeout ou un ICS malformé (Ical.Net lance des types
        // variés) ne doit jamais faire tomber le passage — l'erreur s'affiche
        // dans la gestion. Filtre sur le jeton : un timeout HTTP est aussi une
        // OperationCanceledException.
        catch (Exception ex) when (ct.IsCancellationRequested == false)
        {
            flux.DerniereErreur = ex.Message.Length > 300 ? ex.Message[..300] : ex.Message;
            await db.SaveChangesAsync(ct);
        }
    }
}
