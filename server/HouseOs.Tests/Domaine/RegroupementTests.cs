using HouseOs.Api.Domaine.Editorial;

namespace HouseOs.Tests.Domaine;

/// <summary>Le sommaire des journées chargées : zone, puis équipement, puis ce que
/// l'éditorialiste a nommé, puis « Le reste » (D-2026-09-20 Regroupement Sans Catégorie
/// De Tâche). Rien d'inventé : on range des titres réels, dans l'ordre d'arrivée.</summary>
public class RegroupementTests
{
    private static readonly IReadOnlyList<TacheAGrouper> Journee =
    [
        new("Ranger le garage", "Le garage", null),
        new("SAAQ — changement d'adresse", null, null),
        new("Changer le filtre", null, "Fournaise"),
        new("Hydro — transfert", null, null),
        new("Huiler la porte", "Le garage", null),
        new("Banques et caisses", null, null),
        new("Pneus d'hiver", null, null),
    ];

    [Fact]
    public void La_zone_range_d_office_puis_l_equipement_et_le_reste_ferme_la_marche()
    {
        var rubriques = Regroupement.Regrouper(Journee, []);

        Assert.Collection(rubriques,
            r =>
            {
                Assert.Equal("Le garage", r.Nom);
                Assert.Equal(["Ranger le garage", "Huiler la porte"], r.Taches);
            },
            r =>
            {
                Assert.Equal("Fournaise", r.Nom);
                Assert.Equal(["Changer le filtre"], r.Taches);
            },
            r =>
            {
                Assert.Equal(Regroupement.LeReste, r.Nom);
                Assert.Equal(["SAAQ — changement d'adresse", "Hydro — transfert", "Banques et caisses", "Pneus d'hiver"], r.Taches);
            });
    }

    [Fact]
    public void Les_rubriques_nommees_prennent_ce_que_le_journal_ne_range_pas_lui_meme()
    {
        var rubriques = Regroupement.Regrouper(Journee,
        [
            new("Gouvernements", ["SAAQ — changement d'adresse", "Hydro — transfert"]),
            new("Argent", ["Banques et caisses"]),
        ]);

        Assert.Equal(["Le garage", "Fournaise", "Gouvernements", "Argent", Regroupement.LeReste],
            rubriques.Select(r => r.Nom));
        Assert.Equal(["Pneus d'hiver"], rubriques[^1].Taches);
    }

    [Fact]
    public void Une_tache_inventee_ou_deja_rangee_n_entre_dans_aucune_rubrique_nommee()
    {
        // « Passeport » n'existe pas ; « Ranger le garage » a sa zone. Une rubrique qui ne
        // place rien de vrai disparaît, et la zone garde sa tâche.
        var rubriques = Regroupement.Regrouper(Journee,
        [
            new("Fantômes", ["Passeport (inventé)"]),
            new("Bricolage", ["Ranger le garage", "Pneus d'hiver"]),
        ]);

        Assert.DoesNotContain(rubriques, r => r.Nom == "Fantômes");
        Assert.Equal(["Pneus d'hiver"], rubriques.Single(r => r.Nom == "Bricolage").Taches);
        Assert.Equal(["Ranger le garage", "Huiler la porte"], rubriques.Single(r => r.Nom == "Le garage").Taches);
    }

    [Fact]
    public void Le_reste_nomme_par_le_modele_est_ignore_et_un_nom_de_zone_se_fond()
    {
        var rubriques = Regroupement.Regrouper(Journee,
        [
            new("le reste", ["Pneus d'hiver"]),
            new("le garage", ["Hydro — transfert"]),
        ]);

        // Le garage a pris Hydro — malgré la casse du modèle ; « le reste » du modèle
        // n'a rien fait, et Pneus tombe dans « Le reste » du journal, comme tout ce qui
        // n'est pas placé.
        Assert.Equal(["Ranger le garage", "Huiler la porte", "Hydro — transfert"],
            rubriques.Single(r => r.Nom == "Le garage").Taches);
        Assert.Single(rubriques, r => string.Equals(r.Nom, Regroupement.LeReste, StringComparison.OrdinalIgnoreCase));
        Assert.Contains("Pneus d'hiver", rubriques[^1].Taches);
    }

    [Fact]
    public void Une_journee_ou_tout_est_range_n_a_pas_de_reste()
    {
        var rubriques = Regroupement.Regrouper(
            [new("A", "Cuisine", null), new("B", null, null)],
            [new("Papiers", ["B"])]);

        Assert.Equal(["Cuisine", "Papiers"], rubriques.Select(r => r.Nom));
    }

    [Fact]
    public void Un_titre_present_deux_fois_suit_sa_rubrique_les_deux_fois()
    {
        // Une tâche cochée puis redue le même jour : la ligne faite suit son titre.
        var rubriques = Regroupement.Regrouper(
            [new("Sortir le bac", null, null), new("Sortir le bac", null, null), new("Autre", null, null)],
            [new("Dehors", ["Sortir le bac"])]);

        Assert.Equal(["Sortir le bac", "Sortir le bac"], rubriques.Single(r => r.Nom == "Dehors").Taches);
        Assert.Equal(["Autre"], rubriques.Single(r => r.Nom == Regroupement.LeReste).Taches);
    }

    [Theory]
    [InlineData(9, false)]
    [InlineData(10, true)]
    [InlineData(14, true)]
    public void La_journee_est_chargee_a_partir_de_dix_taches_dues(int taches, bool chargee)
    {
        Assert.Equal(chargee, RangDuJour.JourneeChargee(taches));
        Assert.Equal(chargee ? RangEdition.Sommaire : RangEdition.Court, RangDuJour.Calculer(taches, false));
    }
}
