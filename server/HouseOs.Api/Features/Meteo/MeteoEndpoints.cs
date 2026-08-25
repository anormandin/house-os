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

            var aujourdhui = DateOnly.FromDateTime(DateTime.Now);
            var jours = await db.PrevisionsQuotidiennes
                .Where(j => j.Date >= aujourdhui)
                .OrderBy(j => j.Date)
                .ToListAsync();
            var heures = await db.PrevisionsHoraires.OrderBy(h => h.Heure).ToListAsync();

            var verdicts = new List<VerdictDto>();
            MaintenantDto? maintenant = null;
            if (heures.Count > 0)
            {
                var apercu = new ApercuMeteo(DateTime.Now, heures, jours);
                verdicts = RegleJournee.Toutes
                    .Select(r => r.Evaluer(apercu))
                    .Select(v => new VerdictDto(v.Regle, v.Etat.ToString(), v.Raison))
                    .ToList();

                var heureCourante = new DateTime(DateTime.Now.Year, DateTime.Now.Month,
                    DateTime.Now.Day, DateTime.Now.Hour, 0, 0);
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
                    j.PrecipitationMm, j.ProbabilitePrecipitationMaxPct, j.CodeMeteo)).ToList(),
                verdicts);
        });

        return app;
    }
}
