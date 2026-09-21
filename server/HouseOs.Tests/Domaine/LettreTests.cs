using HouseOs.Api.Domaine.Editorial;
using HouseOs.Api.Domaine.Lettre;

namespace HouseOs.Tests.Domaine;

/// <summary>La série et la note de repli : du calcul pur, sans base ni modèle.</summary>
public class LettreTests
{
    private static readonly DateOnly Dimanche = new(2026, 9, 20);

    [Fact]
    public void La_serie_compte_les_dimanches_de_suite_et_s_arrete_au_premier_trou()
    {
        DateOnly[] coches = [Dimanche.AddDays(-7), Dimanche.AddDays(-14), Dimanche.AddDays(-21), Dimanche.AddDays(-35)];
        Assert.Equal(3, Serie.Compter(Dimanche, coches));
    }

    [Fact]
    public void Une_completion_un_autre_jour_ne_compte_pas()
    {
        Assert.Equal(0, Serie.Compter(Dimanche, [Dimanche.AddDays(-3), Dimanche.AddDays(-10)]));
        Assert.Equal(0, Serie.Compter(Dimanche, []));
    }

    [Fact]
    public void La_serie_ne_compte_pas_le_jour_meme()
    {
        Assert.Equal(1, Serie.Compter(Dimanche, [Dimanche, Dimanche.AddDays(-7)]));
    }

    private static MatiereDeLettre Matiere(
        IReadOnlyList<TachePourEdition>? taches = null, PlancherDuJour? plancher = null,
        MeteoDEdition? meteo = null, CompteProcheDEdition? compte = null,
        IReadOnlyList<FaitPourEdition>? faits = null, IReadOnlyList<EcheanceDevant>? semaine = null) =>
        new(
            new MatiereDEdition(Dimanche, RangEdition.Chronique, plancher, taches ?? [], compte, meteo, faits ?? [], [], null),
            semaine ?? [], [], new Dictionary<string, int>(), []);

    [Fact]
    public void La_note_d_un_jour_vide_se_remplit_avec_le_fonds()
    {
        var note = NoteDeRepli.Composer(Matiere(faits:
        [
            new("ciel.soleil", "Ciel", "LEVER", "6 h 34", "Le soleil se lève à 6 h 34.", true),
            new("ciel.lune", "Ciel", "LUNE", "pleine", "Pleine lune ce soir.", false),
            new("climat.gel", "Climat", "GEL", "3 oct.", "Premier gel vers le 3 octobre.", false),
            new("hasard.dicton", "Hasard", "DICTON", "", "Septembre se nomme le mai de l'automne.", false),
        ]));

        Assert.Equal("Rien à faire aujourd'hui", note.Sujet);
        Assert.Equal(NoteDeRepli.Lignes, note.Paragraphes.Count);
        Assert.Equal("Rien n'est dû aujourd'hui.", note.Paragraphes[0]);
        Assert.Equal("Le soleil se lève à 6 h 34.", note.Paragraphes[1]);
    }

    [Fact]
    public void La_note_d_un_jour_charge_nomme_deux_taches_et_le_reste_en_compte()
    {
        var note = NoteDeRepli.Composer(Matiere(
            taches: [.. Enumerable.Range(1, 14).Select(i => new TachePourEdition($"Tâche {i}", 0, false, null))],
            meteo: new("pluie", 3.6, 9.4),
            compte: new("Noël", 66),
            semaine: [new(Dimanche.AddDays(2), "Vétérinaire", "Ariane", false)]));

        Assert.Equal("14 choses à faire aujourd'hui", note.Sujet);
        Assert.Equal("14 choses aujourd'hui, dont Tâche 1 et Tâche 2.", note.Paragraphes[0]);
        Assert.Equal("Pluie, de 4 à 9 °C.", note.Paragraphes[1]);
        Assert.Equal("Mardi 22 septembre : Vétérinaire (Ariane).", note.Paragraphes[2]);
        Assert.Equal("66 dodos avant Noël.", note.Paragraphes[3]);
    }

    [Fact]
    public void Le_plancher_prend_le_sujet_et_la_premiere_ligne()
    {
        var note = NoteDeRepli.Composer(Matiere(
            taches: [new("Garde de l'animal", 0, true, "Ariane")],
            plancher: new(RaisonDePlancher.Ferme, "Garde de l'animal"),
            compte: new("Déménagement", 1)));

        Assert.Equal("Garde de l'animal", note.Sujet);
        Assert.Equal("Garde de l'animal : ça ne se relègue pas.", note.Paragraphes[0]);
        Assert.Contains("Un dodo avant Déménagement.", note.Paragraphes);
    }

    [Fact]
    public void Sans_meteo_ni_rien_la_note_a_quand_meme_une_ligne()
    {
        var note = NoteDeRepli.Composer(Matiere(taches: [new("Changer les draps", 2, false, null)]));
        Assert.Equal("Une seule chose : Changer les draps", note.Sujet);
        Assert.Single(note.Paragraphes);
    }

    [Fact]
    public void Un_froid_negatif_s_ecrit_avec_le_signe_moins_typographique()
    {
        var note = NoteDeRepli.Composer(Matiere(meteo: new("neige", -5.2, -1.4)));
        Assert.Equal("Neige, de −5 à −1 °C.", note.Paragraphes[1]);
    }

    [Fact]
    public void La_premiere_ligne_coupe_au_dernier_mot_entier()
    {
        var texte = new TexteDeLettre("Sujet", [new string('a', 50) + " " + new string('b', 100)]);
        var ligne = texte.PremiereLigne(60);
        Assert.EndsWith("…", ligne);
        Assert.True(ligne.Length <= 61);
    }
}
