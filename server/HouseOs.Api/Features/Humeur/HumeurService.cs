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

            await Task.Delay(ProchainCreneau(DateTimeOffset.Now) - DateTimeOffset.Now, stoppingToken);
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
        // Une minute de marge pour être sûr d'atterrir après le créneau.
        return new DateTimeOffset(prochaine, maintenant.Offset).AddMinutes(1);
    }

    private async Task GenererSiManquante(DateOnly date, MomentJournee moment, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HouseOsDbContext>();

        var existe = await db.PhrasesDuJour.AnyAsync(p => p.Date == date && p.Moment == moment, ct);
        if (existe)
        {
            return;
        }

        var etat = await ConstruireEtat.Construire(db, date, moment, ct);
        var (titre, sousTitre, source) = await Generer(etat, ct);

        db.PhrasesDuJour.Add(new PhraseDuJour
        {
            Date = date,
            Moment = moment,
            Titre = titre,
            SousTitre = sousTitre,
            Source = source,
            GenereLe = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Humeur : phrase du {Date} ({Moment}) générée via {Source}.",
            date, moment, source);
    }

    private async Task<(string Titre, string SousTitre, SourcePhrase Source)> Generer(
        EtatMaison etat, CancellationToken ct)
    {
        var cle = options.Value.CleEffective();
        if (cle is not null)
        {
            try
            {
                var polie = await PolissageLlm.Polir(etat, cle, options.Value.Modele, ct);
                if (polie is not null)
                {
                    return (polie.Value.Titre, polie.Value.SousTitre, SourcePhrase.Llm);
                }
                logger.LogWarning("Humeur : réponse LLM inutilisable — repli sur la banque.");
            }
            catch (Exception ex) when (ct.IsCancellationRequested == false)
            {
                logger.LogWarning(ex, "Humeur : appel LLM raté — repli sur la banque.");
            }
        }

        var (titre, sousTitre) = BanquePhrases.Generer(etat);
        return (titre, sousTitre, SourcePhrase.Gabarit);
    }
}
