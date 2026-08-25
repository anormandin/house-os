using HouseOs.Api.Domaine;

namespace HouseOs.Tests.Domaine;

public class OccurrenceTests
{
    private static readonly Guid Alain = Guid.NewGuid();

    private static Occurrence NouvelleOccurrence() => new()
    {
        Id = Guid.NewGuid(),
        TacheId = Guid.NewGuid(),
        Echeance = new DateOnly(2026, 9, 15),
    };

    [Fact]
    public void Completer_marque_le_statut_et_l_auteur()
    {
        var occurrence = NouvelleOccurrence();
        var maintenant = DateTimeOffset.UtcNow;

        occurrence.Completer(Alain, maintenant);

        Assert.Equal(StatutOccurrence.Completee, occurrence.Statut);
        Assert.Equal(Alain, occurrence.CompleteeParId);
        Assert.Equal(maintenant, occurrence.CompleteeLe);
    }

    [Fact]
    public void Completer_retourne_une_entree_de_journal_complete()
    {
        var occurrence = NouvelleOccurrence();
        var maintenant = DateTimeOffset.UtcNow;

        var entree = occurrence.Completer(Alain, maintenant, "filtre changé");

        Assert.Equal(occurrence.TacheId, entree.TacheId);
        Assert.Equal(occurrence.Id, entree.OccurrenceId);
        Assert.Equal(Alain, entree.UtilisateurId);
        Assert.Equal(maintenant, entree.CompleteeLe);
        Assert.Equal("filtre changé", entree.Notes);
    }

    [Fact]
    public void Completer_deux_fois_est_refuse()
    {
        var occurrence = NouvelleOccurrence();
        occurrence.Completer(Alain, DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            occurrence.Completer(Alain, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Completer_une_occurrence_passee_est_refuse()
    {
        var occurrence = NouvelleOccurrence();
        occurrence.Passer(DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            occurrence.Completer(Alain, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Annuler_completion_remet_en_attente_et_efface_l_auteur()
    {
        var occurrence = NouvelleOccurrence();
        occurrence.Completer(Alain, DateTimeOffset.UtcNow);

        occurrence.AnnulerCompletion();

        Assert.Equal(StatutOccurrence.EnAttente, occurrence.Statut);
        Assert.Null(occurrence.CompleteeParId);
        Assert.Null(occurrence.CompleteeLe);
        Assert.Equal(new DateOnly(2026, 9, 15), occurrence.Echeance);
    }

    [Fact]
    public void Annuler_completion_d_une_occurrence_en_attente_est_refuse()
    {
        var occurrence = NouvelleOccurrence();

        Assert.Throws<InvalidOperationException>(() => occurrence.AnnulerCompletion());
    }

    [Fact]
    public void Passer_marque_le_statut_et_la_date()
    {
        var occurrence = NouvelleOccurrence();
        var maintenant = DateTimeOffset.UtcNow;

        occurrence.Passer(maintenant);

        Assert.Equal(StatutOccurrence.Passee, occurrence.Statut);
        Assert.Equal(maintenant, occurrence.PasseeLe);
        Assert.Null(occurrence.CompleteeParId);
    }

    [Fact]
    public void Passer_une_occurrence_completee_est_refuse()
    {
        var occurrence = NouvelleOccurrence();
        occurrence.Completer(Alain, DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            occurrence.Passer(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Reporter_change_l_echeance()
    {
        var occurrence = NouvelleOccurrence();

        occurrence.Reporter(new DateOnly(2026, 9, 22));

        Assert.Equal(new DateOnly(2026, 9, 22), occurrence.Echeance);
        Assert.Equal(StatutOccurrence.EnAttente, occurrence.Statut);
    }

    [Fact]
    public void Reporter_une_occurrence_completee_est_refuse()
    {
        var occurrence = NouvelleOccurrence();
        occurrence.Completer(Alain, DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            occurrence.Reporter(new DateOnly(2026, 9, 22)));
    }

    [Fact]
    public void Annuler_une_occurrence_passee_est_refuse()
    {
        var occurrence = NouvelleOccurrence();
        occurrence.Passer(DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() => occurrence.AnnulerCompletion());
        Assert.Equal(StatutOccurrence.Passee, occurrence.Statut);
    }

    [Fact]
    public void Reporter_une_occurrence_passee_est_refuse()
    {
        var occurrence = NouvelleOccurrence();
        occurrence.Passer(DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            occurrence.Reporter(new DateOnly(2026, 9, 22)));
    }
}
