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

        Assert.Equal(ModeRecurrence.Ponctuelle, tache.Mode);
        Assert.Equal(Ariane, tache.AssigneAId);
        Assert.Equal(Alain, tache.CreeParId);
        var occurrence = Assert.Single(tache.Occurrences);
        Assert.Equal(echeance, occurrence.Echeance);
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

        var prochaine = tache.GenererProchaineOccurrence(new DateOnly(2026, 10, 1));

        Assert.Null(prochaine);
    }

    [Fact]
    public void Les_modes_recurrents_sont_reserves_a_la_V1()
    {
        var tache = Tache.CreerPonctuelle(
            "Tondre", null, null, null, Alain, DateTimeOffset.UtcNow);
        tache.Mode = ModeRecurrence.Intervalle;

        Assert.Throws<NotSupportedException>(() =>
            tache.GenererProchaineOccurrence(new DateOnly(2026, 8, 23)));
    }
}
