using HouseOs.Api.Domaine;
using HouseOs.Api.Domaine.Humeur;
using HouseOs.Api.Features.Affichage;
using HouseOs.Api.Features.Auth;
using HouseOs.Api.Features.FluxExternes;
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
        IReadOnlyList<CompteARebours>? comptes = null) =>
        ComposerDonneesEcran.Composer(
            Maintenant, ouvertes ?? [], faites ?? [], phrase, meteo, evenements ?? [], comptes ?? []);

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
                Assert.True(l.EnRetard);
                Assert.False(l.Faite);
                Assert.Equal("Alain", l.Assigne);
            },
            l => Assert.False(l.EnRetard),
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
        var ouvertes = Enumerable.Range(1, 9)
            .Select(i => Occurrence($"Tâche {i}", Aujourdhui)).ToList();
        var faites = Enumerable.Range(1, 4)
            .Select(i => Occurrence($"Faite {i}", Aujourdhui, statut: "Completee", completeePar: Alain)).ToList();

        var donnees = Composer(ouvertes, faites);

        // La mention « + N autres » prend la dixième place.
        Assert.Equal(ComposerDonneesEcran.MaxLignes - 1, donnees.Lignes.Count);
        Assert.Equal(4, donnees.LignesEnPlus);
        // Les faites sont les premières élaguées : elles sont derrière.
        Assert.Equal(9, donnees.Lignes.Count(l => l.Faite == false));
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

    [Fact]
    public void Les_evenements_du_jour_et_la_prochaine_collecte_sont_extraits()
    {
        var evenements = new List<EvenementExterneDto>
        {
            new("Rentrée", "Ecole", Aujourdhui, null),
            new("Ordures", "Collecte", Aujourdhui.AddDays(2), null),
            new("Recyclage", "Collecte", Aujourdhui.AddDays(9), null),
        };

        var donnees = Composer(evenements: evenements);

        Assert.Single(donnees.EvenementsDuJour);
        Assert.Equal("Rentrée", donnees.EvenementsDuJour[0].Titre);
        Assert.Equal("Ordures", donnees.ProchaineCollecte!.Titre);
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
}
