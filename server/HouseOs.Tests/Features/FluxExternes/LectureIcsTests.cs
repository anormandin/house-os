using HouseOs.Api.Features.FluxExternes;

namespace HouseOs.Tests.Features.FluxExternes;

public class LectureIcsTests
{
    private static readonly DateOnly Debut = new(2026, 9, 1);
    private static readonly DateOnly Fin = new(2026, 10, 1);

    [Fact]
    public void EvenementPonctuelDate_AvecHeure()
    {
        const string ics = """
            BEGIN:VCALENDAR
            VERSION:2.0
            PRODID:-//test//FR
            BEGIN:VEVENT
            UID:rentree@css
            DTSTART:20260908T133000
            DTEND:20260908T150000
            SUMMARY:Rencontre de parents
            END:VEVENT
            END:VCALENDAR
            """;

        var evenements = LectureIcs.Normaliser(ics, Debut, Fin);

        var e = Assert.Single(evenements);
        Assert.Equal("Rencontre de parents", e.Titre);
        Assert.Equal(new DateOnly(2026, 9, 8), e.Date);
        Assert.Equal(new TimeOnly(13, 30), e.Heure);
    }

    [Fact]
    public void EvenementTouteLaJournee_SansHeure()
    {
        const string ics = """
            BEGIN:VCALENDAR
            VERSION:2.0
            PRODID:-//test//FR
            BEGIN:VEVENT
            UID:pedago@css
            DTSTART;VALUE=DATE:20260918
            DTEND;VALUE=DATE:20260919
            SUMMARY:Journée pédagogique
            END:VEVENT
            END:VCALENDAR
            """;

        var evenements = LectureIcs.Normaliser(ics, Debut, Fin);

        var e = Assert.Single(evenements);
        Assert.Equal(new DateOnly(2026, 9, 18), e.Date);
        Assert.Null(e.Heure);
    }

    [Fact]
    public void RecurrenceHebdomadaire_ExpanseeDansLaFenetre()
    {
        // Collecte type Recollect : tous les mercredis.
        const string ics = """
            BEGIN:VCALENDAR
            VERSION:2.0
            PRODID:-//test//FR
            BEGIN:VEVENT
            UID:recyclage@recollect
            DTSTART;VALUE=DATE:20260902
            DTEND;VALUE=DATE:20260903
            RRULE:FREQ=WEEKLY;BYDAY=WE
            SUMMARY:Recyclage
            END:VEVENT
            END:VCALENDAR
            """;

        var evenements = LectureIcs.Normaliser(ics, Debut, Fin);

        Assert.Equal(5, evenements.Count); // les mercredis 2, 9, 16, 23, 30 septembre
        Assert.All(evenements, e => Assert.Equal("Recyclage", e.Titre));
        Assert.All(evenements, e => Assert.Equal(DayOfWeek.Wednesday, e.Date.DayOfWeek));
        // Uid distinct par occurrence (contrainte de remplacement propre).
        Assert.Equal(evenements.Count, evenements.Select(e => e.Uid).Distinct().Count());
    }

    [Fact]
    public void HorsFenetre_Exclus()
    {
        const string ics = """
            BEGIN:VCALENDAR
            VERSION:2.0
            PRODID:-//test//FR
            BEGIN:VEVENT
            UID:avant@test
            DTSTART;VALUE=DATE:20260820
            SUMMARY:Trop tôt
            END:VEVENT
            BEGIN:VEVENT
            UID:apres@test
            DTSTART;VALUE=DATE:20261015
            SUMMARY:Trop tard
            END:VEVENT
            END:VCALENDAR
            """;

        Assert.Empty(LectureIcs.Normaliser(ics, Debut, Fin));
    }

    [Fact]
    public void SansTitre_PlaceholderFrancais()
    {
        const string ics = """
            BEGIN:VCALENDAR
            VERSION:2.0
            PRODID:-//test//FR
            BEGIN:VEVENT
            UID:vide@test
            DTSTART;VALUE=DATE:20260910
            END:VEVENT
            END:VCALENDAR
            """;

        var e = Assert.Single(LectureIcs.Normaliser(ics, Debut, Fin));
        Assert.Equal("(sans titre)", e.Titre);
    }

    [Fact]
    public void ContenuSansCalendrier_ErreurClaire()
    {
        var ex = Assert.Throws<FormatException>(
            () => LectureIcs.Normaliser("", Debut, Fin));
        Assert.Contains("iCalendar", ex.Message);
    }
}
