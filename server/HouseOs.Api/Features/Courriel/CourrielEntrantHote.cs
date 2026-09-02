using Microsoft.Extensions.Options;

namespace HouseOs.Api.Features.Courriel;

/// <summary>
/// La boucle : un passage au démarrage, puis toutes les CadenceMinutes (plancher une
/// minute). Séparée du service pour que celui-ci reste injectable dans le endpoint et
/// l'outil MCP — un BackgroundService enregistré par AddHostedService ne l'est pas.
/// </summary>
public sealed class CourrielEntrantHote(
    CourrielEntrantService service,
    IDepotCourriels depot,
    IOptions<CourrielOptions> options,
    ILogger<CourrielEntrantHote> journal)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (depot.Actif == false)
        {
            journal.LogInformation("Courriel : dépôt R2 non configuré — relevé désactivé.");
            return;
        }
        journal.LogInformation(
            "Courriel : relevé actif toutes les {Cadence} min sur {Bucket}/{Prefixe}.",
            Math.Max(1, options.Value.CadenceMinutes), options.Value.R2.Bucket, options.Value.R2.Prefixe);

        while (stoppingToken.IsCancellationRequested == false)
        {
            try
            {
                await service.ReleverAsync(stoppingToken);
            }
            // Filtre sur le jeton : un timeout réseau est aussi une
            // OperationCanceledException et ne doit pas tuer le service.
            catch (Exception ex) when (stoppingToken.IsCancellationRequested == false)
            {
                journal.LogError(ex, "Courriel : échec du passage de relevé — les objets restent dans le dépôt.");
            }

            var cadence = TimeSpan.FromMinutes(Math.Max(1, options.Value.CadenceMinutes));
            await Task.Delay(cadence, stoppingToken);
        }
    }
}
