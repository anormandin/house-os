using HouseOs.Api.Domaine;
using HouseOs.Api.Domaine.Ephemerides;
using HouseOs.Api.Domaine.Humeur;
using HouseOs.Api.Features.Affichage;
using HouseOs.Api.Features.Auth;
using HouseOs.Api.Features.FluxExternes;
using HouseOs.Api.Features.FondsDeTiroir;
using HouseOs.Api.Features.Meteo;
using HouseOs.Api.Features.Taches;

namespace HouseOs.Tests.Features.Affichage;

public class ComposerDonneesEcranTests
{
    private static readonly DateTime Maintenant = new(2026, 9, 3, 14, 30, 0);
    private static readonly DateOnly Aujourdhui = new(2026, 9, 3);
    private static readonly UtilisateurDto Alain = new(Guid.NewGuid(), "alain", "Alain");
    private static readonly UtilisateurDto Ariane = new(Guid.NewGuid(), "ariane", "Ariane");

    private static OccurrenceDto Occurrence(
        string titre, DateOnly? echeance, UtilisateurDto? assigne = null,
        string statut = "EnAttente", UtilisateurDto? completeePar = null) =>
        new(Guid.NewGuid(), Guid.NewGuid(), titre, null, echeance, statut, assigne, completeePar,
            completeePar is null ? null : new DateTimeOffset(Maintenant), null, null, null, "Ponctuelle");

    private static DonneesEcran Composer(
        IReadOnlyList<OccurrenceDto>? ouvertes = null,
        IReadOnlyList<OccurrenceDto>? faites = null,
        PhraseDuJour? phrase = null,
        MeteoDto? meteo = null,
        IReadOnlyList<EvenementExterneDto>? evenements = null,
        IReadOnlyList<CompteARebours>? comptes = null,
        string? lieu = null,
        DateOnly? premiereParution = null,
        ReglagesDuCiel? ciel = null,
        EtatDeLaMaison? maison = null,
        EtatDuCalendrier? calendrier = null) =>
        ComposerDonneesEcran.Composer(
            Maintenant, ouvertes ?? [], faites ?? [], phrase, meteo, evenements ?? [], comptes ?? [],
            lieu, premiereParution, ciel, maison, calendrier);

    [Fact]
    public void Les_ouvertes_precedent_les_faites_et_le_retard_est_marque()
    {
        var donnees = Composer(
            ouvertes:
            [
                Occurrence("Sortir le bac", Aujourdhui.AddDays(-2), Alain),
                Occurrence("Arroser", Aujourdhui, Ariane),
            ],
            faites: [Occurrence("Vaisselle", Aujourdhui, Alain, "Completee", Ariane)]);

        Assert.Collection(donnees.Lignes,
            l =>
            {
                Assert.Equal("Sortir le bac", l.Titre);
                // En jours, pas en booléen : le plancher du journal se déclenche à
                // partir de trois jours de retard.
                Assert.Equal(2, l.JoursDeRetard);
                Assert.False(l.Faite);
                Assert.Equal("Alain", l.Assigne);
            },
            l => Assert.Equal(0, l.JoursDeRetard),
            l =>
            {
                Assert.True(l.Faite);
                // La ligne faite porte qui l'a faite, pas qui devait la faire.
                Assert.Equal("Ariane", l.Assigne);
            });
        Assert.Equal(2, donnees.Ouvertes);
        Assert.Equal(1, donnees.EnRetard);
        Assert.Equal(1, donnees.Faites);
        Assert.Equal(0, donnees.LignesEnPlus);
    }

    [Fact]
    public void Le_plafond_de_lignes_compte_ce_qui_deborde()
    {
        var ouvertes = Enumerable.Range(1, 26)
            .Select(i => Occurrence($"Tâche {i}", Aujourdhui)).ToList();
        var faites = Enumerable.Range(1, 4)
            .Select(i => Occurrence($"Faite {i}", Aujourdhui, statut: "Completee", completeePar: Alain)).ToList();

        var donnees = Composer(ouvertes, faites);

        // La mention « + N autres » prend la dernière place.
        Assert.Equal(ComposerDonneesEcran.MaxLignes - 1, donnees.Lignes.Count);
        Assert.Equal(4, donnees.LignesEnPlus);
        // Les faites sont les premières élaguées : elles sont derrière.
        Assert.Equal(26, donnees.Lignes.Count(l => l.Faite == false));
    }

    [Fact]
    public void La_journee_la_plus_chargee_de_la_prod_tient_sans_elagage()
    {
        // 14 tâches le 2026-10-20 : mesuré sur les maquettes, trois colonnes en
        // tiennent ~27. Le plafond ne doit plus mordre sur une vraie journée.
        var ouvertes = Enumerable.Range(1, 14)
            .Select(i => Occurrence($"Démarche {i}", Aujourdhui)).ToList();

        var donnees = Composer(ouvertes);

        Assert.Equal(14, donnees.Lignes.Count);
        Assert.Equal(0, donnees.LignesEnPlus);
    }

    [Fact]
    public void Le_retard_est_compte_en_jours_et_les_faites_n_en_portent_pas()
    {
        var donnees = Composer(
            ouvertes: [Occurrence("Vieille affaire", Aujourdhui.AddDays(-9), Alain)],
            faites: [Occurrence("Traînée", Aujourdhui.AddDays(-5), Alain, "Completee", Alain)]);

        Assert.Equal(9, donnees.Lignes[0].JoursDeRetard);
        Assert.Equal(0, donnees.Lignes[1].JoursDeRetard);
    }

    [Fact]
    public void La_meteo_prend_le_moment_present_et_retombe_sur_le_jour()
    {
        var jour = new JourMeteoDto(Aujourdhui, 12, 24, 0, 10, 3);
        var avecMaintenant = new MeteoDto(null, new MaintenantDto(21.4, 0), [jour], [new VerdictDto("Tondre", "Bon", "sec")]);
        var sansMaintenant = new MeteoDto(null, null, [jour], []);

        var a = Composer(meteo: avecMaintenant).Meteo!;
        Assert.Equal(21.4, a.TemperatureC);
        Assert.Equal(0, a.CodeMeteo);
        Assert.Single(a.Verdicts);

        var b = Composer(meteo: sansMaintenant).Meteo!;
        Assert.Equal(24, b.TemperatureC);
        Assert.Equal(3, b.CodeMeteo);
    }

    [Fact]
    public void Sans_prevision_du_jour_il_n_y_a_pas_de_meteo()
    {
        var demainSeulement = new MeteoDto(null, null, [new JourMeteoDto(Aujourdhui.AddDays(1), 1, 2, 0, 0, 0)], []);
        Assert.Null(Composer(meteo: demainSeulement).Meteo);
        Assert.Null(Composer(meteo: null).Meteo);
    }

    /// <summary>
    /// Le bandeau du jour montre ce qui n'a pas de famille au fonds de tiroir. Depuis
    /// l'étape 6, les collectes et la ville en ont une — et elle, contrairement au
    /// bandeau, sait se taire quand son flux n'est plus alimenté. Les laisser ici
    /// publierait deux fois le même événement, et publierait celui d'un gratteur mort
    /// (trouvé en revue de code, étape 6).
    /// </summary>
    [Fact]
    public void Le_bandeau_du_jour_laisse_les_collectes_et_la_ville_a_leur_famille()
    {
        var evenements = new List<EvenementExterneDto>
        {
            new("Rentrée", "Ecole", Aujourdhui, null),
            new("Ordures", "Collecte", Aujourdhui, null),
            new("Séance du conseil", "Municipal", Aujourdhui, null),
            new("Souper", "Autre", Aujourdhui, null),
        };

        var donnees = Composer(evenements: evenements);

        Assert.Equal(["Rentrée", "Souper"], donnees.EvenementsDuJour.Select(e => e.Titre));
    }

    [Fact]
    public void Le_prochain_compte_a_rebours_est_le_plus_proche_a_venir()
    {
        var comptes = new List<CompteARebours>
        {
            new() { Titre = "Noël", DateCible = new DateOnly(2026, 12, 25) },
            new() { Titre = "Déménagement", DateCible = new DateOnly(2026, 10, 6) },
            new() { Titre = "Passé", DateCible = Aujourdhui.AddDays(-1) },
        };

        var donnees = Composer(comptes: comptes);

        Assert.Equal("Déménagement", donnees.ProchainCompte!.Titre);
    }

    [Fact]
    public void La_phrase_serveur_est_transmise_ou_absente()
    {
        var phrase = new PhraseDuJour
        {
            Date = Aujourdhui, Moment = MomentJournee.Matin, Titre = "On avance.",
            SousTitre = "Deux choses à faire.", Source = SourcePhrase.Gabarit, GenereLe = new DateTimeOffset(Maintenant),
        };

        Assert.Equal("On avance.", Composer(phrase: phrase).Phrase!.Titre);
        Assert.Null(Composer().Phrase);
        Assert.Equal(Aujourdhui, Composer().Date);
    }

    [Fact]
    public void Le_lieu_de_publication_vient_de_la_configuration_et_peut_manquer()
    {
        // Le dépôt est public : rien de propre à un foyer n'a de défaut dans le code.
        Assert.Null(Composer().Lieu);
        Assert.Null(Composer(lieu: "   ").Lieu);
        Assert.Equal("Rue Fraser, Québec", Composer(lieu: "  Rue Fraser, Québec  ").Lieu);
    }

    [Fact]
    public void Le_numero_d_edition_compte_les_jours_depuis_la_premiere_entree_du_journal()
    {
        // Le jour même de la première entrée, c'est l'édition numéro 1.
        Assert.Equal(1, ComposerDonneesEcran.NumeroEdition(Aujourdhui, Aujourdhui));
        Assert.Equal(461, ComposerDonneesEcran.NumeroEdition(Aujourdhui, Aujourdhui.AddDays(-460)));
        Assert.Equal(461, Composer(premiereParution: Aujourdhui.AddDays(-460)).NumeroEdition);
    }

    [Fact]
    public void Sans_journal_ou_avec_un_journal_du_futur_il_n_y_a_pas_de_numero()
    {
        // Installation neuve : pas de numéro plutôt qu'un « N° 1 » qui vieillirait mal.
        Assert.Null(Composer().NumeroEdition);
        // Une entrée datée de demain (horloge de travers, import) ne sort pas un
        // numéro nul ou négatif.
        Assert.Null(ComposerDonneesEcran.NumeroEdition(Aujourdhui, Aujourdhui.AddDays(1)));
    }

    [Fact]
    public void Sans_aucune_source_la_composition_sort_sans_fonds_de_tiroir()
    {
        // Chaque famille a sa source, et chacune est facultative : la page doit tenir
        // même si les coordonnées manquent, comme tout le reste de l'écran.
        Assert.Empty(Composer().Faits);
    }

    [Fact]
    public void La_maison_et_le_calendrier_arrivent_sans_reglages_de_ciel()
    {
        // Les trois familles sont indépendantes : un foyer sans coordonnées dans son
        // `.env` doit quand même recevoir ce que son journal de complétion raconte.
        var faits = Composer(
            maison: new EtatDeLaMaison(
                [.. Enumerable.Range(0, 9).Select(i => Aujourdhui.AddDays(-i))],
                [], [], [], 0, 0, [], []),
            calendrier: EtatDuCalendrier.Vide with
            {
                ProchainCompte = new CompteDuCalendrier("Déménagement", new DateOnly(2026, 10, 6)),
            }).Faits;

        Assert.Contains(faits, f => f.Cle == "maison.record" && f.Famille == nameof(FamilleDeFait.Maison));
        Assert.Contains(faits, f => f.Cle == "calendrier.compte-a-rebours"
                                    && f.Famille == nameof(FamilleDeFait.Calendrier));
        Assert.DoesNotContain(faits, f => f.Famille == nameof(FamilleDeFait.Ciel));
    }

    [Fact]
    public void Le_fonds_de_tiroir_du_jour_arrive_deja_classe()
    {
        var faits = Composer(ciel: CielDeQuebec()).Faits;

        Assert.NotEmpty(faits);
        Assert.All(faits, f => Assert.Equal(nameof(FamilleDeFait.Ciel), f.Famille));
        // Le fonds arrive déjà classé, et la composition ne réordonne rien.
        // Le fait le plus banal du fonds ferme la marche, toujours.
        Assert.Equal("ciel.jour", faits[^1].Cle);
    }

    [Fact]
    public void Une_tache_ouverte_dans_une_zone_exterieure_fait_sortir_la_noirceur()
    {
        var dehors = Guid.NewGuid();
        var dedans = Guid.NewGuid();
        var ciel = CielDeQuebec(dehors);

        var aLInterieur = Composer(
            ouvertes: [Occurrence("Plier le linge", Aujourdhui) with { ZoneId = dedans }], ciel: ciel);
        Assert.DoesNotContain(aLInterieur.Faits, f => f.Cle == "ciel.noirceur");

        var aLExterieur = Composer(
            ouvertes: [Occurrence("Rentrer les boyaux", Aujourdhui) with { ZoneId = dehors }], ciel: ciel);
        Assert.Contains(aLExterieur.Faits, f => f.Cle == "ciel.noirceur");
    }

    private static ReglagesDuCiel CielDeQuebec(params Guid[] zonesExterieures) =>
        new(new Lieu(46.85, -71.62),
            TimeZoneInfo.FindSystemTimeZoneById("America/Toronto"),
            zonesExterieures.ToHashSet());
}
