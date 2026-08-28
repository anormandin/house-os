using HouseOs.Api.Domaine;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;

namespace HouseOs.Api.Features.FluxExternes;

/// <summary>Traduit un ICS externe vers des événements normalisés sur une
/// fenêtre : récurrences RRULE expansées par Ical.Net, événements datés et
/// toute-la-journée. Pur, testé sur fixtures — le seul endroit qui connaît le
/// format iCalendar entrant.</summary>
public static class LectureIcs
{
    // Longueurs du schéma (HouseOsDbContext) : un SUMMARY réel de 500 caractères ne
    // doit pas faire échouer tout le flux en « value too long », en boucle, à vie.
    private const int LongueurMaxTitre = 200;
    private const int LongueurMaxUid = 300;

    public static List<EvenementExterne> Normaliser(string ics, DateOnly debut, DateOnly finExclue)
    {
        var calendrier = Ical.Net.Calendar.Load(ics)
            ?? throw new FormatException("Le contenu reçu n'est pas un calendrier iCalendar.");
        var evenements = new List<EvenementExterne>();

        foreach (var occurrence in calendrier.GetOccurrences(new CalDateTime(debut.ToDateTime(TimeOnly.MinValue))))
        {
            var depart = occurrence.Period.StartTime;
            // Heures UTC (« …Z ») ou TZID étranger — la norme chez Recollect et les
            // calendriers scolaires — converties vers le fuseau de la maison, sinon
            // un événement de 17 h 30 UTC s'afficherait à 17 h 30 locales (et la date
            // du soir basculerait). Les heures flottantes sont déjà « murales ».
            var local = depart.IsFloating ? depart : depart.ToTimeZone(TimeZoneInfo.Local.Id);
            var date = DateOnly.FromDateTime(local.Value);
            if (date >= finExclue)
            {
                break;
            }
            if (occurrence.Source is not CalendarEvent evenement)
            {
                continue;
            }

            var titre = string.IsNullOrWhiteSpace(evenement.Summary) ? "(sans titre)" : evenement.Summary.Trim();
            if (titre.Length > LongueurMaxTitre)
            {
                titre = titre[..LongueurMaxTitre];
            }
            // Le suffixe date garantit l'unicité par occurrence : c'est le préfixe
            // (l'Uid source) qui se fait tronquer, jamais la date.
            var suffixe = $":{date:yyyy-MM-dd}";
            var uidSource = evenement.Uid ?? "";
            if (uidSource.Length + suffixe.Length > LongueurMaxUid)
            {
                uidSource = uidSource[..(LongueurMaxUid - suffixe.Length)];
            }

            evenements.Add(new EvenementExterne
            {
                Uid = uidSource + suffixe,
                Titre = titre,
                Date = date,
                Heure = depart.HasTime ? TimeOnly.FromDateTime(local.Value) : null,
            });
        }

        return evenements
            .OrderBy(e => e.Date)
            .ThenBy(e => e.Heure)
            .ToList();
    }
}
