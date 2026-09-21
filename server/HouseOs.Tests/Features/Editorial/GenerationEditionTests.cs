using HouseOs.Api.Domaine;
using HouseOs.Api.Domaine.Editorial;
using HouseOs.Api.Features.Affichage;
using HouseOs.Api.Features.Editorial;
using HouseOs.Api.Features.Humeur;
using HouseOs.Api.Infrastructure;
using HouseOs.Tests.Features.Taches;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HouseOs.Tests.Features.Editorial;

/// <summary>
/// L'édition matérialisée, de bout en bout sur Sqlite : le rendu ne parle jamais au
/// modèle, le service de fond n'écrit qu'une fois par jour, le plancher réédite, et
/// la semaine d'éditions nourrit la fraîcheur du fonds
/// (D-2026-09-20 Une Édition Par Jour Matérialisée).
/// </summary>
public class GenerationEditionTests : TestAvecSqlite
{
    private static readonly DateOnly Aujourdhui = new(2026, 9, 22);
    private static readonly DateTime Matin = Aujourdhui.ToDateTime(new TimeOnly(7, 0));
    private readonly RedacteurFictif _redacteur = new();
    private readonly SignalDeReedition _signal = new();
    private readonly Guid _alain = Guid.NewGuid();

    private static readonly TexteDEdition TexteOpus = new(
        "Première fin de semaine libre", "La maison ne demande rien",
        "Aucune occurrence due · le jour raccourcit",
        ["Onze semaines que la liste n'avait pas été vide.", "Rien ne revient avant le 15."], []);

    public GenerationEditionTests()
    {
        Db.Utilisateurs.Add(new Utilisateur
        {
            Id = _alain, NomUtilisateur = "alain", NomAffichage = "Alain", MotDePasseHash = "x",
        });
        Db.SaveChanges();
    }

    private Task<DonneesEcran> Rendre(DateTime? horloge = null, bool persister = true) =>
        ComposerDonneesEcran.LireAsync(
            Db, horloge ?? Matin, persisterEdition: persister, signal: _signal, journal: NullLogger.Instance);

    private Task<(Edition Edition, bool Generee)> ServiceDeFond(DateOnly? date = null) =>
        GenerationEdition.GenererAsync(
            Db, _redacteur, null, null, null, NullLogger.Instance, date ?? Aujourdhui, Matin,
            remplacer: false, CancellationToken.None);

    private void AjouterTache(
        string titre, DateOnly echeance, bool ferme = false, Guid? zoneId = null, Guid? equipementId = null)
    {
        var tache = new Tache
        {
            Id = Guid.NewGuid(), Titre = titre, CreeParId = _alain, CreeLe = DateTimeOffset.UtcNow,
            EcheanceFerme = ferme, ZoneId = zoneId, EquipementId = equipementId,
        };
        Db.Taches.Add(tache);
        Db.Occurrences.Add(new Occurrence { Id = Guid.NewGuid(), TacheId = tache.Id, Echeance = echeance });
        Db.SaveChanges();
    }

    [Fact]
    public async Task Un_second_rendu_dans_la_meme_journee_n_appelle_pas_le_modele()
    {
        _redacteur.Texte = TexteOpus;

        // Premier rendu : pas d'édition. Le rendu pose un gabarit et lève le drapeau —
        // sans appeler personne.
        var premier = await Rendre();
        Assert.Equal(0, _redacteur.Appels);
        Assert.Equal("Gabarit", premier.Edition!.Source);
        Assert.True(await _signal.AttendreAsync(TimeSpan.Zero, CancellationToken.None));

        // Le service de fond repasse : un appel, l'édition est écrite.
        var (edition, generee) = await ServiceDeFond();
        Assert.True(generee);
        Assert.Equal(1, _redacteur.Appels);
        Assert.Equal(SourceEdition.Llm, edition.Source);
        Assert.Equal("modele-fictif", edition.Modele);
        Assert.False(edition.ReeditionEnAttente);

        // Second rendu, puis second passage du service : personne n'appelle plus.
        var second = await Rendre(Aujourdhui.ToDateTime(new TimeOnly(14, 30)));
        var (_, regeneree) = await ServiceDeFond();
        Assert.False(regeneree);
        Assert.Equal(1, _redacteur.Appels);
        Assert.Equal("La maison ne demande rien", second.Edition!.Manchette);
        Assert.Equal("Llm", second.Edition.Source);
        Assert.Equal(2, second.Edition.Paragraphes.Count);
        Assert.Single(await Db.Editions.ToListAsync());
    }

    [Fact]
    public async Task Sans_cle_api_le_gabarit_ecrit_et_l_edition_est_marquee_telle_quelle()
    {
        // Le repli n'est pas un mode dégradé : c'est une installation sans clé. On
        // passe par le vrai rédacteur Anthropic, sans clé, et sans réseau.
        var cleEnv = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", null);
        try
        {
            var redacteur = new RedacteurAnthropic(
                Options.Create(new HumeurOptions { CleApi = "" }),
                Options.Create(new EditionOptions()),
                NullLogger<RedacteurAnthropic>.Instance);

            var (edition, generee) = await GenerationEdition.GenererAsync(
                Db, redacteur, null, null, null, NullLogger.Instance, Aujourdhui, Matin,
                remplacer: false, CancellationToken.None);

            Assert.True(generee);
            Assert.Equal(SourceEdition.Gabarit, edition.Source);
            Assert.Null(edition.Modele);
            Assert.NotEmpty(edition.Manchette);
            Assert.Empty(edition.Paragraphes);
            Assert.False(edition.ReeditionEnAttente);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", cleEnv);
        }
    }

    [Fact]
    public async Task L_edition_garde_la_matiere_donnee_au_modele_meme_quand_il_s_est_tu()
    {
        AjouterTache("Pneus d'hiver", Aujourdhui);

        // Le rendu pose un gabarit sans rien demander : pas de matière.
        await Rendre();
        var gabarit = await Db.Editions.AsNoTracking().SingleAsync();
        Assert.Null(gabarit.Matiere);

        // Le modèle se tait : le gabarit reste, mais la matière qu'on lui a donnée est
        // là — c'est cette journée-là qu'on voudra rejouer.
        _redacteur.Texte = null;
        var (muette, _) = await ServiceDeFond();
        Assert.Equal(SourceEdition.Gabarit, muette.Source);
        var relue = RedactionLlm.DeserialiserMatiere(muette.Matiere!);
        Assert.NotNull(relue);
        Assert.Equal(Aujourdhui, relue.Date);
        Assert.Equal("Pneus d'hiver", Assert.Single(relue.TachesDues).Titre);
        Assert.Equal(RedactionLlm.SerialiserMatiere(_redacteur.DerniereMatiere!), muette.Matiere);

        // Réécrite avec le modèle : la matière suit, relue de la base.
        _redacteur.Texte = TexteOpus;
        await GenerationEdition.GenererAsync(
            Db, _redacteur, null, null, null, NullLogger.Instance, Aujourdhui, Matin,
            remplacer: true, CancellationToken.None);
        Db.ChangeTracker.Clear();
        var ecrite = await Db.Editions.SingleAsync();
        Assert.Equal(SourceEdition.Llm, ecrite.Source);
        Assert.Equal(RedactionLlm.SerialiserMatiere(_redacteur.DerniereMatiere!), ecrite.Matiere);
    }

    [Fact]
    public async Task Un_modele_qui_se_tait_laisse_le_gabarit_sans_rappel_a_chaque_reveil()
    {
        _redacteur.Texte = null;
        var (edition, _) = await ServiceDeFond();
        Assert.Equal(SourceEdition.Gabarit, edition.Source);
        Assert.Equal(1, _redacteur.Appels);

        // Le drapeau est tombé : le prochain réveil ne rappelle pas le modèle.
        var (_, generee) = await ServiceDeFond();
        Assert.False(generee);
        Assert.Equal(1, _redacteur.Appels);
    }

    [Fact]
    public async Task Un_gabarit_laisse_par_un_modele_muet_est_reessaye_une_fois_puis_laisse_tranquille()
    {
        // L'API surchargée à 5 h 31 : gabarit. Le second essai, plus tard, écrit avec le
        // modèle ; un troisième passage ne trouve plus rien à réessayer.
        _redacteur.Texte = null;
        var (gabarit, _) = await ServiceDeFond();
        Assert.Equal(SourceEdition.Gabarit, gabarit.Source);

        _redacteur.Texte = TexteOpus;
        Assert.True(await Reessayer());
        Assert.Equal(2, _redacteur.Appels);
        Assert.Equal(SourceEdition.Llm, (await Db.Editions.SingleAsync()).Source);

        Assert.False(await Reessayer());
        Assert.Equal(2, _redacteur.Appels);
    }

    [Fact]
    public async Task Un_second_essai_rate_n_ecrit_rien_et_le_gabarit_garde_son_heure()
    {
        _redacteur.Texte = null;
        var (gabarit, _) = await ServiceDeFond();
        var ecritLe = gabarit.GenereLe;

        Assert.True(await Reessayer());
        Assert.Equal(2, _redacteur.Appels);
        Db.ChangeTracker.Clear();
        var relue = await Db.Editions.SingleAsync();
        Assert.Equal(SourceEdition.Gabarit, relue.Source);
        Assert.Equal(ecritLe, relue.GenereLe);
    }

    [Fact]
    public async Task Une_edition_ecrite_avant_son_creneau_garde_le_drapeau_pour_le_matin()
    {
        // Un rattrapage à minuit dix écrit la journée qui commence, mais le créneau du
        // matin doit pouvoir la réécrire avec les faits du matin.
        _redacteur.Texte = TexteOpus;
        var (nuit, _) = await GenerationEdition.GenererAsync(
            Db, _redacteur, null, null, null, NullLogger.Instance, Aujourdhui,
            Aujourdhui.ToDateTime(new TimeOnly(0, 10)), remplacer: false, CancellationToken.None,
            avantLeCreneau: true);
        Assert.True(nuit.ReeditionEnAttente);

        var (matin, generee) = await ServiceDeFond();
        Assert.True(generee);
        Assert.False(matin.ReeditionEnAttente);
        Assert.Equal(2, _redacteur.Appels);
    }

    [Fact]
    public async Task Le_second_essai_ne_reecrit_pas_une_edition_qu_un_autre_a_deja_ecrite()
    {
        // Entre le gabarit et le second essai, une régénération à la main a écrit avec
        // le modèle : rien à réessayer, pas d'appel.
        _redacteur.Texte = null;
        await ServiceDeFond();
        _redacteur.Texte = TexteOpus;
        await GenerationEdition.GenererAsync(
            Db, _redacteur, null, null, null, NullLogger.Instance, Aujourdhui, Matin,
            remplacer: true, CancellationToken.None);
        Assert.Equal(2, _redacteur.Appels);

        Assert.False(await Reessayer());
        Assert.Equal(2, _redacteur.Appels);
        Assert.False(await Reessayer(Aujourdhui.AddDays(1)));
    }

    private Task<bool> Reessayer(DateOnly? date = null) =>
        GenerationEdition.ReessayerAsync(
            Db, _redacteur, null, null, null, NullLogger.Instance, date ?? Aujourdhui,
            Aujourdhui.ToDateTime(new TimeOnly(6, 31)), CancellationToken.None);

    [Fact]
    public async Task L_editorialiste_recoit_la_zone_et_l_equipement_des_taches_dues()
    {
        var garage = new Zone { Id = Guid.NewGuid(), Nom = "Le garage", Type = TypeZone.Interieur };
        var fournaise = new Equipement { Id = Guid.NewGuid(), Nom = "Fournaise" };
        Db.Zones.Add(garage);
        Db.Equipements.Add(fournaise);
        Db.SaveChanges();
        AjouterTache("Ranger le garage", Aujourdhui, zoneId: garage.Id);
        AjouterTache("Changer le filtre", Aujourdhui, equipementId: fournaise.Id);
        AjouterTache("SAAQ — changement d'adresse", Aujourdhui);
        _redacteur.Texte = TexteOpus;

        await ServiceDeFond();

        var taches = _redacteur.DerniereMatiere!.TachesDues;
        Assert.Equal("Le garage", taches.Single(t => t.Titre == "Ranger le garage").Zone);
        Assert.Equal("Fournaise", taches.Single(t => t.Titre == "Changer le filtre").Equipement);
        Assert.True(taches.Single(t => t.Titre.StartsWith("SAAQ")).ANommer);
    }

    [Fact]
    public async Task Le_plancher_qui_se_declenche_apres_coup_redeclenche_une_edition()
    {
        _redacteur.Texte = TexteOpus;
        await ServiceDeFond();
        Assert.Equal(1, _redacteur.Appels);
        var ecrite = await Db.Editions.SingleAsync();
        Assert.Null(ecrite.Plancher);
        Assert.Equal(RangEdition.Chronique, ecrite.Rang);

        // Dans la journée, une tâche ferme entre dans la liste du jour.
        AjouterTache("Signer chez le notaire", Aujourdhui, ferme: true);

        var rendu = await Rendre(Aujourdhui.ToDateTime(new TimeOnly(10, 0)));

        // Le rendu n'attend pas le modèle : le gabarit prend la manchette tout de suite.
        Assert.Equal(0 + 1, _redacteur.Appels);
        Assert.Equal("Signer chez le notaire", rendu.Edition!.Manchette);
        Assert.Equal("Une date qui ne se négocie pas", rendu.Edition.Surtitre);
        Assert.Equal("Evenement", rendu.Edition.Rang);
        Assert.Equal("Ferme", rendu.Edition.Plancher!.Raison);
        Assert.Equal("Gabarit", rendu.Edition.Source);
        Assert.True(rendu.Lignes.Single().EcheanceFerme);

        // Et l'éditorialiste, réveillé, réécrit avec le plancher dans sa matière.
        Assert.True(await _signal.AttendreAsync(TimeSpan.Zero, CancellationToken.None));
        _redacteur.Texte = TexteOpus with { Manchette = "Signer chez le notaire", Surtitre = "Le jour du notaire" };
        var (reeditee, generee) = await ServiceDeFond();
        Assert.True(generee);
        Assert.Equal(2, _redacteur.Appels);
        Assert.Equal(RaisonDePlancher.Ferme, _redacteur.DerniereMatiere!.Plancher!.Raison);
        Assert.Equal("Le jour du notaire", reeditee.Surtitre);
        Assert.Equal(SourceEdition.Llm, reeditee.Source);
        Assert.Single(await Db.Editions.ToListAsync());

        // Un rendu de plus, même plancher : rien ne bouge.
        await Rendre(Aujourdhui.ToDateTime(new TimeOnly(16, 0)));
        Assert.Equal(2, _redacteur.Appels);
        Assert.Equal("Le jour du notaire", (await Db.Editions.SingleAsync()).Surtitre);
    }

    [Fact]
    public async Task Un_compte_a_rebours_a_zero_redeclenche_aussi()
    {
        _redacteur.Texte = TexteOpus;
        await ServiceDeFond();
        Db.ComptesARebours.Add(new CompteARebours { Id = Guid.NewGuid(), Titre = "Le camion", DateCible = Aujourdhui });
        await Db.SaveChangesAsync();

        var rendu = await Rendre();

        Assert.Equal("Le camion", rendu.Edition!.Manchette);
        Assert.Equal("Compte", rendu.Edition.Plancher!.Raison);
        Assert.True((await Db.Editions.SingleAsync()).ReeditionEnAttente);
    }

    [Fact]
    public async Task Une_edition_ecrite_pour_un_autre_jour_reste_a_reecrire_le_jour_venu()
    {
        // L'essai du 27 écrit le 21 garde le drapeau : le 27 au matin, l'éditorialiste
        // la réécrit avec les faits du 27, pas ceux du 21.
        _redacteur.Texte = TexteOpus;
        var (essai, _) = await GenerationEdition.GenererAsync(
            Db, _redacteur, null, null, null, NullLogger.Instance, Aujourdhui.AddDays(6), Matin,
            remplacer: true, CancellationToken.None);
        Assert.True(essai.ReeditionEnAttente);
        Assert.Equal(Aujourdhui.AddDays(6), essai.Date);

        var (dujour, _) = await ServiceDeFond();
        Assert.False(dujour.ReeditionEnAttente);
    }

    [Fact]
    public async Task Les_sources_d_une_autre_journee_se_lisent_a_sa_date()
    {
        // Le rattrapage de trois heures du matin ne doit jamais écrire la veille avec
        // les tâches d'aujourd'hui : la matière porte la date demandée.
        AjouterTache("Demain seulement", Aujourdhui.AddDays(1));
        _redacteur.Texte = TexteOpus;

        await GenerationEdition.GenererAsync(
            Db, _redacteur, null, null, null, NullLogger.Instance, Aujourdhui.AddDays(1), Matin,
            remplacer: true, CancellationToken.None);

        Assert.Equal(Aujourdhui.AddDays(1), _redacteur.DerniereMatiere!.Date);
        Assert.Single(_redacteur.DerniereMatiere.TachesDues);
    }

    [Fact]
    public async Task Deux_ecritures_de_la_meme_journee_ne_font_pas_deux_lignes()
    {
        // Le rattrapage du démarrage et une régénération à la main, à la même minute
        // (vu en prod) : un seul gagnant sur l'index unique, l'autre relit — et
        // remplace s'il devait remplacer.
        _redacteur.Texte = TexteOpus;
        await using var autre = new HouseOsDbContextSqlite(
            new DbContextOptionsBuilder<HouseOsDbContext>().UseSqlite(Connexion).Options);
        var (premiere, _) = await GenerationEdition.GenererAsync(
            autre, _redacteur, null, null, null, NullLogger.Instance, Aujourdhui, Matin,
            remplacer: false, CancellationToken.None);
        Assert.Equal(SourceEdition.Llm, premiere.Source);

        // Le contexte du test a lu « rien » avant que l'autre écrive ; il écrit à son tour.
        _redacteur.Texte = TexteOpus with { Manchette = "La seconde" };
        Db.ChangeTracker.Clear();
        var (perdante, generee) = await GenerationEdition.GenererAsync(
            Db, _redacteur, null, null, null, NullLogger.Instance, Aujourdhui, Matin,
            remplacer: true, CancellationToken.None);

        Assert.True(generee);
        Assert.Equal("La seconde", perdante.Manchette);
        Assert.Single(await Db.Editions.ToListAsync());
    }

    [Fact]
    public async Task Une_horloge_d_essai_sur_un_autre_jour_ne_materialise_rien()
    {
        var noel = new DateTime(2026, 12, 25, 7, 30, 0);
        var rendu = await Rendre(noel, persister: false);

        Assert.NotNull(rendu.Edition);
        Assert.Equal("Gabarit", rendu.Edition.Source);
        Assert.Empty(await Db.Editions.ToListAsync());
        Assert.False(await _signal.AttendreAsync(TimeSpan.Zero, CancellationToken.None));

        // Et un aperçu dont le plancher diffère d'une édition existante ne la modifie
        // pas dans le contexte : un SaveChanges qui suivrait n'écraserait rien.
        _redacteur.Texte = TexteOpus;
        await ServiceDeFond();
        Db.ComptesARebours.Add(new CompteARebours { Id = Guid.NewGuid(), Titre = "Le camion", DateCible = Aujourdhui });
        await Db.SaveChangesAsync();
        var apercu = await Rendre(Matin, persister: false);
        Assert.Equal("Gabarit", apercu.Edition!.Source);
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();
        Assert.Equal(SourceEdition.Llm, (await Db.Editions.SingleAsync(e => e.Date == Aujourdhui)).Source);
    }

    [Fact]
    public async Task La_semaine_d_editions_nourrit_la_fraicheur_et_le_prompt()
    {
        // Hier, l'édition a publié la durée du jour ; il y a dix jours, la dérive.
        Db.Editions.Add(new Edition
        {
            Date = Aujourdhui.AddDays(-1), Manchette = "Hier", Surtitre = "Le surtitre d'hier", Chapeau = "c",
            ClesPubliees = ["ciel.jour"], Source = SourceEdition.Llm, GenereLe = DateTimeOffset.UtcNow,
        });
        Db.Editions.Add(new Edition
        {
            Date = Aujourdhui.AddDays(-10), Manchette = "Vieille", ClesPubliees = ["ciel.derive"],
            Source = SourceEdition.Llm, GenereLe = DateTimeOffset.UtcNow,
        });
        await Db.SaveChangesAsync();

        var memoire = await MemoireDesEditions.LireAsync(Db, Aujourdhui);

        Assert.True(memoire.Fraicheur.Fraicheur("ciel.jour", Aujourdhui) < 1);
        Assert.Equal(1, memoire.Fraicheur.Fraicheur("ciel.derive", Aujourdhui));
        Assert.Equal(1, memoire.Fraicheur.Fraicheur("jamais.sorti", Aujourdhui));
        var precedente = Assert.Single(memoire.Precedentes);
        Assert.Equal("Le surtitre d'hier", precedente.Surtitre);

        // Et l'éditorialiste la reçoit.
        _redacteur.Texte = TexteOpus;
        await ServiceDeFond();
        Assert.Equal("Hier", Assert.Single(_redacteur.DerniereMatiere!.Precedentes).Manchette);
    }

    [Fact]
    public async Task L_edition_du_jour_n_est_jamais_sa_propre_memoire()
    {
        Db.Editions.Add(new Edition
        {
            Date = Aujourdhui, Manchette = "Aujourd'hui", ClesPubliees = ["ciel.jour"],
            Source = SourceEdition.Llm, GenereLe = DateTimeOffset.UtcNow,
        });
        await Db.SaveChangesAsync();

        var memoire = await MemoireDesEditions.LireAsync(Db, Aujourdhui);

        Assert.Equal(1, memoire.Fraicheur.Fraicheur("ciel.jour", Aujourdhui));
        Assert.Empty(memoire.Precedentes);
    }

    [Fact]
    public void Les_cles_publiees_suivent_le_budget_du_rang_et_gardent_ce_que_le_journal_dessine_a_part()
    {
        FaitDeTiroirFictif Fait(string cle) => new(cle);
        var faits = new[] { "a", "b", "calendrier.compte-a-rebours", "c", "ville.collecte", "d", "e" }
            .Select(cle => Fait(cle).Vers()).ToList();

        var cles = GenerationEdition.ClesAPublier(faits, RangEdition.Court);

        // Budget de trois, plus les deux clés à place fixe, dans l'ordre du score.
        Assert.Equal(["a", "b", "calendrier.compte-a-rebours", "c", "ville.collecte"], cles);
        Assert.Equal(["a"], GenerationEdition.ClesAPublier(faits.Take(1).ToList(), RangEdition.Evenement));
    }

    [Fact]
    public void Le_rendu_sert_les_faits_publies_d_abord_puis_le_reste_au_score()
    {
        var faits = new[] { "a", "b", "c", "d" }.Select(cle => new FaitDeTiroirFictif(cle).Vers()).ToList();
        var edition = new Edition { Date = Aujourdhui, Manchette = "m", ClesPubliees = ["c", "a", "disparu"] };

        var ordre = ComposerDonneesEcran.DansLOrdreDeLEdition(faits, edition).Select(f => f.Cle).ToList();

        Assert.Equal(["c", "a", "b", "d"], ordre);
        Assert.Equal(["a", "b", "c", "d"],
            ComposerDonneesEcran.DansLOrdreDeLEdition(faits, null).Select(f => f.Cle));
    }

    private sealed record FaitDeTiroirFictif(string Cle)
    {
        public HouseOs.Api.Features.FondsDeTiroir.FaitDeTiroir Vers() => new(
            Cle, HouseOs.Api.Features.FondsDeTiroir.FamilleDeFait.Ciel, "e", "v", "t",
            new HouseOs.Api.Features.FondsDeTiroir.ScoreDeFait(1, 1, 1));
    }
}
