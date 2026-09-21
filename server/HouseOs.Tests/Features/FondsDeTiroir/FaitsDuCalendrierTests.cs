using HouseOs.Api.Features.FondsDeTiroir;

namespace HouseOs.Tests.Features.FondsDeTiroir;

/// <summary>
/// La famille « le calendrier » : comptes à rebours, échéances à venir, fenêtres
/// saisonnières et papiers qui expirent. Sans base et sans rendu
/// (vault : D-2026-09-20 Fonds De Tiroir Séparé Du Journal).
/// </summary>
public class FaitsDuCalendrierTests
{
    private static readonly DateOnly Aujourdhui = new(2026, 9, 20);

    private static IReadOnlyList<FaitDeTiroir> Ouvrir(EtatDuCalendrier calendrier, DateOnly? date = null) =>
        [.. FaitsDuCalendrier.Produire(new ContexteDuJour(date ?? Aujourdhui, null, false, null, calendrier))];

    private static FaitDeTiroir? Fait(EtatDuCalendrier calendrier, string cle, DateOnly? date = null) =>
        Ouvrir(calendrier, date).FirstOrDefault(f => f.Cle == cle);

    [Fact]
    public void Sans_calendrier_la_famille_entiere_se_tait()
    {
        Assert.Empty(FaitsDuCalendrier.Produire(new ContexteDuJour(Aujourdhui, null, false)));
        Assert.Empty(Ouvrir(EtatDuCalendrier.Vide));
    }

    [Fact]
    public void Le_compte_a_rebours_porte_son_titre_et_se_presse_dans_la_derniere_semaine()
    {
        var demenagement = EtatDuCalendrier.Vide with
        {
            ProchainCompte = new CompteDuCalendrier("Déménagement", new DateOnly(2026, 10, 6)),
        };

        var loin = Assert.IsType<FaitDeTiroir>(Fait(demenagement, "calendrier.compte-a-rebours"));
        // Le titre saisi par le foyer EST le sujet : le reléguer au texte long
        // donnerait « DANS 16 JOURS » sans jamais dire de quoi.
        Assert.Equal("Déménagement", loin.Etiquette);
        Assert.Equal("Dans 16 jours", loin.Valeur);
        Assert.Equal("Au calendrier, le 6 octobre 2026.", loin.Texte);

        var proche = Fait(demenagement, "calendrier.compte-a-rebours", new DateOnly(2026, 10, 1))!;
        Assert.True(proche.Score.Total > loin.Score.Total);

        Assert.Equal("C'est demain", Fait(demenagement, "calendrier.compte-a-rebours", new DateOnly(2026, 10, 5))!.Valeur);
        Assert.Equal("C'est aujourd'hui", Fait(demenagement, "calendrier.compte-a-rebours", new DateOnly(2026, 10, 6))!.Valeur);

        // Passé, il se tait — l'encadré du journal fait de même.
        Assert.Null(Fait(demenagement, "calendrier.compte-a-rebours", new DateOnly(2026, 10, 7)));
        // Et à plus de quatre mois, c'est une idée, pas une échéance.
        Assert.Null(Fait(demenagement, "calendrier.compte-a-rebours", new DateOnly(2026, 5, 1)));
    }

    /// <summary>
    /// Le titre cité vient du foyer et n'a pas de longueur bornée. Le consommateur
    /// coupait la phrase en cours de route et lui faisait perdre sa fin — « La première
    /// le 28 septembre : Homelab — plan de migratio… ». C'est le titre qu'on élague,
    /// pas la phrase : élaguer, pas rapetisser (laissé en suspens à l'étape 3,
    /// tranché à l'étape 4).
    /// </summary>
    [Fact]
    public void Un_titre_cite_est_elague_pour_que_la_phrase_garde_sa_fin()
    {
        var long_ = EtatDuCalendrier.Vide with
        {
            CaSEnVient =
            [
                new EcheanceProchaine("Homelab — plan de migration des services vers le nouveau rack", Aujourdhui.AddDays(8)),
                new EcheanceProchaine("Changer filtre Jura", Aujourdhui.AddDays(11)),
            ],
        };

        var fait = Assert.IsType<FaitDeTiroir>(Fait(long_, "calendrier.ca-s-en-vient"));
        Assert.Equal("La première le 28 septembre : Homelab — plan de migration des…", fait.Texte);
        // Et « … . » ne s'écrit pas : les points de suspension achèvent la phrase.
        Assert.DoesNotContain("….", fait.Texte);
    }

    [Fact]
    public void Ca_s_en_vient_commence_a_sept_jours_et_demande_au_moins_deux_echeances()
    {
        // En deçà de sept jours, la liste du jour et l'encadré les montrent déjà : le
        // journal se répéterait, ce qui est le mode de panne de cette vue.
        var proches = EtatDuCalendrier.Vide with
        {
            CaSEnVient =
            [
                new EcheanceProchaine("Signer la nouvelle hypothèque", Aujourdhui.AddDays(3)),
                new EcheanceProchaine("Aller chercher tréteaux", Aujourdhui.AddDays(6)),
            ],
        };
        Assert.Null(Fait(proches, "calendrier.ca-s-en-vient"));

        // Une seule échéance du mois n'est pas « ça s'en vient » : c'est une tâche, et
        // elle sortira d'elle-même le jour venu.
        var seule = EtatDuCalendrier.Vide with
        {
            CaSEnVient = [new EcheanceProchaine("Changer fitre Jura", Aujourdhui.AddDays(11))],
        };
        Assert.Null(Fait(seule, "calendrier.ca-s-en-vient"));

        var horizon = EtatDuCalendrier.Vide with
        {
            CaSEnVient =
            [
                new EcheanceProchaine("Nettoyer les gouttières", Aujourdhui.AddDays(41)),
                new EcheanceProchaine("Changer fitre Jura", Aujourdhui.AddDays(11)),
                new EcheanceProchaine("Lubrifier les coupe-froid", Aujourdhui.AddDays(25)),
                new EcheanceProchaine("Trop tôt", Aujourdhui.AddDays(2)),
            ],
        };
        var fait = Assert.IsType<FaitDeTiroir>(Fait(horizon, "calendrier.ca-s-en-vient"));
        // Ni celle de dans deux jours, ni celle de dans six semaines.
        Assert.Equal("2 échéances", fait.Valeur);
        Assert.Equal("La première le 1er octobre : Changer fitre Jura.", fait.Texte);
    }

    [Fact]
    public void La_fenetre_qui_se_ferme_passe_devant_celle_qui_vient_de_s_ouvrir()
    {
        // « Il reste neuf jours » engage la journée ; « c'est la saison » est une
        // invitation. Un seul widget pour la saison, et c'est l'urgent qui le prend.
        var calendrier = EtatDuCalendrier.Vide with
        {
            Saisons =
            [
                new FenetreDeSaison("Nettoyer les gouttières", new DateOnly(2026, 9, 18), new DateOnly(2026, 11, 15)),
                new FenetreDeSaison("Rentrer les boyaux", new DateOnly(2026, 8, 1), new DateOnly(2026, 9, 29)),
            ],
        };

        var fait = Assert.IsType<FaitDeTiroir>(Fait(calendrier, "calendrier.saison"));
        Assert.Equal("La saison se ferme", fait.Etiquette);
        Assert.Equal("Il reste 9 jours", fait.Valeur);
        Assert.Equal("Après, Rentrer les boyaux attendra l'an prochain.", fait.Texte);

        // Sans fenêtre qui se referme, c'est celle qui s'ouvre qui parle.
        var ouverture = EtatDuCalendrier.Vide with
        {
            Saisons = [new FenetreDeSaison("Nettoyer les gouttières", new DateOnly(2026, 9, 18), new DateOnly(2026, 11, 15))],
        };
        var qui = Assert.IsType<FaitDeTiroir>(Fait(ouverture, "calendrier.saison"));
        Assert.Equal("La saison s'ouvre", qui.Etiquette);
        Assert.Equal("Depuis 2 jours", qui.Valeur);
        Assert.Equal("Nettoyer les gouttières redevient possible jusqu'au 15 novembre.", qui.Texte);

        // Une semaine plus tard, l'ouverture n'est plus une nouvelle et la fermeture
        // est encore loin : la saison se tait, comme tous les jours de l'automne.
        Assert.Null(Fait(ouverture, "calendrier.saison", new DateOnly(2026, 10, 10)));
        // Hors fenêtre, rien non plus.
        Assert.Null(Fait(ouverture, "calendrier.saison", new DateOnly(2026, 7, 1)));
    }

    [Fact]
    public void Une_garantie_et_un_papier_ne_se_disent_pas_avec_les_memes_mots()
    {
        var garantie = EtatDuCalendrier.Vide with
        {
            Expirations = [new ExpirationProchaine("Vélo électrique Alain", Aujourdhui.AddDays(23), true)],
        };
        var fait = Assert.IsType<FaitDeTiroir>(Fait(garantie, "calendrier.expiration"));
        Assert.Equal("Une garantie expire", fait.Etiquette);
        Assert.Equal("Dans 23 jours", fait.Valeur);
        Assert.Equal("Vélo électrique Alain, jusqu'au 13 octobre 2026.", fait.Texte);

        var papier = EtatDuCalendrier.Vide with
        {
            Expirations = [new ExpirationProchaine("Attestation d'assurance habitation", Aujourdhui.AddDays(5), false)],
        };
        var proche = Assert.IsType<FaitDeTiroir>(Fait(papier, "calendrier.expiration"));
        Assert.Equal("Un papier expire", proche.Etiquette);
        // Deux semaines avant, ça cesse d'être une note de bas de page.
        Assert.True(proche.Score.Total > fait.Score.Total);

        // Dans un an, ce n'est pas une nouvelle : c'est le cas du seul document daté
        // de la prod (assurance Beneva, 1er septembre 2027).
        var loin = EtatDuCalendrier.Vide with
        {
            Expirations = [new ExpirationProchaine("Attestation d'assurance habitation", new DateOnly(2027, 9, 1), false)],
        };
        Assert.Null(Fait(loin, "calendrier.expiration"));
    }

    [Fact]
    public void Les_deux_invariants_de_forme_tiennent_sur_une_annee_entiere()
    {
        for (var jour = new DateOnly(2027, 1, 1); jour.Year == 2027; jour = jour.AddDays(1))
        {
            var faits = Ouvrir(UnCalendrierQuiParle(jour), jour);
            Assert.NotEmpty(faits);
            InvariantsDeFait.Verifier(faits);
        }
    }

    /// <summary>Un calendrier qui a de quoi remplir les quatre faits, ancré sur la date donnée.</summary>
    private static EtatDuCalendrier UnCalendrierQuiParle(DateOnly jour) => new(
        new CompteDuCalendrier("Déménagement", jour.AddDays(16)),
        [
            new EcheanceProchaine("Changer fitre Jura", jour.AddDays(11)),
            new EcheanceProchaine("Lubrifier les coupe-froid", jour.AddDays(25)),
        ],
        [new FenetreDeSaison("Rentrer les boyaux", jour.AddDays(-50), jour.AddDays(9))],
        [new ExpirationProchaine("Vélo électrique Alain", jour.AddDays(23), true)]);
}
