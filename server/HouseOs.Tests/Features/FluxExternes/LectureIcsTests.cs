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
    public void SummaryTropLong_TronqueALaLongueurDuSchema()
    {
        // Un vrai calendrier peut mettre un paragraphe entier en SUMMARY : sans
        // troncature, « value too long » tuait le flux entier toutes les 6 h.
        var titre = new string('a', 500);
        var ics = $"""
            BEGIN:VCALENDAR
            VERSION:2.0
            PRODID:-//test//FR
            BEGIN:VEVENT
            UID:verbeux@test
            DTSTART;VALUE=DATE:20260910
            SUMMARY:{titre}
            END:VEVENT
            END:VCALENDAR
            """;

        var e = Assert.Single(LectureIcs.Normaliser(ics, Debut, Fin));

        Assert.Equal(200, e.Titre.Length);
        Assert.Equal(titre[..200], e.Titre);
    }

    [Fact]
    public void UidTropLong_TronqueEnGardantLeSuffixeDate()
    {
        var uid = new string('u', 400);
        var ics = $"""
            BEGIN:VCALENDAR
            VERSION:2.0
            PRODID:-//test//FR
            BEGIN:VEVENT
            UID:{uid}
            DTSTART;VALUE=DATE:20260910
            SUMMARY:Uid interminable
            END:VEVENT
            END:VCALENDAR
            """;

        var e = Assert.Single(LectureIcs.Normaliser(ics, Debut, Fin));

        Assert.True(e.Uid.Length <= 300);
        // Le suffixe date reste : c'est lui qui rend l'Uid unique par occurrence.
        Assert.EndsWith(":2026-09-10", e.Uid);
    }

    [Fact]
    public void RecurrenceInfinieSansCountNiUntil_SArreteALaFinDeLaFenetre()
    {
        // La norme chez Recollect : RRULE sans COUNT ni UNTIL. L'expansion doit
        // s'arrêter à la fenêtre — pas boucler, pas déborder.
        const string ics = """
            BEGIN:VCALENDAR
            VERSION:2.0
            PRODID:-//test//FR
            BEGIN:VEVENT
            UID:quotidien@test
            DTSTART;VALUE=DATE:20260901
            RRULE:FREQ=DAILY
            SUMMARY:Chaque jour
            END:VEVENT
            END:VCALENDAR
            """;

        var evenements = LectureIcs.Normaliser(ics, Debut, Fin);

        Assert.Equal(30, evenements.Count); // tout septembre, rien d'octobre
        Assert.Equal(new DateOnly(2026, 9, 30), evenements[^1].Date);
    }

    [Fact]
    public void ContenuSansCalendrier_ErreurClaire()
    {
        var ex = Assert.Throws<FormatException>(
            () => LectureIcs.Normaliser("", Debut, Fin));
        Assert.Contains("iCalendar", ex.Message);
    }

    [Fact]
    public void EvenementEnUtc_ConvertiEnHeureLocale()
    {
        // La norme chez Recollect et les calendriers scolaires : DTSTART en « …Z ».
        const string ics = """
            BEGIN:VCALENDAR
            VERSION:2.0
            PRODID:-//test//FR
            BEGIN:VEVENT
            UID:conseil@ville
            DTSTART:20260908T173000Z
            SUMMARY:Conseil municipal
            END:VEVENT
            END:VCALENDAR
            """;

        var evenements = LectureIcs.Normaliser(ics, Debut, Fin);

        // Attendu calculé via le fuseau de la machine — déterministe partout.
        var attenduLocal = TimeZoneInfo.ConvertTimeFromUtc(
            new DateTime(2026, 9, 8, 17, 30, 0, DateTimeKind.Utc), TimeZoneInfo.Local);
        var e = Assert.Single(evenements);
        Assert.Equal(DateOnly.FromDateTime(attenduLocal), e.Date);
        Assert.Equal(TimeOnly.FromDateTime(attenduLocal), e.Heure);
    }

    [Fact]
    public void EvenementAvecTzidEtranger_ConvertiEnHeureLocale()
    {
        const string ics = """
            BEGIN:VCALENDAR
            VERSION:2.0
            PRODID:-//test//FR
            BEGIN:VEVENT
            UID:webinaire@ouest
            DTSTART;TZID=America/Vancouver:20260908T090000
            SUMMARY:Webinaire
            END:VEVENT
            END:VCALENDAR
            """;

        var evenements = LectureIcs.Normaliser(ics, Debut, Fin);

        var vancouver = TimeZoneInfo.FindSystemTimeZoneById("America/Vancouver");
        var utc = TimeZoneInfo.ConvertTimeToUtc(new DateTime(2026, 9, 8, 9, 0, 0), vancouver);
        var attenduLocal = TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.Local);
        var e = Assert.Single(evenements);
        Assert.Equal(DateOnly.FromDateTime(attenduLocal), e.Date);
        Assert.Equal(TimeOnly.FromDateTime(attenduLocal), e.Heure);
    }

    [Fact]
    public void Recurrence_AvecExdate_OccurrenceAnnuleeAbsente()
    {
        // Collecte décalée un jour férié : la municipalité annule le mercredi via EXDATE.
        const string ics = """
            BEGIN:VCALENDAR
            VERSION:2.0
            PRODID:-//test//FR
            BEGIN:VEVENT
            UID:ordures@recollect
            DTSTART;VALUE=DATE:20260902
            RRULE:FREQ=WEEKLY;BYDAY=WE;COUNT=4
            EXDATE;VALUE=DATE:20260909
            SUMMARY:Ordures
            END:VEVENT
            END:VCALENDAR
            """;

        var evenements = LectureIcs.Normaliser(ics, Debut, Fin);

        Assert.Equal(3, evenements.Count);
        Assert.DoesNotContain(evenements, e => e.Date == new DateOnly(2026, 9, 9));
    }

    [Fact]
    public void Recurrence_DeplaceeParRecurrenceId_SuitLaNouvelleDate()
    {
        // L'occurrence du 9 est déplacée au jeudi 10 (férié) via RECURRENCE-ID.
        const string ics = """
            BEGIN:VCALENDAR
            VERSION:2.0
            PRODID:-//test//FR
            BEGIN:VEVENT
            UID:compost@recollect
            DTSTART;VALUE=DATE:20260902
            RRULE:FREQ=WEEKLY;BYDAY=WE;COUNT=3
            SUMMARY:Compost
            END:VEVENT
            BEGIN:VEVENT
            UID:compost@recollect
            RECURRENCE-ID;VALUE=DATE:20260909
            DTSTART;VALUE=DATE:20260910
            SUMMARY:Compost (reporté)
            END:VEVENT
            END:VCALENDAR
            """;

        var evenements = LectureIcs.Normaliser(ics, Debut, Fin);

        Assert.Equal(3, evenements.Count);
        Assert.DoesNotContain(evenements, e => e.Date == new DateOnly(2026, 9, 9));
        Assert.Contains(evenements, e => e.Date == new DateOnly(2026, 9, 10));
    }

    [Fact]
    public void ComposantsNonEvenements_Ignores()
    {
        const string ics = """
            BEGIN:VCALENDAR
            VERSION:2.0
            PRODID:-//test//FR
            BEGIN:VTODO
            UID:todo@test
            DTSTART;VALUE=DATE:20260910
            SUMMARY:Une tâche VTODO
            END:VTODO
            BEGIN:VEVENT
            UID:vrai@test
            DTSTART;VALUE=DATE:20260911
            SUMMARY:Un vrai événement
            END:VEVENT
            END:VCALENDAR
            """;

        var evenements = LectureIcs.Normaliser(ics, Debut, Fin);

        var e = Assert.Single(evenements);
        Assert.Equal("Un vrai événement", e.Titre);
    }
}
