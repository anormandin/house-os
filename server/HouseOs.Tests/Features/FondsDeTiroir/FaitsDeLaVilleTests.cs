using HouseOs.Api.Features.FondsDeTiroir;

namespace HouseOs.Tests.Features.FondsDeTiroir;

/// <summary>
/// La famille « la ville » : la prochaine collecte, celle qui ne revient pas, et ce qui
/// se passe en ville. Sans base, sans réseau, et sans une ligne de municipal
/// (vault : D-2026-09-20 Sources Municipales Séparées Par Solidité).
/// </summary>
public class FaitsDeLaVilleTests
{
    private static readonly DateOnly Aujourdhui = new(2026, 9, 20);

    /// <summary>La fenêtre d'ingestion des flux externes : c'est sur elle que se
    /// comptent les habitudes d'un calendrier de collectes.</summary>
    private const int Fenetre = 60;

    /// <summary>Un bac hebdomadaire sur toute la fenêtre — l'ordinaire, celui auquel
    /// une collecte d'espèce se compare.</summary>
    private static List<EvenementDeLaVille> Hebdomadaire(string titre, DateOnly premiere) =>
        [.. Enumerable.Range(0, 8).Select(s => new EvenementDeLaVille(premiere.AddDays(7 * s), titre))];

    private static EtatDeLaVille Ville(
        IEnumerable<EvenementDeLaVille>? collectes = null,
        IEnumerable<EvenementDeLaVille>? municipaux = null,
        int joursDepuisCollectes = 0,
        int? joursDepuisMunicipaux = 0) =>
        new(
            collectes is null ? [] : [new FluxDeLaVille(joursDepuisCollectes, [.. collectes])],
            municipaux is null ? [] : [new FluxDeLaVille(joursDepuisMunicipaux, [.. municipaux])],
            Fenetre);

    private static IReadOnlyList<FaitDeTiroir> Ouvrir(EtatDeLaVille ville, DateOnly? date = null) =>
        [.. FaitsDeLaVille.Produire(new ContexteDuJour(date ?? Aujourdhui, null, false, Ville: ville))];

    private static FaitDeTiroir? Fait(EtatDeLaVille ville, string cle, DateOnly? date = null) =>
        Ouvrir(ville, date).FirstOrDefault(f => f.Cle == cle);

    [Fact]
    public void Sans_flux_la_famille_entiere_se_tait()
    {
        Assert.Empty(FaitsDeLaVille.Produire(new ContexteDuJour(Aujourdhui, null, false)));
        Assert.Empty(Ouvrir(Ville()));
    }

    [Fact]
    public void La_prochaine_collecte_dit_quand_et_porte_le_titre_dans_le_texte()
    {
        var ville = Ville(Hebdomadaire("Ordures et recyclage", Aujourdhui.AddDays(2)));

        var fait = Assert.IsType<FaitDeTiroir>(Fait(ville, "ville.collecte"));

        // Le titre vient de la ville : étiquette fixe, valeur chiffrée, nom dans le
        // texte (vault : Fonds De Tiroir).
        Assert.Equal("La prochaine collecte", fait.Etiquette);
        Assert.Equal("Dans 2 jours", fait.Valeur);
        Assert.Equal("Ordures et recyclage, le 22 septembre.", fait.Texte);
    }

    [Fact]
    public void La_veille_est_le_seul_soir_ou_le_bac_doit_sortir()
    {
        var premiere = Aujourdhui.AddDays(1);
        var ville = Ville(Hebdomadaire("Bac brun", premiere));

        var veille = Fait(ville, "ville.collecte")!;
        var jourMeme = Fait(ville, "ville.collecte", premiere)!;
        var loin = Fait(ville, "ville.collecte", premiere.AddDays(1))!;

        Assert.Equal("C'est demain", veille.Valeur);
        Assert.Equal(Pertinence.EngageLaJournee, veille.Score.Pertinence);
        Assert.Equal("C'est aujourd'hui", jourMeme.Valeur);
        Assert.Equal(Pertinence.SuggereUnGeste, jourMeme.Score.Pertinence);
        Assert.Equal(Pertinence.EclaireLaJournee, loin.Score.Pertinence);
    }

    [Fact]
    public void Une_collecte_a_plus_d_une_semaine_n_est_pas_la_nouvelle_du_jour()
    {
        var ville = Ville(Hebdomadaire("Ordures", Aujourdhui.AddDays(8)));

        Assert.Null(Fait(ville, "ville.collecte"));
    }

    /// <summary>
    /// On ne sait pas ce qu'une collecte <b>est</b> — aucune connaissance municipale
    /// n'entre dans le dépôt : on voit qu'elle ne se répète pas, là où le bac revient
    /// huit fois dans la fenêtre.
    /// </summary>
    [Fact]
    public void Une_collecte_qui_ne_revient_pas_se_dit_deux_semaines_d_avance()
    {
        var collectes = Hebdomadaire("Ordures et recyclage", Aujourdhui.AddDays(2));
        collectes.Add(new EvenementDeLaVille(Aujourdhui.AddDays(10), "Encombrants"));

        var fait = Assert.IsType<FaitDeTiroir>(Fait(Ville(collectes), "ville.collecte-speciale"));

        Assert.Equal("Une collecte spéciale", fait.Etiquette);
        Assert.Equal("Dans 10 jours", fait.Valeur);
        Assert.Contains("Encombrants", fait.Texte);
        // Plus rare que le bac hebdomadaire, qui peut sortir tous les jours.
        Assert.True(fait.Score.Rarete > Rarete.Quotidien);

        // À plus de deux semaines, elle attend son tour.
        collectes[^1] = new EvenementDeLaVille(Aujourdhui.AddDays(20), "Encombrants");
        Assert.Null(Fait(Ville(collectes), "ville.collecte-speciale"));
    }

    [Fact]
    public void Quand_la_speciale_est_la_prochaine_une_seule_des_deux_parle()
    {
        var collectes = Hebdomadaire("Ordures et recyclage", Aujourdhui.AddDays(4));
        collectes.Add(new EvenementDeLaVille(Aujourdhui.AddDays(1), "Sapins de Noël"));

        var faits = Ouvrir(Ville(collectes));

        // Deux widgets pour le même camion, c'est une colonne perdue.
        Assert.Null(faits.FirstOrDefault(f => f.Cle == "ville.collecte"));
        Assert.NotNull(faits.FirstOrDefault(f => f.Cle == "ville.collecte-speciale"));
    }

    [Fact]
    public void Un_calendrier_trop_maigre_ne_designe_aucune_collecte_d_espece()
    {
        // Trois collectes dans toute la fenêtre : aucune habitude, donc rien qui sorte
        // de l'ordinaire — sans quoi un calendrier neuf aurait trois faits « spéciaux ».
        var ville = Ville([
            new EvenementDeLaVille(Aujourdhui.AddDays(2), "Ordures"),
            new EvenementDeLaVille(Aujourdhui.AddDays(9), "Recyclage"),
            new EvenementDeLaVille(Aujourdhui.AddDays(16), "Bac brun"),
        ]);

        Assert.Null(Fait(ville, "ville.collecte-speciale"));
        Assert.NotNull(Fait(ville, "ville.collecte"));
    }

    [Fact]
    public void Un_evenement_municipal_dit_son_heure_quand_il_en_a_une()
    {
        var ville = Ville(municipaux: [
            new EvenementDeLaVille(Aujourdhui.AddDays(2), "Marché public", new TimeOnly(10, 0)),
            new EvenementDeLaVille(Aujourdhui.AddDays(5), "Séance du conseil"),
        ]);

        var fait = Assert.IsType<FaitDeTiroir>(Fait(ville, "ville.evenement"));

        Assert.Equal("En ville", fait.Etiquette);
        Assert.Equal("Dans 2 jours", fait.Valeur);
        Assert.Equal("Marché public, le 22 septembre, à 10 h 00.", fait.Texte);
    }

    /// <summary>
    /// La règle de la famille : un flux qu'on n'alimente plus cesse de parler. Un
    /// gratteur mort il y a une semaine laisserait son programme passer pour celui de
    /// cette semaine (vault : D-2026-09-20 Flux Externe Poussé).
    /// </summary>
    [Fact]
    public void Un_flux_pousse_perime_cesse_de_sortir_au_lieu_de_mentir()
    {
        var municipaux = new List<EvenementDeLaVille>
        {
            new(Aujourdhui.AddDays(2), "Marché public"),
        };

        Assert.NotNull(Fait(Ville(municipaux: municipaux, joursDepuisMunicipaux: 7), "ville.evenement"));
        // Huit jours sans une poussée : le gratteur est mort, pas la ville.
        Assert.Null(Fait(Ville(municipaux: municipaux, joursDepuisMunicipaux: 8), "ville.evenement"));
        // Jamais rien reçu : aussi périmé que trop vieux.
        Assert.Null(Fait(Ville(municipaux: municipaux, joursDepuisMunicipaux: null), "ville.evenement"));
    }

    [Fact]
    public void Un_calendrier_de_collectes_qui_ne_se_telecharge_plus_se_tait_aussi()
    {
        var collectes = Hebdomadaire("Ordures", Aujourdhui.AddDays(1));

        Assert.NotNull(Fait(Ville(collectes, joursDepuisCollectes: 7), "ville.collecte"));
        Assert.Empty(Ouvrir(Ville(collectes, joursDepuisCollectes: 8)));
    }

    /// <summary>
    /// Le cas de la vérification du plan : le gratteur débranché une semaine, et
    /// l'édition sort sans un seul widget de la ville — pas avec un vieux programme.
    /// </summary>
    [Fact]
    public void Un_gratteur_debranche_une_semaine_laisse_l_edition_sans_widget_de_ville()
    {
        var ville = new EtatDeLaVille(
            [],
            [new FluxDeLaVille(8, [new EvenementDeLaVille(Aujourdhui.AddDays(1), "Séance du conseil")])],
            Fenetre);

        Assert.Empty(Ouvrir(ville));
    }

    [Fact]
    public void Un_flux_vide_ne_casse_rien()
    {
        Assert.Empty(Ouvrir(new EtatDeLaVille([new FluxDeLaVille(0, [])], [new FluxDeLaVille(0, [])], Fenetre)));
    }

    /// <summary>
    /// Les deux invariants de forme du fonds, balayés sur une année entière : une
    /// valeur de trente et un signes ou un texte qui redit son étiquette se verraient
    /// au mur, pas ici.
    /// </summary>
    [Fact]
    public void Les_invariants_du_fonds_tiennent_sur_une_annee_entiere()
    {
        for (var jour = new DateOnly(2026, 1, 1); jour.Year == 2026; jour = jour.AddDays(1))
        {
            var collectes = Hebdomadaire("Ordures, recyclage et bac brun", jour.AddDays(1));
            collectes.Add(new EvenementDeLaVille(jour.AddDays(9), "Encombrants et résidus de construction"));
            var ville = Ville(
                collectes,
                [new EvenementDeLaVille(jour.AddDays(3), "Conseil municipal", new TimeOnly(19, 30))]);

            InvariantsDeFait.Verifier(Ouvrir(ville, jour));
        }
    }
}
