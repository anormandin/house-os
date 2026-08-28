using HouseOs.Api.Domaine.Meteo;
using HouseOs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HouseOs.Api.Features.Meteo;

public record JourMeteoDto(
    DateOnly Date,
    double TempMin,
    double TempMax,
    double PrecipitationMm,
    int ProbabilitePrecipitation,
    int CodeMeteo);

public record VerdictDto(string Regle, string Etat, string Raison);

/// <summary>Les conditions de l'heure courante — le « dehors, là, maintenant ».</summary>
public record MaintenantDto(double TemperatureC, int CodeMeteo);

public record MeteoDto(
    DateTimeOffset? MisAJourLe,
    MaintenantDto? Maintenant,
    List<JourMeteoDto> Jours,
    List<VerdictDto> Verdicts);

public static class MeteoEndpoints
{
    public static IEndpointRouteBuilder MapMeteo(this IEndpointRouteBuilder app)
    {
        // Lecture des tables locales seulement — jamais d'appel externe ici.
        app.MapGet("/api/meteo", async (HouseOsDbContext db) =>
        {
            var misAJourLe = await db.RelevesMeteo
                .OrderByDescending(r => r.RecupereLe)
                .Select(r => (DateTimeOffset?)r.RecupereLe)
                .FirstOrDefaultAsync();

            // Une seule lecture d'horloge : à la bascule de minuit, quatre lectures
            // séparées pouvaient mélanger la date d'hier et l'heure d'aujourd'hui.
            var instant = DateTime.Now;
            var aujourdhui = DateOnly.FromDateTime(instant);
            var jours = await db.PrevisionsQuotidiennes
                .Where(j => j.Date >= aujourdhui)
                .OrderBy(j => j.Date)
                .ToListAsync();
            var heures = await db.PrevisionsHoraires.OrderBy(h => h.Heure).ToListAsync();

            var verdicts = new List<VerdictDto>();
            MaintenantDto? maintenant = null;
            int? probabiliteRestante = null;
            if (heures.Count > 0)
            {
                var apercu = new ApercuMeteo(instant, heures, jours);
                verdicts = RegleJournee.Toutes
                    .Select(r => r.Evaluer(apercu))
                    .Select(v => new VerdictDto(v.Regle, v.Etat.ToString(), v.Raison))
                    .ToList();
                probabiliteRestante = PluieDuJour.ProbabiliteRestantePct(apercu);

                var heureCourante = new DateTime(instant.Year, instant.Month,
                    instant.Day, instant.Hour, 0, 0);
                var courante = heures.FirstOrDefault(h => h.Heure == heureCourante);
                maintenant = courante is null
                    ? null
                    : new MaintenantDto(courante.TemperatureC, courante.CodeMeteo);
            }

            return new MeteoDto(
                misAJourLe,
                maintenant,
                jours.Select(j => new JourMeteoDto(
                    j.Date, j.TemperatureMinC, j.TemperatureMaxC,
                    j.PrecipitationMm,
                    // Aujourd'hui : la pluie des heures restantes — le max du jour
                    // civil peut venir de la nuit passée et contredire les
                    // pastilles « bonne journée pour… » (issue #58).
                    j.Date == aujourdhui && probabiliteRestante.HasValue
                        ? probabiliteRestante.Value
                        : j.ProbabilitePrecipitationMaxPct,
                    j.CodeMeteo)).ToList(),
                verdicts);
        });

        return app;
    }
}
