using HouseOs.Api.Domaine.Meteo;
using HouseOs.Api.Features.FondsDeTiroir;

namespace HouseOs.Tests.Features.FondsDeTiroir;

/// <summary>
/// La famille « le climat » : les normales du lieu et la journée d'il y a un an. Sans
/// base, sans réseau et sans rendu (vault : D-2026-09-20 Fonds De Tiroir Séparé Du Journal).
///
/// <para>Les normales de référence sont celles d'un automne de la région de Québec —
/// gel vers le 8 octobre, neige vers le 12 novembre — pour que ce qui est testé se
/// recoupe avec la connaissance du coin plutôt qu'avec la sortie du calcul.</para>
/// </summary>
public class FaitsDuClimatTests
{
    private static readonly EtatDuClimat Normal = new(
        SaisonsObservees: 10,
        PremierGel: new DateNormale(10, 8, 6),
        PremiereNeige: new DateNormale(11, 12, 9),
        DerniereDouceur: new DateNormale(10, 3, 7),
        MoisLePlusSec: new MoisNormal(2, 61.4),
        MoisLePlusPluvieux: new MoisNormal(11, 112.8));

    private static IReadOnlyList<FaitDeTiroir> Ouvrir(DateOnly date, EtatDuClimat? climat = null) =>
        [.. FaitsDuClimat.Produire(new ContexteDuJour(date, null, false, Climat: climat ?? Normal))];

    private static FaitDeTiroir? Fait(DateOnly date, string cle, EtatDuClimat? climat = null) =>
        Ouvrir(date, climat).FirstOrDefault(f => f.Cle == cle);

    [Fact]
    public void Sans_normales_la_famille_entiere_se_tait()
    {
        // Une installation neuve, un premier démarrage sans réseau : la même règle que
        // le ciel sans coordonnées — une absence normale, et le journal sort quand même.
        Assert.Empty(FaitsDuClimat.Produire(new ContexteDuJour(new DateOnly(2026, 9, 20), null, false)));
    }

    [Fact]
    public void Une_normale_manquante_ne_fait_taire_que_son_fait()
    {
        // Un lieu où il ne neige pas garde son premier gel : chaque normale répond
        // d'elle-même.
        var sansNeige = Normal with { PremiereNeige = null };

        Assert.Null(Fait(new DateOnly(2026, 11, 5), "climat.neige", sansNeige));
        Assert.NotNull(Fait(new DateOnly(2026, 10, 5), "climat.gel", sansNeige));
    }

    [Fact]
    public void Le_gel_ne_parle_qu_en_approchant()
    {
        // Annoncer le premier gel en juin, c'est de la décoration ; l'annoncer fin
        // septembre, c'est une consigne. La fenêtre est d'un mois avant.
        Assert.Null(Fait(new DateOnly(2026, 6, 15), "climat.gel"));
        Assert.Null(Fait(new DateOnly(2026, 9, 7), "climat.gel"));

        var unMoisAvant = Fait(new DateOnly(2026, 9, 8), "climat.gel");
        Assert.NotNull(unMoisAvant);
        Assert.Equal("Le premier gel", unMoisAvant!.Etiquette);
        Assert.Equal("Vers le 8 octobre", unMoisAvant.Valeur);
        Assert.Contains("10 saisons", unMoisAvant.Texte);
        Assert.Contains("6 jours", unMoisAvant.Texte);
    }

    [Fact]
    public void Le_gel_qui_se_fait_attendre_reste_au_journal_une_semaine()
    {
        // Le 9 octobre est le jour où tout le monde remarque qu'il n'a pas encore
        // gelé : une normale qui disparaîtrait le lendemain de sa date raterait
        // exactement ça. Huit jours plus tard, en revanche, l'affaire est classée.
        Assert.NotNull(Fait(new DateOnly(2026, 10, 15), "climat.gel"));
        Assert.Null(Fait(new DateOnly(2026, 10, 16), "climat.gel"));
    }

    [Fact]
    public void La_derniere_semaine_pese_plus_lourd_que_le_mois_d_avant()
    {
        var loin = Fait(new DateOnly(2026, 9, 20), "climat.gel")!;
        var proche = Fait(new DateOnly(2026, 10, 5), "climat.gel")!;

        Assert.Equal(Pertinence.SuggereUnGeste, loin.Score.Pertinence);
        Assert.Equal(Pertinence.EngageLaJournee, proche.Score.Pertinence);
        Assert.True(proche.Score.Total > loin.Score.Total);
    }

    [Fact]
    public void La_rarete_est_le_compte_des_jours_de_parution_et_rien_d_autre()
    {
        // La règle du fonds : « combien de jours par année le fait peut paraître »,
        // pas « à quelle fréquence on aimerait le lire ». Le gel paraît trente et un
        // jours avant sa date et sept après, soit trente-huit.
        var gel = Fait(new DateOnly(2026, 10, 5), "climat.gel")!;
        Assert.Equal(1.0 / 38, gel.Score.Rarete, tolerance: 1e-12);

        // La douceur a une fenêtre plus courte, donc une rareté plus forte.
        var douceur = Fait(new DateOnly(2026, 10, 1), "climat.douceur")!;
        Assert.Equal(1.0 / 29, douceur.Score.Rarete, tolerance: 1e-12);
        Assert.True(douceur.Score.Rarete > gel.Score.Rarete);
    }

    [Fact]
    public void Une_normale_de_janvier_s_annonce_depuis_decembre()
    {
        // Le passage d'une année à l'autre : une normale au 5 janvier doit se voir le
        // 20 décembre, et non attendre le Nouvel An.
        var tardif = Normal with { PremiereNeige = new DateNormale(1, 5, 8) };

        var fait = Fait(new DateOnly(2026, 12, 20), "climat.neige", tardif);

        Assert.NotNull(fait);
        Assert.Equal("Vers le 5 janvier", fait!.Valeur);
    }

    [Fact]
    public void Une_normale_de_fin_decembre_reste_au_journal_au_debut_de_janvier()
    {
        // Le passage d'année dans l'autre sens : une normale au 28 décembre, lue le
        // 2 janvier, vient de passer depuis cinq jours — en plein dans le rattrapage
        // d'une semaine. Ne regarder que l'année courante et la suivante la renvoyait
        // à presque douze mois et faisait disparaître le fait. Trouvé en revue de code.
        var tardive = Normal with { PremiereNeige = new DateNormale(12, 28, 6) };

        var fait = Fait(new DateOnly(2027, 1, 2), "climat.neige", tardive);

        Assert.NotNull(fait);
        Assert.Equal("Vers le 28 décembre", fait!.Valeur);
        Assert.Equal(Pertinence.EngageLaJournee, fait.Score.Pertinence);

        // Huit jours après, c'est classé — et le fait ne ressort pas pour autant à
        // l'occasion de décembre prochain, qui est encore à un an.
        Assert.Null(Fait(new DateOnly(2027, 1, 5), "climat.neige", tardive));
    }

    [Fact]
    public void Le_mois_le_plus_pluvieux_ne_sort_que_pendant_ce_mois_la()
    {
        // La sécheresse de février ne dit rien un 20 septembre ; elle dit quelque
        // chose le 3 février.
        Assert.Null(Fait(new DateOnly(2026, 9, 20), "climat.mois"));

        var novembre = Fait(new DateOnly(2026, 11, 3), "climat.mois")!;
        Assert.Equal("Le mois le plus pluvieux", novembre.Etiquette);
        Assert.Equal("113 mm en moyenne", novembre.Valeur);
        Assert.Contains("février", novembre.Texte);
        Assert.Equal(Pertinence.EclaireLaJournee, novembre.Score.Pertinence);

        var fevrier = Fait(new DateOnly(2026, 2, 20), "climat.mois")!;
        Assert.Equal("Le mois le plus sec", fevrier.Etiquette);
        // Le 20 du mois, ce n'est plus une nouvelle : le premier de mois était le jour.
        Assert.Equal(Pertinence.Decoration, fevrier.Score.Pertinence);
    }

    [Fact]
    public void L_an_dernier_compare_avec_aujourd_hui_quand_la_prevision_est_connue()
    {
        var journee = new JourneePassee(3.3, 15.1);
        var doux = Normal with { AnDernier = journee, MaxDAujourdhuiC = 22.4 };

        var fait = Fait(new DateOnly(2026, 9, 20), "climat.an-dernier", doux)!;

        Assert.Equal("Il a fait 15 °C", fait.Valeur);
        Assert.Equal("7 degrés de moins qu'aujourd'hui, à pareille date.", fait.Texte);
        // Sept degrés d'écart, c'est sous le seuil : ça se remarque à peine, et le
        // fait reste de la décoration.
        Assert.Equal(Pertinence.Decoration, fait.Score.Pertinence);

        var froid = Normal with { AnDernier = journee, MaxDAujourdhuiC = 3.0 };
        var ecart = Fait(new DateOnly(2026, 9, 20), "climat.an-dernier", froid)!;
        Assert.Equal("12 degrés de plus qu'aujourd'hui, à pareille date.", ecart.Texte);
        Assert.Equal(Pertinence.EclaireLaJournee, ecart.Score.Pertinence);
    }

    [Fact]
    public void Sans_prevision_du_jour_l_an_dernier_dit_autre_chose_plutot_que_de_se_taire()
    {
        // Les tables de prévisions peuvent être vides (premier démarrage, réseau
        // coupé) : le fait garde sa matière et change de phrase.
        var seul = Normal with { AnDernier = new JourneePassee(3.3, 15.1) };

        var fait = Fait(new DateOnly(2026, 9, 20), "climat.an-dernier", seul)!;

        Assert.Equal("Il a fait 15 °C", fait.Valeur);
        Assert.Equal("La nuit était descendue à 3 °C.", fait.Texte);
    }

    [Fact]
    public void Une_annee_sans_archive_de_l_an_dernier_ne_donne_pas_ce_fait()
    {
        // La première année d'une installation : l'archive commence dix ans avant,
        // mais la comparaison n'a lieu que si la journée existe.
        Assert.Null(Fait(new DateOnly(2026, 9, 20), "climat.an-dernier"));
    }

    /// <summary>
    /// Les deux invariants de forme du fonds, balayés sur une année entière et sur
    /// toutes les températures plausibles : une valeur de trente et un signes ou un
    /// texte qui redit l'étiquette se verraient au mur, pas ici.
    /// </summary>
    [Fact]
    public void Les_invariants_du_fonds_tiennent_sur_une_annee_entiere()
    {
        foreach (var fait in UneAnneeDeFaits())
        {
            InvariantsDeFait.Verifier(fait);
        }
    }

    /// <summary>
    /// Les textes du climat portent leur chiffre <b>à la fin</b> : l'imprécision d'une
    /// date normale, l'écart avec aujourd'hui. C'est précisément ce qu'un consommateur
    /// serré coupe en premier — relevé au rendu du 2026-09-20, où « à 9 jours près » est
    /// tombé hors du widget et n'a laissé qu'une date sèche, que la décision interdit.
    /// Une règle d'écriture, comme les trente signes de la valeur : ni pixel ni colonne.
    /// </summary>
    [Fact]
    public void Un_texte_de_climat_tient_en_deux_lignes_pour_que_son_chiffre_survive()
    {
        foreach (var fait in UneAnneeDeFaits())
        {
            Assert.InRange(fait.Texte.Length, 1, 60);
        }
    }

    [Fact]
    public void Chaque_jour_de_l_annee_donne_au_moins_un_fait_de_climat()
    {
        // « Il a fait X° ce jour-là l'an dernier » est le fait de tous les jours de la
        // famille : c'est lui qui la garde présente hors des fenêtres d'automne.
        for (var jour = new DateOnly(2026, 1, 1); jour.Year == 2026; jour = jour.AddDays(1))
        {
            Assert.NotEmpty(Ouvrir(jour, AvecLaJourneeDe(jour)));
        }
    }

    private static IEnumerable<FaitDeTiroir> UneAnneeDeFaits()
    {
        for (var jour = new DateOnly(2026, 1, 1); jour.Year == 2026; jour = jour.AddDays(1))
        {
            foreach (var fait in Ouvrir(jour, AvecLaJourneeDe(jour)))
            {
                yield return fait;
            }
        }
        // Et les mêmes, sans prévision du jour : l'autre branche du texte.
        for (var jour = new DateOnly(2026, 1, 1); jour.Year == 2026; jour = jour.AddDays(1))
        {
            foreach (var fait in Ouvrir(jour, AvecLaJourneeDe(jour) with { MaxDAujourdhuiC = null }))
            {
                yield return fait;
            }
        }
    }

    /// <summary>
    /// Une journée d'il y a un an et une prévision du jour, qui balaient l'année : du
    /// froid de janvier à la chaleur de juillet, et des écarts des deux signes.
    /// </summary>
    private static EtatDuClimat AvecLaJourneeDe(DateOnly jour)
    {
        var max = 13 - 15 * Math.Cos(2 * Math.PI * (jour.DayOfYear - 20) / 365.0);
        return Normal with
        {
            AnDernier = new JourneePassee(max - 9, max),
            // Une prévision qui balance autour de la journée de l'an dernier : le
            // texte doit tenir au-dessus, au-dessous et à l'égalité exacte.
            MaxDAujourdhuiC = max + jour.DayOfYear % 21 - 10,
        };
    }
}
