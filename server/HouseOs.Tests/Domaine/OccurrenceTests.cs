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
}
