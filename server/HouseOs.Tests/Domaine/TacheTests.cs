using HouseOs.Api.Domaine;

namespace HouseOs.Tests.Domaine;

public class TacheTests
{
    private static readonly Guid Alain = Guid.NewGuid();
    private static readonly Guid Ariane = Guid.NewGuid();

    [Fact]
    public void CreerPonctuelle_cree_la_tache_avec_une_seule_occurrence_en_attente()
    {
        var echeance = new DateOnly(2026, 10, 6);

        var tache = Tache.CreerPonctuelle(
            "Changement d'adresse", null, echeance, Ariane, Alain, DateTimeOffset.UtcNow);

        Assert.Equal(ModeRecurrence.Ponctuelle, tache.Recurrence.Mode);
        Assert.Equal(Ariane, tache.AssigneAId);
        Assert.Equal(Alain, tache.CreeParId);
        var occurrence = Assert.Single(tache.Occurrences);
        Assert.Equal(echeance, occurrence.Echeance);
        Assert.Equal(Ariane, occurrence.AssigneAId);
        Assert.Equal(StatutOccurrence.EnAttente, occurrence.Statut);
        Assert.Equal(tache.Id, occurrence.TacheId);
    }

    [Fact]
    public void CreerPonctuelle_accepte_une_tache_sans_echeance()
    {
        var tache = Tache.CreerPonctuelle(
            "Acheter des boîtes", null, null, null, Alain, DateTimeOffset.UtcNow);

        var occurrence = Assert.Single(tache.Occurrences);
        Assert.Null(occurrence.Echeance);
    }

    [Fact]
    public void Une_ponctuelle_ne_genere_pas_d_occurrence_suivante()
    {
        var tache = Tache.CreerPonctuelle(
            "Résilier internet", null, new DateOnly(2026, 10, 1), null, Alain, DateTimeOffset.UtcNow);

        var prochaine = tache.GenererProchaineOccurrence(
            new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 1), null);

        Assert.Null(prochaine);
    }

    [Fact]
    public void CreerRecurrente_materialise_la_premiere_occurrence()
    {
        var spec = new SpecRecurrence
        {
            Mode = ModeRecurrence.Fixe,
            FixeType = TypeFixe.JoursSemaine,
            JoursSemaineMasque = SpecRecurrence.MasqueDe(DayOfWeek.Saturday),
        };

        // Créée le dimanche 23 août → premier samedi = le 29
        var tache = Tache.CreerRecurrente(
            "Passer l'aspirateur", null, spec, StrategieAssignation.Alternance, Ariane,
            premiereEcheance: null, Alain, DateTimeOffset.UtcNow, new DateOnly(2026, 8, 23));

        var occurrence = Assert.Single(tache.Occurrences);
        Assert.Equal(new DateOnly(2026, 8, 29), occurrence.Echeance);
        Assert.Equal(Ariane, occurrence.AssigneAId);
    }

    [Fact]
    public void CreerRecurrente_respecte_une_premiere_echeance_fournie()
    {
        var spec = new SpecRecurrence { Mode = ModeRecurrence.Intervalle, IntervalleJours = 30 };

        var tache = Tache.CreerRecurrente(
            "Changer le filtre", null, spec, StrategieAssignation.Fixe, null,
            premiereEcheance: new DateOnly(2026, 11, 1), Alain, DateTimeOffset.UtcNow,
            new DateOnly(2026, 8, 23));

        var occurrence = Assert.Single(tache.Occurrences);
        Assert.Equal(new DateOnly(2026, 11, 1), occurrence.Echeance);
    }

    [Fact]
    public void CreerRecurrente_refuse_le_mode_ponctuel()
    {
        Assert.Throws<InvalidOperationException>(() => Tache.CreerRecurrente(
            "Oups", null, SpecRecurrence.Ponctuelle(), StrategieAssignation.Fixe, null,
            null, Alain, DateTimeOffset.UtcNow, new DateOnly(2026, 8, 23)));
    }

    [Fact]
    public void GenererProchaineOccurrence_recurrente_porte_l_assigne_choisi()
    {
        var spec = new SpecRecurrence { Mode = ModeRecurrence.Intervalle, IntervalleJours = 7 };
        var tache = Tache.CreerRecurrente(
            "Tondre", null, spec, StrategieAssignation.Alternance, null,
            null, Alain, DateTimeOffset.UtcNow, new DateOnly(2026, 8, 23));

        var prochaine = tache.GenererProchaineOccurrence(
            new DateOnly(2026, 8, 30), new DateOnly(2026, 8, 30), Ariane);

        Assert.NotNull(prochaine);
        Assert.Equal(new DateOnly(2026, 9, 6), prochaine.Echeance);
        Assert.Equal(Ariane, prochaine.AssigneAId);
        Assert.Equal(StatutOccurrence.EnAttente, prochaine.Statut);
    }
}
