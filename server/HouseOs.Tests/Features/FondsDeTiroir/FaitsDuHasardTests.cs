using HouseOs.Api.Features.FondsDeTiroir;
using Microsoft.Extensions.Logging.Abstractions;

namespace HouseOs.Tests.Features.FondsDeTiroir;

/// <summary>
/// La famille « le hasard » : le dicton de l'almanach et la fête du jour. Sans base,
/// sans réseau et sans rendu (vault : D-2026-09-20 Fonds De Tiroir Séparé Du Journal).
///
/// <para>La banque livrée avec l'app sert de matériau : c'est elle qui part en prod, et
/// un test qui ne balaierait qu'une banque fabriquée ne dirait rien de ce qui s'affiche
/// au mur.</para>
/// </summary>
public class FaitsDuHasardTests
{
    private static readonly BanqueDuHasard Livree =
        LectureDeLaBanque.Lire(null, AppContext.BaseDirectory, NullLogger.Instance);

    private static IReadOnlyList<FaitDeTiroir> Ouvrir(BanqueDuHasard banque, DateOnly date) =>
        [.. FaitsDuHasard.Produire(new ContexteDuJour(date, null, false, Hasard: banque))];

    private static FaitDeTiroir? Fait(BanqueDuHasard banque, DateOnly date, string cle) =>
        Ouvrir(banque, date).FirstOrDefault(f => f.Cle == cle);

    private static BanqueDuHasard Banque(params FeteDeLAnnee[] fetes) => new([], fetes);

    // Le texte d'une fête doit AJOUTER quelque chose au nom : les fixtures respectent
    // l'invariant du fonds, sans quoi elles ne prouveraient rien de la banque livrée.
    private static FeteDeLAnnee Fete(string nom, QuandLaFete quand, bool ferie = false) =>
        new(nom, "Ce que le nom ne dit pas déjà.", ferie, quand);

    [Fact]
    public void Sans_banque_la_famille_entiere_se_tait()
    {
        // La même règle que pour le ciel sans coordonnées : une absence normale, pas
        // une panne. Un foyer qui ne veut pas de dictons garde tout le reste.
        Assert.Empty(FaitsDuHasard.Produire(new ContexteDuJour(new DateOnly(2026, 9, 20), null, false)));
        Assert.Empty(Ouvrir(BanqueDuHasard.Vide, new DateOnly(2026, 9, 20)));
    }

    [Fact]
    public void Le_dicton_est_le_bouche_trou_du_fonds()
    {
        // Le contrat de la famille : le dicton ne prend jamais la place de quelque
        // chose qui compte. Il doit donc passer derrière le fait le plus banal du
        // fonds — la durée du jour, dont un autre test dit qu'elle ne bat rien.
        var dicton = Fait(Livree, new DateOnly(2026, 9, 20), "hasard.dicton")!;

        var duJour = new ScoreDeFait(Rarete.Quotidien, 1, Pertinence.Decoration);
        Assert.True(dicton.Score.Total < duJour.Total);
        // Et il ne disparaît pas pour autant : un bouche-trou qui vaut zéro ne
        // bouche rien.
        Assert.True(dicton.Score.Total > 0);
    }

    [Fact]
    public void Le_dicton_tourne_dans_le_mois_et_reste_fige_pour_la_journee()
    {
        // Deux tirages du même jour doivent donner le même journal ; deux jours de
        // suite, deux dictons — sans quoi le mur radote tout le mois.
        var premier = Fait(Livree, new DateOnly(2026, 9, 20), "hasard.dicton")!;
        Assert.Equal(premier.Texte, Fait(Livree, new DateOnly(2026, 9, 20), "hasard.dicton")!.Texte);
        Assert.NotEqual(premier.Texte, Fait(Livree, new DateOnly(2026, 9, 21), "hasard.dicton")!.Texte);

        // La matière du dicton vit dans le texte long : un proverbe n'a pas de chiffre
        // à mettre en valeur, et il ne tiendrait pas dans la forme courte.
        Assert.Equal("Le dicton", premier.Etiquette);
        Assert.Equal("Du mois de septembre", premier.Valeur);
        Assert.StartsWith("« ", premier.Texte);
    }

    [Fact]
    public void La_preposition_du_mois_s_elide_devant_une_voyelle()
    {
        // « Du mois de août » se voit à trois mètres. Trois mois de l'année commencent
        // par une voyelle.
        Assert.Equal("Du mois d'avril", Fait(Livree, new DateOnly(2026, 4, 3), "hasard.dicton")!.Valeur);
        Assert.Equal("Du mois d'août", Fait(Livree, new DateOnly(2026, 8, 3), "hasard.dicton")!.Valeur);
        Assert.Equal("Du mois d'octobre", Fait(Livree, new DateOnly(2026, 10, 3), "hasard.dicton")!.Valeur);
        Assert.Equal("Du mois de mai", Fait(Livree, new DateOnly(2026, 5, 3), "hasard.dicton")!.Valeur);
    }

    [Fact]
    public void Un_mois_sans_dicton_se_tait_sans_casser_la_famille()
    {
        var banque = new BanqueDuHasard([new DictonDeLAlmanach(1, "Janvier sec et sage est un bon présage.")], []);

        Assert.NotNull(Fait(banque, new DateOnly(2026, 1, 15), "hasard.dicton"));
        Assert.Null(Fait(banque, new DateOnly(2026, 2, 15), "hasard.dicton"));
    }

    [Fact]
    public void La_fete_a_date_fixe_ne_sort_que_le_jour_meme()
    {
        var saintJean = Banque(Fete("La Saint-Jean", new QuandLaFete { Mois = 6, Jour = 24 }, ferie: true));

        var fait = Fait(saintJean, new DateOnly(2026, 6, 24), "hasard.fete")!;
        Assert.Equal("Un jour férié", fait.Etiquette);
        Assert.Equal("La Saint-Jean", fait.Valeur);

        Assert.Null(Fait(saintJean, new DateOnly(2026, 6, 23), "hasard.fete"));
        Assert.Null(Fait(saintJean, new DateOnly(2026, 6, 25), "hasard.fete"));
        // Et elle revient l'année suivante : la banque décrit une règle, pas une date.
        Assert.NotNull(Fait(saintJean, new DateOnly(2031, 6, 24), "hasard.fete"));
    }

    [Fact]
    public void Une_journee_nationale_ne_se_dit_pas_comme_un_jour_chome()
    {
        var banque = Banque(Fete("L'Halloween", new QuandLaFete { Mois = 10, Jour = 31 }));
        var fait = Fait(banque, new DateOnly(2026, 10, 31), "hasard.fete")!;

        Assert.Equal("Une journée nationale", fait.Etiquette);
        // Un congé engage la journée ; une journée qu'on souligne l'éclaire seulement.
        var ferie = Banque(Fete("L'Halloween", new QuandLaFete { Mois = 10, Jour = 31 }, ferie: true));
        Assert.True(Fait(ferie, new DateOnly(2026, 10, 31), "hasard.fete")!.Score.Total > fait.Score.Total);
    }

    [Fact]
    public void Les_quatre_formes_de_date_donnent_les_bonnes_journees()
    {
        // Sans elles, la moitié des jours fériés du Québec manqueraient à l'appel :
        // Pâques, les Patriotes, le Travail et l'Action de grâce sont tous mobiles.
        Assert.Equal(new DateOnly(2026, 9, 7),
            new QuandLaFete { Mois = 9, JourDeSemaine = "lundi", Rang = 1 }.DansLAnnee(2026));
        Assert.Equal(new DateOnly(2026, 10, 12),
            new QuandLaFete { Mois = 10, JourDeSemaine = "lundi", Rang = 2 }.DansLAnnee(2026));
        Assert.Equal(new DateOnly(2026, 6, 21),
            new QuandLaFete { Mois = 6, JourDeSemaine = "dimanche", Rang = 3 }.DansLAnnee(2026));
        // Un rang négatif se compte depuis la fin du mois.
        Assert.Equal(new DateOnly(2026, 5, 25),
            new QuandLaFete { Mois = 5, JourDeSemaine = "lundi", Rang = -1 }.DansLAnnee(2026));

        // « Le lundi qui précède le 25 mai » : strictement avant, même quand le 25 est
        // lui-même un lundi — c'est la lettre de la Loi sur les normes du travail.
        Assert.Equal(new DateOnly(2026, 5, 18),
            new QuandLaFete { Mois = 5, JourDeSemaine = "lundi", Jour = 25 }.DansLAnnee(2026));
        Assert.Equal(new DateOnly(2027, 5, 24),
            new QuandLaFete { Mois = 5, JourDeSemaine = "lundi", Jour = 25 }.DansLAnnee(2027));

        // Le comput grégorien, contre des dimanches de Pâques publiés.
        Assert.Equal(new DateOnly(2024, 3, 31), new QuandLaFete { DepuisPaques = 0 }.DansLAnnee(2024));
        Assert.Equal(new DateOnly(2025, 4, 20), new QuandLaFete { DepuisPaques = 0 }.DansLAnnee(2025));
        Assert.Equal(new DateOnly(2026, 4, 5), new QuandLaFete { DepuisPaques = 0 }.DansLAnnee(2026));
        Assert.Equal(new DateOnly(2027, 3, 28), new QuandLaFete { DepuisPaques = 0 }.DansLAnnee(2027));
        Assert.Equal(new DateOnly(2026, 4, 3), new QuandLaFete { DepuisPaques = -2 }.DansLAnnee(2026));
        Assert.Equal(new DateOnly(2026, 4, 6), new QuandLaFete { DepuisPaques = 1 }.DansLAnnee(2026));
    }

    [Fact]
    public void Une_fete_qu_on_ne_sait_pas_dater_ne_sort_pas()
    {
        // Même règle que partout dans le fonds : une source qui ne tient pas debout se
        // tait, elle ne fait jamais tomber la composition.
        Assert.Null(new QuandLaFete().DansLAnnee(2026));
        Assert.Null(new QuandLaFete { Mois = 13, Jour = 1 }.DansLAnnee(2026));
        Assert.Null(new QuandLaFete { Mois = 2, Jour = 30 }.DansLAnnee(2026));
        Assert.Null(new QuandLaFete { Mois = 9, JourDeSemaine = "lundy", Rang = 1 }.DansLAnnee(2026));
        // Un cinquième lundi dans un mois qui n'en a que quatre.
        Assert.Null(new QuandLaFete { Mois = 9, JourDeSemaine = "lundi", Rang = 5 }.DansLAnnee(2026));

        var bancale = Banque(Fete("Une fête sans date", new QuandLaFete { Rang = 2 }));
        Assert.Empty(Ouvrir(bancale, new DateOnly(2026, 9, 20)).Where(f => f.Cle == "hasard.fete"));
    }

    [Fact]
    public void Deux_fetes_le_meme_jour_ne_donnent_qu_un_fait_et_toujours_le_meme()
    {
        // Le 21 juin 2026 porte à la fois la fête des Pères et la journée des peuples
        // autochtones. Le jour chômé passe devant, et le nom départage le reste : deux
        // tirages de la même journée doivent donner le même journal.
        var banque = Banque(
            Fete("Les peuples autochtones", new QuandLaFete { Mois = 6, Jour = 21 }),
            Fete("La fête des Pères", new QuandLaFete { Mois = 6, JourDeSemaine = "dimanche", Rang = 3 }, ferie: true));

        var faits = Ouvrir(banque, new DateOnly(2026, 6, 21)).Where(f => f.Cle == "hasard.fete").ToList();
        var fait = Assert.Single(faits);
        Assert.Equal("La fête des Pères", fait.Valeur);
    }

    [Fact]
    public void La_rarete_de_la_fete_se_compte_dans_la_banque_elle_meme()
    {
        // « Combien de jours par année le fait peut paraître » — et la banque les
        // compte. Un foyer qui n'inscrit que ses jours chômés obtient un fait plus rare
        // que celui qui inscrit toutes les journées nationales, et c'est exact.
        var quand = new QuandLaFete { Mois = 6, Jour = 24 };
        var maigre = Banque(Fete("La Saint-Jean", quand, ferie: true));
        var fournie = Banque(
            Fete("La Saint-Jean", quand, ferie: true),
            Fete("Une autre", new QuandLaFete { Mois = 1, Jour = 2 }),
            Fete("Une troisième", new QuandLaFete { Mois = 1, Jour = 3 }));

        var date = new DateOnly(2026, 6, 24);
        Assert.Equal(1.0, Fait(maigre, date, "hasard.fete")!.Score.Rarete);
        Assert.Equal(1.0 / 3, Fait(fournie, date, "hasard.fete")!.Score.Rarete, tolerance: 1e-12);
    }

    [Fact]
    public void Un_nom_de_fete_trop_long_est_elague_plutot_que_coupe_au_mur()
    {
        // Le nom vient d'un fichier, donc d'un inconnu : il passe par l'élagage comme
        // n'importe quel titre saisi. Élaguer, pas rapetisser.
        var banque = Banque(Fete(
            "La journée internationale de la commémoration de quelque chose",
            new QuandLaFete { Mois = 3, Jour = 4 }));

        var fait = Fait(banque, new DateOnly(2026, 3, 4), "hasard.fete")!;
        Assert.Equal("La journée internationale de…", fait.Valeur);
        Assert.InRange(fait.Valeur.Length, 1, 30);
    }

    /// <summary>
    /// Les deux invariants de forme du fonds, balayés sur une année entière avec la
    /// banque <b>livrée</b> : c'est celle qui part en prod. Un dicton de quarante
    /// signes ou une fête dont le texte redit le nom se verraient au mur, pas ici.
    /// </summary>
    [Fact]
    public void La_banque_livree_respecte_les_invariants_du_fonds_sur_une_annee()
    {
        Assert.NotEmpty(Livree.Dictons);
        Assert.NotEmpty(Livree.Fetes);

        for (var jour = new DateOnly(2026, 1, 1); jour.Year == 2026; jour = jour.AddDays(1))
        {
            InvariantsDeFait.Verifier(Ouvrir(Livree, jour));
        }
    }

    [Fact]
    public void La_banque_livree_donne_un_dicton_chaque_jour_de_l_annee()
    {
        // C'est ce qu'on demande à un bouche-trou : ne jamais laisser un trou. Les
        // douze mois doivent donc être servis, y compris février d'une année bissextile.
        for (var jour = new DateOnly(2028, 1, 1); jour.Year == 2028; jour = jour.AddDays(1))
        {
            Assert.NotNull(Fait(Livree, jour, "hasard.dicton"));
        }
    }

    [Fact]
    public void La_banque_livree_porte_les_jours_feries_du_Quebec()
    {
        // Recoupé sur 2026 : quatre de ces huit journées sont mobiles, et c'est
        // exactement pour elles que la banque sait décrire autre chose qu'une date.
        var attendus = new Dictionary<DateOnly, string>
        {
            [new DateOnly(2026, 1, 1)] = "Le jour de l'An",
            [new DateOnly(2026, 4, 3)] = "Le Vendredi saint",
            [new DateOnly(2026, 4, 6)] = "Le lundi de Pâques",
            [new DateOnly(2026, 5, 18)] = "La Journée des patriotes",
            [new DateOnly(2026, 6, 24)] = "La Saint-Jean",
            [new DateOnly(2026, 7, 1)] = "La fête du Canada",
            [new DateOnly(2026, 9, 7)] = "La fête du Travail",
            [new DateOnly(2026, 10, 12)] = "L'Action de grâce",
            [new DateOnly(2026, 12, 25)] = "Noël",
        };

        foreach (var (date, nom) in attendus)
        {
            var fait = Fait(Livree, date, "hasard.fete");
            Assert.NotNull(fait);
            Assert.Equal(nom, fait.Valeur);
            Assert.Equal("Un jour férié", fait.Etiquette);
        }
    }
}
