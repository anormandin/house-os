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
    private const int MaxTachesDuJour = 5;
    private const int MaxProchainesTaches = 4;
    private const int HorizonProchainesJours = 7;

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

        var tachesDuJour = await db.Occurrences
            .Where(o => o.Statut == StatutOccurrence.EnAttente && o.Echeance != null && o.Echeance <= date)
            .OrderBy(o => o.Echeance)
            .Take(MaxTachesDuJour)
            .Select(o => o.Tache!.Titre)
            .ToListAsync(ct);

        var horizon = date.AddDays(HorizonProchainesJours);
        var prochaines = (await db.Occurrences
                .Where(o => o.Statut == StatutOccurrence.EnAttente
                    && o.Echeance != null && o.Echeance > date && o.Echeance <= horizon)
                .OrderBy(o => o.Echeance)
                .Take(MaxProchainesTaches)
                .Select(o => new { o.Tache!.Titre, Echeance = o.Echeance!.Value })
                .ToListAsync(ct))
            .Select(o => new TacheAVenir(o.Titre, o.Echeance.DayNumber - date.DayNumber))
            .ToList();

        return new EtatMaison(date, moment, ouvertes, enRetard, faites, comptesProches,
            tachesDuJour, prochaines, await SignalMeteo(db, date, ct));
    }

    private static async Task<SignalMeteoRemarquable?> SignalMeteo(
        HouseOsDbContext db, DateOnly date, CancellationToken ct)
    {
        var heures = await db.PrevisionsHoraires.OrderBy(h => h.Heure).ToListAsync(ct);
        if (heures.Count == 0)
        {
            return null;
        }
        var jours = await db.PrevisionsQuotidiennes.OrderBy(j => j.Date).ToListAsync(ct);
        // La météo évaluée est celle de la DATE de la phrase : au rattrapage de 3 h du
        // matin, la phrase du soir d'hier ne doit pas annoncer les orages d'aujourd'hui.
        var instant = date.ToDateTime(TimeOnly.FromDateTime(DateTime.Now));
        return MeteoRemarquable.Evaluer(new ApercuMeteo(instant, heures, jours));
    }

    // Minuit local exprimé en UTC : Npgsql n'accepte que l'offset 0 en paramètre timestamptz.
    private static DateTimeOffset AvecOffsetLocal(DateOnly date)
    {
        var minuit = date.ToDateTime(TimeOnly.MinValue);
        return new DateTimeOffset(minuit, TimeZoneInfo.Local.GetUtcOffset(minuit)).ToUniversalTime();
    }
}
