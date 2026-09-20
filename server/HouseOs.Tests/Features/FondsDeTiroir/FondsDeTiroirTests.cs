using HouseOs.Api.Domaine.Ephemerides;
using HouseOs.Api.Features.FondsDeTiroir;

namespace HouseOs.Tests.Features.FondsDeTiroir;

/// <summary>
/// Le fonds de tiroir se teste <b>sans rendu, sans Playwright et sans appareil</b> :
/// c'est le corollaire de test de D-2026-09-20 Fonds De Tiroir Séparé Du Journal.
/// Rien ici ne parle de colonne, de pixel ni de 1-bit.
/// </summary>
public class FondsDeTiroirTests
{
    private static readonly Lieu Quebec = new(46.85, -71.62);
    private static readonly TimeZoneInfo FuseauQuebec = TimeZoneInfo.FindSystemTimeZoneById("America/Toronto");

    private static ContexteDuJour Jour(DateOnly date, bool dehors = false) =>
        new(date, new PointDObservation(Quebec, FuseauQuebec), dehors);

    private static IReadOnlyList<FaitDeTiroir> Ouvrir(DateOnly date, bool dehors = false) =>
        Tiroir.Ouvrir(Jour(date, dehors), HistoriqueDeParution.Vide);

    [Fact]
    public void Un_jour_ordinaire_ne_rend_que_les_faits_quotidiens()
    {
        // Le 12 novembre 2026 : ni équinoxe, ni pleine lune, ni bascule d'heure. Il
        // doit quand même rester de quoi remplir un widget — c'est toute la raison
        // d'être du fonds de tiroir.
        var faits = Ouvrir(new DateOnly(2026, 11, 12));

        Assert.Equal(["ciel.derive", "ciel.jour"], faits.Select(f => f.Cle).Order());
        Assert.All(faits, f => Assert.Equal(FamilleDeFait.Ciel, f.Famille));
        Assert.All(faits, f => Assert.NotEmpty(f.Valeur));
        Assert.All(faits, f => Assert.NotEmpty(f.Texte));
    }

    [Fact]
    public void L_equinoxe_bat_la_duree_du_jour_de_cent_fois()
    {
        // La rareté est la première composante du score : l'équinoxe ne peut sortir
        // que quatre jours sur trois cent soixante-cinq, la durée du jour tous les
        // jours. Le rapport doit être écrasant, sans quoi la surprise n'arrive jamais.
        var faits = Ouvrir(new DateOnly(2026, 9, 22));

        Assert.Equal("ciel.saison", faits[0].Cle);
        var duree = faits.Single(f => f.Cle == "ciel.jour");
        Assert.True(faits[0].Score.Total > 100 * duree.Score.Total);
    }

    [Fact]
    public void La_duree_du_jour_ne_bat_rien()
    {
        // L'autre bout de la règle : le fait le plus banal du fonds est dernier partout
        // où un autre fait existe, et n'est là que pour ne jamais laisser un trou.
        foreach (var date in new[] { new DateOnly(2026, 9, 22), new DateOnly(2026, 9, 25), new DateOnly(2026, 10, 31) })
        {
            var faits = Ouvrir(date);
            Assert.True(faits.Count > 1);
            Assert.Equal("ciel.jour", faits[^1].Cle);
        }
    }

    [Fact]
    public void Le_fait_contextuel_se_tait_les_jours_sans_travail_dehors()
    {
        var date = new DateOnly(2026, 9, 20);

        Assert.DoesNotContain(Ouvrir(date), f => f.Cle == "ciel.noirceur");

        // Avec une tâche ouverte dans une zone extérieure, « il fera noir à 18 h 49 »
        // cesse d'être de la décoration et passe devant les faits quotidiens.
        var dehors = Ouvrir(date, dehors: true);
        var noirceur = Assert.Single(dehors, f => f.Cle == "ciel.noirceur");
        Assert.Contains("18 h 49", noirceur.Valeur);
        Assert.True(noirceur.Score.Total > dehors.Single(f => f.Cle == "ciel.jour").Score.Total);
    }

    [Fact]
    public void Un_fait_sorti_hier_passe_derriere_un_fait_jamais_sorti()
    {
        var date = new DateOnly(2026, 9, 20);
        var frais = Ouvrir(date);
        var deriveFraiche = frais.Single(f => f.Cle == "ciel.derive").Score.Total;

        var historique = new HistoriqueDeParution(
            new Dictionary<string, DateOnly> { ["ciel.derive"] = date.AddDays(-1) });
        var apres = Tiroir.Ouvrir(Jour(date), historique);
        var deriveRessassee = apres.Single(f => f.Cle == "ciel.derive").Score.Total;

        Assert.True(deriveRessassee < deriveFraiche);
        // Mais elle ne disparaît jamais tout à fait : le jour où c'est la seule chose
        // vraie qui reste, mieux vaut se répéter qu'un trou dans le journal.
        Assert.True(deriveRessassee > 0);
        Assert.Contains(apres, f => f.Cle == "ciel.derive");
    }

    [Fact]
    public void Apres_sept_jours_un_fait_revient_entier()
    {
        var date = new DateOnly(2026, 9, 20);
        var reference = Ouvrir(date).Single(f => f.Cle == "ciel.jour").Score.Total;

        foreach (var age in new[] { 7, 30 })
        {
            var historique = new HistoriqueDeParution(
                new Dictionary<string, DateOnly> { ["ciel.jour"] = date.AddDays(-age) });
            var fait = Tiroir.Ouvrir(Jour(date), historique).Single(f => f.Cle == "ciel.jour");
            Assert.Equal(reference, fait.Score.Total, tolerance: 1e-12);
        }
    }

    [Fact]
    public void L_historique_vide_ne_penalise_personne()
    {
        // L'état d'une installation neuve, et celui de l'étape 2 : le fonds doit sortir
        // normalement, jamais en mode dégradé.
        var faits = Ouvrir(new DateOnly(2026, 9, 22));
        Assert.All(faits, f => Assert.Equal(1, f.Score.Fraicheur));
    }

    [Fact]
    public void La_lune_ne_se_dit_qu_a_la_pleine_et_a_la_nouvelle()
    {
        // La fenêtre fait un jour : chaque pleine lune est nommée un soir, pas trois.
        var jours = Enumerable.Range(0, 60)
            .Select(i => new DateOnly(2026, 9, 20).AddDays(i))
            .Count(d => Ouvrir(d).Any(f => f.Cle == "ciel.lune"));

        // Deux lunaisons : deux pleines et deux nouvelles, à une près selon les bords.
        Assert.InRange(jours, 3, 5);
    }

    [Fact]
    public void Le_changement_d_heure_s_annonce_deux_semaines_d_avance_et_se_presse_a_la_fin()
    {
        // Au Québec, on recule le 1er novembre 2026.
        Assert.DoesNotContain(Ouvrir(new DateOnly(2026, 10, 17)), f => f.Cle == "ciel.changement-heure");

        var loin = Ouvrir(new DateOnly(2026, 10, 18)).Single(f => f.Cle == "ciel.changement-heure");
        Assert.Equal("Dans 14 jours", loin.Valeur);
        Assert.Equal("Le 1er novembre, on aura reculé d'une heure.", loin.Texte);

        var veille = Ouvrir(new DateOnly(2026, 10, 31)).Single(f => f.Cle == "ciel.changement-heure");
        Assert.Equal("C'est cette nuit", veille.Valeur);
        Assert.True(veille.Score.Total > loin.Score.Total);
    }

    [Fact]
    public void Le_jour_meme_du_changement_d_heure_le_fait_est_encore_la_et_parle_au_passe()
    {
        // Le bug que la revue a trouvé : la bascule a lieu au petit matin, donc à midi
        // le jour du changement l'horloge porte déjà le nouveau décalage. En comparant
        // à partir d'aujourd'hui, le fait disparaissait le seul jour où il compte —
        // l'écran passait d'un « c'est cette nuit » la veille à plus rien du tout.
        var jourJ = Ouvrir(new DateOnly(2026, 11, 1)).Single(f => f.Cle == "ciel.changement-heure");

        Assert.Equal("C'était cette nuit", jourJ.Valeur);
        Assert.Equal("On a reculé d'une heure cette nuit.", jourJ.Texte);

        // Et le lendemain il se tait : le prochain rendez-vous est en mars.
        Assert.DoesNotContain(Ouvrir(new DateOnly(2026, 11, 2)), f => f.Cle == "ciel.changement-heure");
    }

    [Fact]
    public void La_bascule_jour_nuit_n_est_pas_l_equinoxe()
    {
        // Le jour et la nuit ne sont pas à égalité à l'équinoxe : la réfraction et le
        // diamètre du disque donnent quelques minutes de jour en plus, si bien que la
        // nuit ne passe devant qu'après. C'est justement ce qui en fait un fait à part.
        var equinoxe = new DateOnly(2026, 9, 22);
        Assert.DoesNotContain(Ouvrir(equinoxe), f => f.Cle == "ciel.equilibre");

        var bascule = Enumerable.Range(0, 10)
            .Select(i => equinoxe.AddDays(i))
            .Single(d => Ouvrir(d).Any(f => f.Cle == "ciel.equilibre"));
        Assert.InRange(bascule.DayNumber - equinoxe.DayNumber, 1, 6);
        Assert.Contains("nuit", Ouvrir(bascule).Single(f => f.Cle == "ciel.equilibre").Valeur);
    }

    [Fact]
    public void Deux_tirages_de_la_meme_journee_donnent_le_meme_journal()
    {
        // Sans ordre total, l'écran changerait tout seul d'un réveil à l'autre.
        var date = new DateOnly(2026, 10, 31);
        Assert.Equal(Ouvrir(date).Select(f => f.Cle), Ouvrir(date).Select(f => f.Cle));
    }

    [Fact]
    public void Le_fonds_tient_ailleurs_qu_au_Quebec()
    {
        // Le dépôt est public (vault : Distribution). Deux cas qui casseraient un
        // calcul écrit pour le Québec : l'hémisphère sud, et une latitude où le soleil
        // ne se lève pas du tout.
        // L'équinoxe tombe le 23 septembre à 0 h 09 UTC, donc le 23 au matin en
        // Tasmanie et le 22 au soir au Québec : le même instant, deux dates. C'est
        // exactement l'erreur qu'un écran mural afficherait pendant vingt-quatre heures.
        var hobart = TimeZoneInfo.FindSystemTimeZoneById("Australia/Hobart");
        var lieuHobart = new Lieu(-42.88, 147.32);
        Assert.DoesNotContain(
            Tiroir.Ouvrir(
                new ContexteDuJour(new DateOnly(2026, 9, 22), new PointDObservation(lieuHobart, hobart), false),
                HistoriqueDeParution.Vide),
            f => f.Cle == "ciel.saison");

        var faitsHobart = Tiroir.Ouvrir(
            new ContexteDuJour(new DateOnly(2026, 9, 23), new PointDObservation(lieuHobart, hobart), false),
            HistoriqueDeParution.Vide);
        Assert.Contains(faitsHobart, f => f.Cle == "ciel.saison");
        // Au sud, c'est le jour qui s'allonge en septembre — et c'est l'étiquette qui
        // porte le sens, pour que la valeur tienne sur une rangée de tableau.
        Assert.Equal("Le jour s'allonge", faitsHobart.Single(f => f.Cle == "ciel.derive").Etiquette);
        Assert.Equal(
            "Le jour raccourcit",
            Ouvrir(new DateOnly(2026, 9, 23)).Single(f => f.Cle == "ciel.derive").Etiquette);

        var svalbard = new ContexteDuJour(
            new DateOnly(2026, 12, 21),
            new PointDObservation(new Lieu(78.22, 15.63), TimeZoneInfo.FindSystemTimeZoneById("Europe/Oslo")),
            true);
        var faitsSvalbard = Tiroir.Ouvrir(svalbard, HistoriqueDeParution.Vide);
        // Pas de lever, pas de coucher : le fait « il fera noir » n'a aucun sens et ne
        // sort pas, mais la nuit polaire, elle, se dit.
        Assert.DoesNotContain(faitsSvalbard, f => f.Cle == "ciel.noirceur");
        Assert.Contains("ne se lève pas", faitsSvalbard.Single(f => f.Cle == "ciel.jour").Valeur);
    }

    [Fact]
    public void La_saison_porte_le_nom_du_mois_et_le_texte_ajoute_la_date()
    {
        // Le nom du mois, pas celui de la saison : « équinoxe de printemps » est faux
        // dans l'hémisphère sud et le dépôt est public.
        var decembre = Ouvrir(new DateOnly(2026, 12, 15)).Single(f => f.Cle == "ciel.saison-approche");
        Assert.Equal("Solstice de décembre", decembre.Etiquette);
        Assert.Equal("Dans 6 jours", decembre.Valeur);
        Assert.StartsWith("Le 21 décembre, à ", decembre.Texte);

        var mars = Ouvrir(new DateOnly(2027, 3, 18)).Single(f => f.Cle == "ciel.saison-approche");
        Assert.Equal("Équinoxe de mars", mars.Etiquette);
    }

    /// <summary>
    /// Le texte long doit <b>ajouter</b> quelque chose. Trouvé en revue de code : la
    /// colonne affichait « LE JOUR RACCOURCIT / 3 min par jour / Le jour raccourcit
    /// d'environ 3 minutes par jour », soit trois lignes de mur pour une seule idée —
    /// et c'était le cas le plus courant, pas un cas limite.
    /// </summary>
    [Fact]
    public void Le_texte_long_ne_redit_jamais_l_etiquette_ni_la_valeur()
    {
        foreach (var fait in UneAnneeDeFaits())
        {
            Assert.DoesNotContain(fait.Etiquette, fait.Texte, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(fait.Valeur, fait.Texte, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void La_noirceur_ne_porte_pas_le_meme_titre_que_le_verdict_meteo()
    {
        // Le journal titre déjà « Dehors » son verdict météo, et les deux sortent le
        // même jour par construction : le fait exige une tâche dehors, ce qui est
        // exactement le jour où « bonne journée pour tondre » a le plus de chances de
        // tomber. Deux colonnes voisines coiffées « DEHORS », c'est une colonne perdue.
        var noirceur = Ouvrir(new DateOnly(2026, 9, 20), dehors: true).Single(f => f.Cle == "ciel.noirceur");

        Assert.NotEqual("Dehors", noirceur.Etiquette);
        Assert.Equal("La noirceur", noirceur.Etiquette);
        Assert.Equal("À 18 h 49", noirceur.Valeur);
    }

    /// <summary>
    /// La forme courte doit tenir dans une colonne de widget. Le rendu de l'étape 2 a
    /// montré ce que coûte l'oubli : « Équinoxe de septembre dans 2 jours » se faisait
    /// couper en plein milieu sur le mur. L'étiquette porte le sujet, la valeur porte
    /// le chiffre.
    /// </summary>
    [Fact]
    public void La_valeur_courte_tient_dans_une_colonne()
    {
        foreach (var fait in UneAnneeDeFaits())
        {
            Assert.InRange(fait.Valeur.Length, 1, 30);
            Assert.NotEmpty(fait.Etiquette);
            Assert.NotEmpty(fait.Texte);
        }
    }

    [Fact]
    public void Chaque_jour_de_l_annee_donne_au_moins_un_fait()
    {
        // « Le jour où la maison ne demande rien, il doit rester sept faits à dire. »
        // Le ciel n'en donne pas sept, mais il ne doit jamais en donner zéro.
        for (var jour = new DateOnly(2026, 1, 1); jour.Year == 2026; jour = jour.AddDays(1))
        {
            Assert.NotEmpty(Ouvrir(jour));
        }
    }

    private static IEnumerable<FaitDeTiroir> UneAnneeDeFaits()
    {
        for (var jour = new DateOnly(2026, 1, 1); jour.Year == 2026; jour = jour.AddDays(1))
        {
            foreach (var fait in Ouvrir(jour, dehors: true))
            {
                yield return fait;
            }
        }
    }
}
