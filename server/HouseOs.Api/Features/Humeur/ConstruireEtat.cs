using HouseOs.Api.Domaine;
using HouseOs.Api.Domaine.Humeur;
using HouseOs.Api.Domaine.Meteo;
using HouseOs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HouseOs.Api.Features.Humeur;

/// <summary>Couche 1 : calcule l'état structuré de la maison depuis les tables.
/// Mêmes règles de filtre que la liste d'occurrences (OperationsTaches).</summary>
public static class ConstruireEtat
{
    private const int MaxComptesProches = 3;

    public static async Task<EtatMaison> Construire(
        HouseOsDbContext db,
        DateOnly date,
        MomentJournee moment,
        CancellationToken ct)
    {
        var ouvertes = await db.Occurrences.CountAsync(o =>
            o.Statut == StatutOccurrence.EnAttente && o.Echeance != null && o.Echeance <= date, ct);
        var enRetard = await db.Occurrences.CountAsync(o =>
            o.Statut == StatutOccurrence.EnAttente && o.Echeance != null && o.Echeance < date, ct);

        var debutJour = AvecOffsetLocal(date);
        var finJour = AvecOffsetLocal(date.AddDays(1));
        var faites = await db.Occurrences.CountAsync(o =>
            o.Statut == StatutOccurrence.Completee && o.CompleteeLe >= debutJour && o.CompleteeLe < finJour, ct);

        var comptesProches = (await db.ComptesARebours
                .Where(c => c.DateCible >= date)
                .OrderBy(c => c.DateCible)
                .Take(MaxComptesProches)
                .ToListAsync(ct))
            .Select(c => new CompteProche(c.Titre, c.DateCible.DayNumber - date.DayNumber))
            .ToList();

        return new EtatMaison(date, moment, ouvertes, enRetard, faites, comptesProches,
            await MeteoDuJour(db, date, ct));
    }

    private static async Task<MeteoDuJour?> MeteoDuJour(HouseOsDbContext db, DateOnly date, CancellationToken ct)
    {
        var jour = await db.PrevisionsQuotidiennes.FirstOrDefaultAsync(j => j.Date == date, ct);
        if (jour is null)
        {
            return null;
        }

        var heures = await db.PrevisionsHoraires.OrderBy(h => h.Heure).ToListAsync(ct);
        var jours = await db.PrevisionsQuotidiennes.OrderBy(j => j.Date).ToListAsync(ct);
        var apercu = new ApercuMeteo(DateTime.Now, heures, jours);
        var favorables = RegleJournee.Toutes
            .Select(r => r.Evaluer(apercu))
            .Where(v => v.Etat == EtatVerdict.Bon)
            .Select(v => v.Regle)
            .ToList();

        return new MeteoDuJour(jour.TemperatureMinC, jour.TemperatureMaxC,
            jour.ProbabilitePrecipitationMaxPct, favorables);
    }

    // Minuit local exprimé en UTC : Npgsql n'accepte que l'offset 0 en paramètre timestamptz.
    private static DateTimeOffset AvecOffsetLocal(DateOnly date)
    {
        var minuit = date.ToDateTime(TimeOnly.MinValue);
        return new DateTimeOffset(minuit, TimeZoneInfo.Local.GetUtcOffset(minuit)).ToUniversalTime();
    }
}
