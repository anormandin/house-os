using HouseOs.Api.Domaine.Lettre;
using HouseOs.Api.Features.Affichage;
using HouseOs.Api.Features.FondsDeTiroir;
using HouseOs.Api.Features.Meteo;
using HouseOs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HouseOs.Api.Features.Lettre;

/// <summary>
/// La lettre du matin, à l'heure de la maison, sur le patron de
/// <see cref="Editorial.EditorialisteService"/> : rattrapage au démarrage jusqu'à la
/// limite, jamais deux fois, aucun appel dans le chemin de requête.
///
/// <para>Quand le modèle se tait à l'heure d'envoi, rien ne part : un second essai a
/// lieu <see cref="DelaiDeReessai"/> plus tard, et s'il échoue aussi — ou s'il tomberait
/// après la limite — la note de quatre lignes part à sa place. Sans clé, la note est
/// le fonctionnement normal et part tout de suite
/// (D-2026-09-21 Lettre Écrite À Part Sur La Même Matière,
/// D-2026-09-21 Lettre Matérialisée Et Rattrapée Le Jour Même).</para>
/// </summary>
public class LettreService(
    IServiceScopeFactory scopeFactory,
    IRedacteurLettre redacteur,
    IEnvoyeurDeCourriel envoyeur,
    IOptions<LettreOptions> options,
    IOptions<MeteoOptions> meteo,
    IOptions<AffichageOptions> affichage,
    BanqueDuHasard? banque,
    ILogger<LettreService> logger)
    : BackgroundService
{
    public static readonly TimeSpan DelaiDeReessai = TimeSpan.FromHours(1);

    /// <summary>Un serveur SMTP qui refuse ne se rappelle pas à la minute.</summary>
    public static readonly TimeSpan DelaiApresEchecDEnvoi = TimeSpan.FromMinutes(15);

    private DateOnly? _reessaiFaitPour;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (stoppingToken.IsCancellationRequested == false)
        {
            DateTimeOffset prochain;
            try
            {
                prochain = await PasserAsync(DateTime.Now, stoppingToken);
            }
            catch (Exception ex) when (stoppingToken.IsCancellationRequested == false)
            {
                logger.LogError(ex, "Lettre : échec du passage.");
                prochain = DateTimeOffset.Now + DelaiApresEchecDEnvoi;
            }
            var delai = prochain - DateTimeOffset.Now;
            if (delai < TimeSpan.FromMinutes(1))
            {
                delai = TimeSpan.FromMinutes(1);
            }
            try
            {
                await Task.Delay(delai, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Arrêt de l'hôte.
            }
        }
    }

    internal DateTimeOffset Creneau(DateOnly date) => Local(date, options.Value.HeureEnvoi).AddMinutes(1);

    internal DateTimeOffset Limite(DateOnly date) => Local(date, options.Value.LimiteRattrapage);

    private static DateTimeOffset Local(DateOnly date, TimeOnly heure)
    {
        var d = date.ToDateTime(heure);
        return new DateTimeOffset(d, TimeZoneInfo.Local.GetUtcOffset(d));
    }

    /// <summary>Un passage : ce qu'il y a à faire maintenant, et quand repasser.</summary>
    internal async Task<DateTimeOffset> PasserAsync(DateTime maintenant, CancellationToken ct)
    {
        var date = DateOnly.FromDateTime(maintenant);
        var heure = TimeOnly.FromDateTime(maintenant);
        var demain = Creneau(date.AddDays(1));
        if (heure < options.Value.HeureEnvoi)
        {
            return Creneau(date);
        }
        if (heure >= options.Value.LimiteRattrapage)
        {
            return demain;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HouseOsDbContext>();
        var lettre = await db.Lettres.SingleOrDefaultAsync(l => l.Date == date, ct);
        if (lettre?.Envoyee == true)
        {
            return demain;
        }

        if (lettre is null)
        {
            (lettre, _) = await GenerationLettre.ComposerAsync(
                db, redacteur, meteo.Value, banque, affichage.Value.Lieu, logger, date, maintenant,
                remplacer: false, ct);
            if (lettre.Envoyee)
            {
                return demain;
            }
            if (lettre.Source == SourceLettre.Gabarit && redacteur.PeutEcrire)
            {
                // Le modèle s'est tu : rien ne part, second essai plus tard — sauf s'il
                // tomberait après la limite, où la note part tout de suite.
                var essai = MomentDuReessai(lettre, date);
                if (essai is { } e)
                {
                    logger.LogWarning("Lettre du {Date} : le modèle s'est tu — second essai à {Essai:t}.", date, e.LocalDateTime);
                    return e;
                }
                logger.LogWarning("Lettre du {Date} : le modèle s'est tu et la limite est proche — la note part.", date);
            }
            return await Envoyer(db, lettre, maintenant, demain, ct);
        }

        // Composée, pas partie : un envoi raté, ou un modèle muet à réessayer.
        if (lettre.Source == SourceLettre.Llm || redacteur.PeutEcrire == false)
        {
            return await Envoyer(db, lettre, maintenant, demain, ct);
        }
        var moment = MomentDuReessai(lettre, date);
        if (moment is { } m && new DateTimeOffset(maintenant) < m)
        {
            return m;
        }
        if (moment is not null && _reessaiFaitPour != date)
        {
            logger.LogInformation("Lettre du {Date} restée en gabarit — second essai.", date);
            (lettre, _) = await GenerationLettre.ComposerAsync(
                db, redacteur, meteo.Value, banque, affichage.Value.Lieu, logger, date, maintenant,
                remplacer: true, ct);
            _reessaiFaitPour = date;
            if (lettre.Source == SourceLettre.Gabarit)
            {
                logger.LogWarning("Lettre du {Date} : le second essai n'a rien donné — la note part.", date);
            }
        }
        return await Envoyer(db, lettre, maintenant, demain, ct);
    }

    /// <summary>Quand réessayer une lettre restée en gabarit : une heure après sa
    /// composition, si ce moment tombe avant la limite. Null sinon : la note part.</summary>
    internal DateTimeOffset? MomentDuReessai(LettreDuMatin lettre, DateOnly date)
    {
        var essai = lettre.ComposeeLe + DelaiDeReessai;
        return essai < Limite(date) ? essai : null;
    }

    private async Task<DateTimeOffset> Envoyer(
        HouseOsDbContext db, LettreDuMatin lettre, DateTime maintenant, DateTimeOffset demain, CancellationToken ct)
    {
        try
        {
            await GenerationLettre.EnvoyerAsync(db, envoyeur, options.Value, lettre, logger, maintenant, ct);
            return demain;
        }
        catch (Exception ex) when (ct.IsCancellationRequested == false)
        {
            logger.LogError(ex, "Lettre du {Date} : l'envoi a échoué — nouvel essai dans {Delai} min.",
                lettre.Date, DelaiApresEchecDEnvoi.TotalMinutes);
            return new DateTimeOffset(maintenant) + DelaiApresEchecDEnvoi;
        }
    }
}
