using HouseOs.Api.Domaine.Editorial;

namespace HouseOs.Tests.Domaine;

/// <summary>
/// Le domaine de l'édition, sans base : le plancher, le rang, le gabarit. Les mêmes
/// règles que `web/src/lib/ecran-vues.ts` — c'est ce que le serveur fige dans
/// l'édition, et ce qui redéclenche une édition quand ça change.
/// </summary>
public class EditorialTests
{
    private static readonly DateOnly Aujourdhui = new(2026, 10, 2);

    private static OccurrenceDue Due(string titre, int joursDeRetard = 0, bool ferme = false) =>
        new(titre, Aujourdhui.AddDays(-joursDeRetard), ferme);

    [Fact]
    public void Le_plancher_compte_puis_ferme_puis_retard()
    {
        var notaire = Due("Signer chez le notaire", ferme: true);
        var boites = Due("Boîtes", joursDeRetard: 9);

        Assert.Null(Plancher.Evaluer([Due("Boîtes")], null, Aujourdhui));
        Assert.Null(Plancher.Evaluer([Due("Boîtes", Plancher.JoursDeRetard)], null, Aujourdhui));
        Assert.Equal(new PlancherDuJour(RaisonDePlancher.Retard, "Boîtes"),
            Plancher.Evaluer([boites], null, Aujourdhui));
        Assert.Equal(new PlancherDuJour(RaisonDePlancher.Ferme, "Signer chez le notaire"),
            Plancher.Evaluer([boites, notaire], null, Aujourdhui));
        Assert.Equal(new PlancherDuJour(RaisonDePlancher.Compte, "Le camion"),
            Plancher.Evaluer([boites, notaire], ("Le camion", Aujourdhui), Aujourdhui));
        // Un compte à rebours encore devant ne déclenche rien.
        Assert.Null(Plancher.Evaluer([], ("Le camion", Aujourdhui.AddDays(1)), Aujourdhui));
    }

    [Fact]
    public void Une_echeance_ferme_a_venir_n_est_pas_encore_entrante()
    {
        // Le notaire du 6 n'engage pas la journée du 2 : c'est la liste du jour qui le
        // montre à sa date, et c'est là que le plancher se déclenche.
        var plusTard = new OccurrenceDue("Signer chez le notaire", Aujourdhui.AddDays(4), true);
        Assert.Null(Plancher.Evaluer([plusTard], null, Aujourdhui));
        var sansDate = new OccurrenceDue("Sans date", null, true);
        Assert.Null(Plancher.Evaluer([sansDate], null, Aujourdhui));
    }

    [Theory]
    [InlineData(0, false, RangEdition.Chronique, 7)]
    [InlineData(2, false, RangEdition.Manchette, 5)]
    [InlineData(5, false, RangEdition.Resserre, 4)]
    [InlineData(9, false, RangEdition.Court, 3)]
    [InlineData(10, false, RangEdition.Sommaire, 3)]
    [InlineData(0, true, RangEdition.Evenement, 1)]
    [InlineData(14, true, RangEdition.Evenement, 1)]
    public void Le_rang_suit_la_table_de_bascule(int dues, bool plancher, RangEdition attendu, int budget)
    {
        Assert.Equal(attendu, RangDuJour.Calculer(dues, plancher));
        Assert.Equal(budget, RangDuJour.BudgetDeWidgets(attendu));
    }

    [Fact]
    public void Le_gabarit_rend_ce_que_le_mur_montrait_avant_l_editorialiste()
    {
        var phrase = ("La maison respire.", "Rien au programme aujourd'hui.");
        var texte = GabaritEdition.Ecrire(null, phrase);
        Assert.Equal("", texte.Surtitre);
        Assert.Equal("La maison respire.", texte.Manchette);
        Assert.Equal("Rien au programme aujourd'hui.", texte.Chapeau);
        Assert.Empty(texte.Paragraphes);

        var plancher = new PlancherDuJour(RaisonDePlancher.Ferme, "Signer chez le notaire");
        var force = GabaritEdition.Ecrire(plancher, phrase);
        Assert.Equal("Une date qui ne se négocie pas", force.Surtitre);
        Assert.Equal("Signer chez le notaire", force.Manchette);
    }
}
