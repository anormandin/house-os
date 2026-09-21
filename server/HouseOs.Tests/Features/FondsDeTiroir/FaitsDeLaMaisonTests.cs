using HouseOs.Api.Features.FondsDeTiroir;

namespace HouseOs.Tests.Features.FondsDeTiroir;

/// <summary>
/// La famille « la maison » se teste sur des fixtures de journal de complétion, sans
/// base et sans rendu (vault : D-2026-09-20 Fonds De Tiroir Séparé Du Journal).
/// </summary>
public class FaitsDeLaMaisonTests
{
    private static readonly DateOnly Aujourdhui = new(2026, 9, 20);

    private static IReadOnlyList<FaitDeTiroir> Ouvrir(EtatDeLaMaison maison, DateOnly? date = null) =>
        [.. FaitsDeLaMaison.Produire(new ContexteDuJour(date ?? Aujourdhui, null, false, maison))];

    private static FaitDeTiroir? Fait(EtatDeLaMaison maison, string cle, DateOnly? date = null) =>
        Ouvrir(maison, date).FirstOrDefault(f => f.Cle == cle);

    /// <summary>Les N jours qui précèdent (et incluent) une date de fin.</summary>
    private static List<DateOnly> Serie(DateOnly fin, int jours) =>
        [.. Enumerable.Range(0, jours).Select(i => fin.AddDays(-i))];

    [Fact]
    public void Sans_journal_de_completion_la_famille_entiere_se_tait()
    {
        // C'est l'état d'une installation neuve, et celui de l'écran tant que la
        // composition ne lui passe rien : une absence normale, jamais une panne.
        Assert.Empty(FaitsDeLaMaison.Produire(new ContexteDuJour(Aujourdhui, null, false)));
        Assert.Empty(Ouvrir(EtatDeLaMaison.Vide));
    }

    [Fact]
    public void La_serie_tient_la_journee_avant_qu_on_ait_coche_quoi_que_ce_soit()
    {
        // Le piège : à 6 h du matin rien n'est encore fait aujourd'hui. Si la série
        // exigeait une complétion du jour, l'écran annoncerait « série rompue » chaque
        // matin et « 12 jours » chaque soir — un journal qui se contredit tout seul.
        // Un vieux record de quinze jours, pour que la série en cours reste une série
        // en cours et ne bascule pas dans le fait « record ».
        var ancienRecord = Serie(Aujourdhui.AddDays(-10), 15);
        var maison = EtatDeLaMaison.Vide with
        {
            JoursActifs = [.. Serie(Aujourdhui.AddDays(-1), 6), .. ancienRecord],
        };

        var serie = Assert.IsType<FaitDeTiroir>(Fait(maison, "maison.serie"));
        Assert.Equal("6 jours d'affilée", serie.Valeur);

        // Et la complétion du jour la prolonge, sans casser quoi que ce soit.
        var avecAujourdhui = maison with { JoursActifs = [.. Serie(Aujourdhui, 7), .. ancienRecord] };
        Assert.Equal("7 jours d'affilée", Fait(avecAujourdhui, "maison.serie")!.Valeur);

        // Deux jours seulement, ce n'est pas une série.
        Assert.Null(Fait(
            maison with { JoursActifs = [.. Serie(Aujourdhui, 2), .. ancienRecord] }, "maison.serie"));
    }

    [Fact]
    public void Un_trou_d_une_journee_casse_la_serie_mais_pas_le_record()
    {
        // Douze jours, un jour blanc, puis quatre : la série en cours vaut quatre et le
        // record tient à douze.
        var maison = EtatDeLaMaison.Vide with
        {
            JoursActifs = [.. Serie(Aujourdhui, 4), .. Serie(Aujourdhui.AddDays(-5), 12)],
        };

        var serie = Assert.IsType<FaitDeTiroir>(Fait(maison, "maison.serie"));
        Assert.Equal("4 jours d'affilée", serie.Valeur);
        Assert.Equal("Le record de la maison est de 12 jours.", serie.Texte);
        Assert.Null(Fait(maison, "maison.record"));
    }

    [Fact]
    public void Le_record_prend_la_place_de_la_serie_plutot_que_de_doubler_la_colonne()
    {
        // Deux widgets voisins pour le même chiffre, c'est une colonne perdue — c'est
        // le défaut que la revue de l'étape 2 avait relevé sur « DEHORS ».
        var maison = EtatDeLaMaison.Vide with { JoursActifs = Serie(Aujourdhui, 9) };

        var record = Assert.IsType<FaitDeTiroir>(Fait(maison, "maison.record"));
        Assert.Equal("9 jours d'affilée", record.Valeur);
        Assert.Null(Fait(maison, "maison.serie"));
        // Et il bat largement la série ordinaire : six fois l'an contre tous les jours.
        Assert.True(record.Score.Total
            > Fait(maison with { JoursActifs = [.. Serie(Aujourdhui, 4), .. Serie(Aujourdhui.AddDays(-5), 12)] },
                "maison.serie")!.Score.Total);

        // Quatre jours d'affilée n'ont jamais été un record.
        Assert.Null(Fait(maison with { JoursActifs = Serie(Aujourdhui, 4) }, "maison.record"));
    }

    [Fact]
    public void La_premiere_serie_d_une_maison_neuve_se_dit_quand_meme()
    {
        // Le trou trouvé en revue à l'étape 4 : une série qui est AUSSI le record, mais
        // trop courte pour en être un, se taisait deux fois. La série se taisait parce
        // qu'elle était le record ; le record se taisait parce qu'il n'avait pas cinq
        // jours. Une maison qui coche trois jours d'affilée pour la première fois —
        // exactement ce que ce fait existe pour raconter — n'affichait rien.
        foreach (var jours in new[] { 3, 4 })
        {
            var neuve = EtatDeLaMaison.Vide with { JoursActifs = Serie(Aujourdhui, jours) };

            Assert.Null(Fait(neuve, "maison.record"));
            var serie = Assert.IsType<FaitDeTiroir>(Fait(neuve, "maison.serie"));
            Assert.Equal($"{jours} jours d'affilée", serie.Valeur);
            // Et le texte n'y redit pas le chiffre du dessus sous prétexte que la série
            // est le record : il ajoute, comme partout ailleurs dans le fonds.
            Assert.Equal("C'est ce que la maison a fait de mieux jusqu'ici.", serie.Texte);
        }

        // Passé cinq jours, c'est bien le record qui reprend la parole, seul.
        var recordAtteint = EtatDeLaMaison.Vide with { JoursActifs = Serie(Aujourdhui, 5) };
        Assert.NotNull(Fait(recordAtteint, "maison.record"));
        Assert.Null(Fait(recordAtteint, "maison.serie"));
    }

    [Fact]
    public void Les_seances_attendent_la_dixieme_et_la_dizaine_les_remonte()
    {
        var neuf = EtatDeLaMaison.Vide with
        {
            Seances = [new SeancesDeTache("Boîtes!", 9, new DateOnly(2026, 8, 26))],
        };
        Assert.Null(Fait(neuf, "maison.seances"));

        var vingtSept = neuf with
        {
            Seances = [new SeancesDeTache("Boîtes!", 27, new DateOnly(2026, 8, 26))],
        };
        var fait = Assert.IsType<FaitDeTiroir>(Fait(vingtSept, "maison.seances"));
        Assert.Equal("Depuis le 26 août", fait.Etiquette);
        Assert.Equal("27 séances", fait.Valeur);
        // Le point d'exclamation du titre ne se recolle pas au point de la phrase.
        Assert.Equal("Toutes pour la même tâche, Boîtes.", fait.Texte);

        // La dizaine franchie est le chiffre qu'on retient : ce jour-là, le fait remonte.
        var trente = neuf with { Seances = [new SeancesDeTache("Boîtes!", 30, new DateOnly(2026, 8, 26))] };
        Assert.True(Fait(trente, "maison.seances")!.Score.Total > fait.Score.Total);
    }

    [Fact]
    public void Un_titre_de_tache_trop_long_est_elague_a_un_mot_entier()
    {
        // Même règle que pour « ça s'en vient » : le widget coupait la phrase et lui
        // faisait perdre sa ponctuation. On coupe le titre, à un mot entier, et la
        // phrase reste entière (laissé en suspens à l'étape 3, tranché à l'étape 4).
        var maison = EtatDeLaMaison.Vide with
        {
            Seances = [new SeancesDeTache(
                "Vider et trier les boîtes du sous-sol avant le déménagement!", 12, new DateOnly(2026, 8, 26))],
        };

        var fait = Assert.IsType<FaitDeTiroir>(Fait(maison, "maison.seances"));
        Assert.Equal("Toutes pour la même tâche, Vider et trier les boîtes du…", fait.Texte);
        Assert.DoesNotContain("….", fait.Texte);
    }

    [Fact]
    public void Le_doyen_annonce_son_prochain_entretien_et_se_tait_dans_une_maison_neuve()
    {
        var maison = EtatDeLaMaison.Vide with
        {
            Equipements =
            [
                new EquipementDeLaMaison("Machine à café Jura A1", new DateOnly(2024, 3, 1), null),
                new EquipementDeLaMaison("Chauffe-eau électrique", new DateOnly(2010, 6, 15), new DateOnly(2026, 10, 31)),
            ],
        };

        var doyen = Assert.IsType<FaitDeTiroir>(Fait(maison, "maison.doyen"));
        Assert.Equal("16 ans", doyen.Valeur);
        Assert.Equal("Chauffe-eau électrique, dont le prochain entretien est le 31 octobre.", doyen.Texte);

        // Sans date d'achat, aucun doyen — c'est le cas de presque tous les équipements
        // d'une installation qui commence.
        Assert.Null(Fait(
            maison with { Equipements = [new EquipementDeLaMaison("Toiture", null, null)] },
            "maison.doyen"));

        // Et « 0 an » ne se dit pas : une maison neuve n'a pas de doyen.
        Assert.Null(Fait(
            maison with { Equipements = [new EquipementDeLaMaison("Bassin", Aujourdhui.AddDays(-30), null)] },
            "maison.doyen"));
    }

    [Fact]
    public void Une_zone_sans_tache_est_vide_et_non_negligee()
    {
        // La nuance compte : quatre des cinq pièces de la prod n'ont aucune tâche. Les
        // annoncer « jamais rien » serait un reproche adressé à personne.
        var vides = EtatDeLaMaison.Vide with { Zones = [new ZoneDeLaMaison("Salon", 0, null)] };
        Assert.Null(Fait(vides, "maison.piece-oubliee"));

        var jamais = EtatDeLaMaison.Vide with { Zones = [new ZoneDeLaMaison("Sous-sol", 3, null)] };
        var fait = Assert.IsType<FaitDeTiroir>(Fait(jamais, "maison.piece-oubliee"));
        Assert.Equal("Jamais rien", fait.Valeur);
        Assert.Equal("Sous-sol : aucune de ses tâches n'a encore été cochée.", fait.Texte);

        // Deux semaines sans rien, ce n'est pas de la négligence.
        var tranquille = EtatDeLaMaison.Vide with
        {
            Zones = [new ZoneDeLaMaison("Cuisine", 2, Aujourdhui.AddDays(-14))],
        };
        Assert.Null(Fait(tranquille, "maison.piece-oubliee"));

        var oubliee = EtatDeLaMaison.Vide with
        {
            Zones =
            [
                new ZoneDeLaMaison("Cuisine", 2, Aujourdhui.AddDays(-14)),
                new ZoneDeLaMaison("Sous-sol", 1, new DateOnly(2026, 6, 18)),
            ],
        };
        var negligee = Assert.IsType<FaitDeTiroir>(Fait(oubliee, "maison.piece-oubliee"));
        Assert.Equal("94 jours", negligee.Valeur);
        Assert.Equal("Sous-sol : rien de coché depuis le 18 juin.", negligee.Texte);
    }

    [Fact]
    public void Le_cout_de_l_annee_dit_sur_combien_d_interventions_il_s_etale()
    {
        var maison = EtatDeLaMaison.Vide with { CoutDeLAnnee = 1240.50m, InterventionsDeLAnnee = 7 };

        var cout = Assert.IsType<FaitDeTiroir>(Fait(maison, "maison.cout"));
        Assert.Equal("Depuis le 1er janvier", cout.Etiquette);
        // Les cents ne se lisent pas à trois mètres.
        Assert.DoesNotContain(",", cout.Valeur);
        Assert.Contains("1", cout.Valeur);
        Assert.Equal("Réparti sur 7 interventions consignées.", cout.Texte);

        // Une année sans un sou consigné ne dit rien plutôt que « 0 $ ».
        Assert.Null(Fait(EtatDeLaMaison.Vide, "maison.cout"));
    }

    [Fact]
    public void Ce_jour_la_l_an_dernier_ne_donne_rien_avant_la_deuxieme_annee()
    {
        // Le fait est écrit maintenant et restera muet jusqu'en septembre 2027 : c'est
        // un argument pour le bâtir tôt, pas tard.
        Assert.Null(Fait(EtatDeLaMaison.Vide, "maison.an-dernier"));

        var une = EtatDeLaMaison.Vide with { FaitLAnDernier = ["Changer les draps"] };
        var seule = Assert.IsType<FaitDeTiroir>(Fait(une, "maison.an-dernier"));
        Assert.Equal("Une chose réglée", seule.Valeur);
        Assert.Equal("C'était Changer les draps.", seule.Texte);

        var trois = EtatDeLaMaison.Vide with
        {
            FaitLAnDernier = ["Nettoyer les gouttières", "Changer les draps", "Boîtes!"],
        };
        var plusieurs = Assert.IsType<FaitDeTiroir>(Fait(trois, "maison.an-dernier"));
        Assert.Equal("3 choses réglées", plusieurs.Valeur);
        Assert.Equal("Dont Nettoyer les gouttières, entre autres.", plusieurs.Texte);
    }

    [Fact]
    public void L_anniversaire_ne_sort_que_le_jour_dit_et_jamais_la_premiere_annee()
    {
        var maison = EtatDeLaMaison.Vide with
        {
            Anniversaires =
            [
                new AnniversaireDeLaMaison("La toiture", new DateOnly(2023, 9, 20)),
                new AnniversaireDeLaMaison("Déménagement", new DateOnly(2026, 10, 6)),
            ],
        };

        var fait = Assert.IsType<FaitDeTiroir>(Fait(maison, "maison.anniversaire"));
        Assert.Equal("3 ans aujourd'hui", fait.Valeur);
        Assert.Equal("La toiture, depuis 2023.", fait.Texte);

        // La veille, rien : un anniversaire ne dure pas une semaine.
        Assert.Null(Fait(maison, "maison.anniversaire", Aujourdhui.AddDays(-1)));
        // Et le jour même de l'emménagement, ce n'est pas encore un anniversaire.
        Assert.Null(Fait(maison, "maison.anniversaire", new DateOnly(2026, 10, 6)));
        Assert.NotNull(Fait(maison, "maison.anniversaire", new DateOnly(2027, 10, 6)));
    }

    /// <summary>
    /// Trouvé en revue de code : un fait <b>vrai tous les jours</b> coté « quelques
    /// fois par an » prend la tête du journal <b>tous les matins de l'année</b>, parce
    /// que la fraîcheur se remet à neuf au bout de sept jours. « Le doyen de la maison /
    /// 16 ans » resterait au mur à vie. La rareté se compte en jours de parution
    /// possibles, pas en envies : ces faits sont quotidiens, et c'est la pertinence qui
    /// dit ce qu'ils changent à aujourd'hui.
    /// </summary>
    [Fact]
    public void Un_fait_vrai_tous_les_jours_ne_prend_pas_la_place_d_un_fait_rare()
    {
        var maison = UneMaisonQuiParle(Aujourdhui);
        var faits = Ouvrir(maison);

        var rares = new[] { "maison.record", "maison.anniversaire", "maison.an-dernier" };
        var quotidiens = new[] { "maison.doyen", "maison.piece-oubliee", "maison.cout", "maison.seances" };

        var plancherDesRares = rares.Min(cle => faits.Single(f => f.Cle == cle).Score.Total);
        var plafondDesQuotidiens = quotidiens.Max(cle => faits.Single(f => f.Cle == cle).Score.Total);

        Assert.True(
            plancherDesRares > plafondDesQuotidiens,
            "Un fait qui ne peut paraître que quelques jours par an doit passer devant " +
            "un fait vrai tous les jours, sans quoi le journal radote.");

        // Et ils restent devant le fait le plus banal du fonds : ils éclairent la
        // journée, là où l'heure du coucher est de la décoration.
        var banal = new ScoreDeFait(Rarete.Quotidien, 1, 1).Total;
        Assert.All(quotidiens, cle => Assert.True(faits.Single(f => f.Cle == cle).Score.Total > banal));
    }

    [Fact]
    public void Les_deux_invariants_de_forme_tiennent_sur_une_annee_entiere()
    {
        // Les mêmes que pour le ciel : la valeur tient dans une colonne de widget, et
        // le texte long n'est jamais la redite de ce qui est déjà au-dessus.
        for (var jour = new DateOnly(2027, 1, 1); jour.Year == 2027; jour = jour.AddDays(1))
        {
            var faits = Ouvrir(UneMaisonQuiParle(jour), jour);
            Assert.NotEmpty(faits);
            InvariantsDeFait.Verifier(faits);
        }
    }

    /// <summary>
    /// Une maison qui a de quoi remplir les huit faits, ancrée sur la date donnée —
    /// c'est ce qui permet de balayer l'année sans écrire trois cent soixante-cinq
    /// fixtures.
    /// </summary>
    private static EtatDeLaMaison UneMaisonQuiParle(DateOnly jour) => new(
        [.. Serie(jour, 9)],
        [new SeancesDeTache("Boîtes!", 27, jour.AddDays(-25))],
        [new EquipementDeLaMaison("Chauffe-eau électrique", new DateOnly(2010, 6, 15), jour.AddDays(41))],
        [new ZoneDeLaMaison("Sous-sol", 1, jour.AddDays(-94))],
        1240.50m,
        7,
        [new AnniversaireDeLaMaison("La toiture", jour.AddYears(-3))],
        ["Nettoyer les gouttières", "Changer les draps"]);
}
