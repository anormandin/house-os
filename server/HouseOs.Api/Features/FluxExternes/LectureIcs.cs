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
    public static List<EvenementExterne> Normaliser(string ics, DateOnly debut, DateOnly finExclue)
    {
        var calendrier = Ical.Net.Calendar.Load(ics)
            ?? throw new FormatException("Le contenu reçu n'est pas un calendrier iCalendar.");
        var evenements = new List<EvenementExterne>();

        foreach (var occurrence in calendrier.GetOccurrences(new CalDateTime(debut.ToDateTime(TimeOnly.MinValue))))
        {
            var depart = occurrence.Period.StartTime;
            var date = DateOnly.FromDateTime(depart.Value);
            if (date >= finExclue)
            {
                break;
            }
            if (occurrence.Source is not CalendarEvent evenement)
            {
                continue;
            }

            evenements.Add(new EvenementExterne
            {
                Uid = $"{evenement.Uid}:{date:yyyy-MM-dd}",
                Titre = string.IsNullOrWhiteSpace(evenement.Summary) ? "(sans titre)" : evenement.Summary.Trim(),
                Date = date,
                Heure = depart.HasTime ? TimeOnly.FromDateTime(depart.Value) : null,
            });
        }

        return evenements
            .OrderBy(e => e.Date)
            .ThenBy(e => e.Heure)
            .ToList();
    }
}
