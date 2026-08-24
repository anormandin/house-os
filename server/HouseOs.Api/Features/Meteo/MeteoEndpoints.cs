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

public record MeteoDto(DateTimeOffset? MisAJourLe, List<JourMeteoDto> Jours, List<VerdictDto> Verdicts);

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

            var aujourdhui = DateOnly.FromDateTime(DateTime.Now);
            var jours = await db.PrevisionsQuotidiennes
                .Where(j => j.Date >= aujourdhui)
                .OrderBy(j => j.Date)
                .ToListAsync();
            var heures = await db.PrevisionsHoraires.OrderBy(h => h.Heure).ToListAsync();

            var verdicts = new List<VerdictDto>();
            if (heures.Count > 0)
            {
                var apercu = new ApercuMeteo(DateTime.Now, heures, jours);
                verdicts = RegleJournee.Toutes
                    .Select(r => r.Evaluer(apercu))
                    .Select(v => new VerdictDto(v.Regle, v.Etat.ToString(), v.Raison))
                    .ToList();
            }

            return new MeteoDto(
                misAJourLe,
                jours.Select(j => new JourMeteoDto(
                    j.Date, j.TemperatureMinC, j.TemperatureMaxC,
                    j.PrecipitationMm, j.ProbabilitePrecipitationMaxPct, j.CodeMeteo)).ToList(),
                verdicts);
        });

        return app;
    }
}
