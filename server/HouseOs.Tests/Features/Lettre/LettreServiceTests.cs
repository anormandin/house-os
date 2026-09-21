using HouseOs.Api.Domaine;
using HouseOs.Api.Domaine.Lettre;
using HouseOs.Api.Features.Affichage;
using HouseOs.Api.Features.Lettre;
using HouseOs.Api.Features.Meteo;
using HouseOs.Api.Infrastructure;
using HouseOs.Tests.Features.Taches;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HouseOs.Tests.Features.Lettre;

/// <summary>
/// Le service de fond de la lettre, passage par passage sur Sqlite : l'heure, le
/// rattrapage jusqu'à midi, le second essai à une heure, la note, jamais deux fois
/// (D-2026-09-21 Lettre Matérialisée Et Rattrapée Le Jour Même,
/// D-2026-09-21 Lettre Écrite À Part Sur La Même Matière).
/// </summary>
public sealed class LettreServiceTests : TestAvecSqlite
{
    private static readonly DateOnly Aujourdhui = new(2026, 9, 22);
    private static readonly TexteDeLettre Prose = new("Rien à faire, sauf sortir les bacs",
        ["Je ne vous demande rien aujourd'hui, sinon de sortir les bacs au chemin avant que le camion passe ce matin.",
         "Dehors, six degrés au réveil et dix-neuf cet après-midi, ciel dégagé, comme l'an dernier à pareille date.",
         "Douze dodos avant le déménagement, et rien d'autre n'est dû : la journée est à vous deux."]);

    private readonly RedacteurLettreFictif _redacteur = new();
    private readonly EnvoyeurFictif _envoyeur = new();
    private readonly LettreOptions _options = new();
    private readonly ServiceProvider _fournisseur;
    private readonly Guid _alain = Guid.NewGuid();
    private readonly Guid _ariane = Guid.NewGuid();

    public LettreServiceTests()
    {
        var options = new DbContextOptionsBuilder<HouseOsDbContext>().UseSqlite(Connexion).Options;
        var services = new ServiceCollection();
        services.AddScoped<HouseOsDbContext>(_ => new HouseOsDbContextSqlite(options));
        _fournisseur = services.BuildServiceProvider();

        Db.Utilisateurs.AddRange(
            new Utilisateur { Id = _alain, NomUtilisateur = "alain", NomAffichage = "Alain", MotDePasseHash = "x", Courriel = "alain@exemple.tld" },
            new Utilisateur { Id = _ariane, NomUtilisateur = "ariane", NomAffichage = "Ariane", MotDePasseHash = "x" });
        Db.SaveChanges();
    }

    private LettreService Service() => new(
        _fournisseur.GetRequiredService<IServiceScopeFactory>(), _redacteur, _envoyeur,
        Options.Create(_options), Options.Create(new MeteoOptions()), Options.Create(new AffichageOptions()),
        null, NullLogger<LettreService>.Instance);

    private static DateTime A(int heure, int minute = 0) => Aujourdhui.ToDateTime(new TimeOnly(heure, minute));

    private LettreDuMatin? Lettre() => Db.Lettres.AsNoTracking().SingleOrDefault(l => l.Date == Aujourdhui);

    [Fact]
    public async Task Avant_l_heure_rien_ne_se_passe_et_le_reveil_est_le_creneau()
    {
        _redacteur.Texte = Prose;
        var prochain = await Service().PasserAsync(A(3), CancellationToken.None);

        Assert.Equal(0, _redacteur.Appels);
        Assert.Empty(_envoyeur.Envoyes);
        Assert.Equal(new TimeOnly(6, 31), TimeOnly.FromDateTime(prochain.LocalDateTime));
    }

    [Fact]
    public async Task A_l_heure_la_lettre_est_composee_et_part_a_ceux_qui_ont_une_adresse()
    {
        _redacteur.Texte = Prose;
        var prochain = await Service().PasserAsync(A(6, 31), CancellationToken.None);

        var envoye = Assert.Single(_envoyeur.Envoyes);
        Assert.Equal("Rien à faire, sauf sortir les bacs", envoye.Sujet);
        Assert.Equal(["alain@exemple.tld"], envoye.Destinataires.Select(d => d.Adresse));
        Assert.Contains("Bonjour vous deux.", envoye.Texte);
        Assert.Contains("— la maison", envoye.Texte);
        var lettre = Lettre()!;
        Assert.True(lettre.Envoyee);
        Assert.Equal(SourceLettre.Llm, lettre.Source);
        Assert.Equal(["alain@exemple.tld"], lettre.Destinataires);
        Assert.NotNull(lettre.Matiere);
        Assert.Equal(Aujourdhui.AddDays(1), DateOnly.FromDateTime(prochain.LocalDateTime));
    }

    [Fact]
    public async Task Demarre_a_neuf_heures_la_lettre_part_quand_meme()
    {
        _redacteur.Texte = Prose;
        await Service().PasserAsync(A(9), CancellationToken.None);
        Assert.Single(_envoyeur.Envoyes);
    }

    [Fact]
    public async Task Demarre_a_treize_heures_la_journee_est_perdue_et_jamais_rattrapee()
    {
        _redacteur.Texte = Prose;
        var service = Service();
        var prochain = await service.PasserAsync(A(13), CancellationToken.None);
        Assert.Empty(_envoyeur.Envoyes);
        Assert.Null(Lettre());
        Assert.Equal(Aujourdhui.AddDays(1), DateOnly.FromDateTime(prochain.LocalDateTime));

        // Le lendemain, c'est la lettre du lendemain qui part — pas celle d'hier.
        await service.PasserAsync(Aujourdhui.AddDays(1).ToDateTime(new TimeOnly(6, 31)), CancellationToken.None);
        Assert.Null(Lettre());
        Assert.Single(_envoyeur.Envoyes);
    }

    [Fact]
    public async Task Deja_partie_un_second_passage_n_appelle_personne()
    {
        _redacteur.Texte = Prose;
        var service = Service();
        await service.PasserAsync(A(6, 31), CancellationToken.None);
        await service.PasserAsync(A(8), CancellationToken.None);
        await Service().PasserAsync(A(10), CancellationToken.None);

        Assert.Equal(1, _redacteur.Appels);
        Assert.Single(_envoyeur.Envoyes);
    }

    [Fact]
    public async Task Modele_muet_rien_ne_part_et_le_second_essai_est_une_heure_plus_tard()
    {
        _redacteur.Texte = null;
        var prochain = await Service().PasserAsync(A(6, 31), CancellationToken.None);

        Assert.Empty(_envoyeur.Envoyes);
        var lettre = Lettre()!;
        Assert.False(lettre.Envoyee);
        Assert.Equal(SourceLettre.Gabarit, lettre.Source);
        Assert.Equal(new TimeOnly(7, 31), TimeOnly.FromDateTime(prochain.LocalDateTime));
    }

    [Fact]
    public async Task Muet_deux_fois_la_note_part_une_seule_fois()
    {
        _redacteur.Texte = null;
        var service = Service();
        await service.PasserAsync(A(6, 31), CancellationToken.None);
        await service.PasserAsync(A(7), CancellationToken.None);   // trop tôt : on attend
        Assert.Empty(_envoyeur.Envoyes);
        Assert.Equal(1, _redacteur.Appels);

        var prochain = await service.PasserAsync(A(7, 32), CancellationToken.None);
        Assert.Equal(2, _redacteur.Appels);
        var envoye = Assert.Single(_envoyeur.Envoyes);
        Assert.Equal(SourceLettre.Gabarit, Lettre()!.Source);
        Assert.Equal("Rien à faire aujourd'hui", envoye.Sujet);
        Assert.Equal(Aujourdhui.AddDays(1), DateOnly.FromDateTime(prochain.LocalDateTime));

        await service.PasserAsync(A(9), CancellationToken.None);
        Assert.Equal(2, _redacteur.Appels);
        Assert.Single(_envoyeur.Envoyes);
    }

    [Fact]
    public async Task Le_second_essai_qui_reussit_envoie_la_prose()
    {
        _redacteur.Texte = null;
        var service = Service();
        await service.PasserAsync(A(6, 31), CancellationToken.None);
        _redacteur.Texte = Prose;
        await service.PasserAsync(A(7, 32), CancellationToken.None);

        var envoye = Assert.Single(_envoyeur.Envoyes);
        Assert.Equal(Prose.Sujet, envoye.Sujet);
        Assert.Equal(SourceLettre.Llm, Lettre()!.Source);
    }

    [Fact]
    public async Task Muet_a_onze_heures_quarante_la_note_part_tout_de_suite()
    {
        _redacteur.Texte = null;
        await Service().PasserAsync(A(11, 40), CancellationToken.None);

        Assert.Single(_envoyeur.Envoyes);
        Assert.Equal(1, _redacteur.Appels);
        Assert.True(Lettre()!.Envoyee);
    }

    [Fact]
    public async Task Sans_cle_la_note_est_le_fonctionnement_normal()
    {
        _redacteur.PeutEcrire = false;
        await Service().PasserAsync(A(6, 31), CancellationToken.None);

        var envoye = Assert.Single(_envoyeur.Envoyes);
        Assert.Equal(SourceLettre.Gabarit, Lettre()!.Source);
        Assert.Contains("Rien n'est dû aujourd'hui.", envoye.Texte);
    }

    [Fact]
    public async Task Sans_adresse_nulle_part_la_lettre_est_composee_mais_ne_part_pas()
    {
        var alain = Db.Utilisateurs.Single(u => u.Id == _alain);
        alain.Courriel = null;
        Db.SaveChanges();
        _redacteur.Texte = Prose;

        var prochain = await Service().PasserAsync(A(6, 31), CancellationToken.None);
        Assert.Empty(_envoyeur.Envoyes);
        Assert.False(Lettre()!.Envoyee);
        Assert.Equal(Aujourdhui.AddDays(1), DateOnly.FromDateTime(prochain.LocalDateTime));
    }

    [Fact]
    public async Task Un_smtp_qui_refuse_ne_marque_rien_et_repasse_un_quart_d_heure_plus_tard()
    {
        _redacteur.Texte = Prose;
        _envoyeur.Echouer = true;
        var service = Service();
        var prochain = await service.PasserAsync(A(6, 31), CancellationToken.None);
        Assert.False(Lettre()!.Envoyee);
        Assert.Equal(new TimeOnly(6, 46), TimeOnly.FromDateTime(prochain.LocalDateTime));

        _envoyeur.Echouer = false;
        await service.PasserAsync(A(6, 46), CancellationToken.None);
        Assert.Single(_envoyeur.Envoyes);
        Assert.Equal(1, _redacteur.Appels);
    }

    [Fact]
    public async Task Sans_smtp_la_lettre_est_composee_et_lisible_mais_ne_part_pas()
    {
        _envoyeur.Actif = false;
        _redacteur.Texte = Prose;
        await Service().PasserAsync(A(6, 31), CancellationToken.None);
        Assert.Empty(_envoyeur.Envoyes);
        Assert.Equal(Prose.Sujet, Lettre()!.Sujet);
        Assert.False(Lettre()!.Envoyee);
    }

    [Fact]
    public async Task La_matiere_porte_la_semaine_devant_les_faites_et_la_serie()
    {
        var tache = new Tache { Id = Guid.NewGuid(), Titre = "Changer les draps", CreeParId = _alain, CreeLe = DateTimeOffset.UtcNow };
        var autre = new Tache { Id = Guid.NewGuid(), Titre = "Vétérinaire", CreeParId = _alain, CreeLe = DateTimeOffset.UtcNow };
        var loin = new Tache { Id = Guid.NewGuid(), Titre = "Ramoner", CreeParId = _alain, CreeLe = DateTimeOffset.UtcNow };
        Db.Taches.AddRange(tache, autre, loin);
        Db.Occurrences.Add(new Occurrence { Id = Guid.NewGuid(), TacheId = tache.Id, Echeance = Aujourdhui });
        Db.Occurrences.Add(new Occurrence { Id = Guid.NewGuid(), TacheId = autre.Id, Echeance = Aujourdhui.AddDays(3), AssigneAId = _ariane });
        // À neuf jours : hors de la semaine devant.
        Db.Occurrences.Add(new Occurrence { Id = Guid.NewGuid(), TacheId = loin.Id, Echeance = Aujourdhui.AddDays(9) });
        foreach (var semaines in new[] { 1, 2, 3 })
        {
            var quand = Aujourdhui.AddDays(-7 * semaines).ToDateTime(new TimeOnly(10, 0));
            Db.Journal.Add(new EntreeJournal
            {
                Id = Guid.NewGuid(), TacheId = tache.Id, OccurrenceId = Guid.NewGuid(), UtilisateurId = _alain,
                CompleteeLe = new DateTimeOffset(quand),
            });
        }
        Db.Journal.Add(new EntreeJournal
        {
            Id = Guid.NewGuid(), TacheId = autre.Id, OccurrenceId = Guid.NewGuid(), UtilisateurId = _ariane,
            CompleteeLe = new DateTimeOffset(A(6, 0)).AddHours(-3),
        });
        Db.SaveChanges();
        _redacteur.Texte = Prose;

        await Service().PasserAsync(A(6, 31), CancellationToken.None);

        var matiere = _redacteur.DerniereMatiere!;
        Assert.Equal(3, matiere.Serie("Changer les draps"));
        var devant = Assert.Single(matiere.SemaineDevant);
        Assert.Equal(("Vétérinaire", "Ariane"), (devant.Titre, devant.Assigne));
        var faite = Assert.Single(matiere.FaitesDepuisLaDerniere);
        Assert.Equal(("Vétérinaire", "Ariane"), (faite.Titre, faite.Par));
        Assert.Contains("\"serie\":3", Lettre()!.Matiere);
    }

    [Fact]
    public async Task La_memoire_ne_retient_que_les_lettres_parties()
    {
        Db.Lettres.AddRange(
            new LettreDuMatin { Date = Aujourdhui.AddDays(-1), Sujet = "Hier", Paragraphes = [new string('h', 200)], Source = SourceLettre.Llm, ComposeeLe = DateTimeOffset.UtcNow, EnvoyeeLe = DateTimeOffset.UtcNow },
            new LettreDuMatin { Date = Aujourdhui.AddDays(-2), Sujet = "Muette", Paragraphes = ["m"], Source = SourceLettre.Gabarit, ComposeeLe = DateTimeOffset.UtcNow },
            new LettreDuMatin { Date = Aujourdhui.AddDays(-9), Sujet = "Trop vieille", Paragraphes = ["v"], Source = SourceLettre.Llm, ComposeeLe = DateTimeOffset.UtcNow, EnvoyeeLe = DateTimeOffset.UtcNow });
        Db.SaveChanges();
        _redacteur.Texte = Prose;

        await Service().PasserAsync(A(6, 31), CancellationToken.None);

        var precedente = Assert.Single(_redacteur.DerniereMatiere!.Precedentes);
        Assert.Equal("Hier", precedente.Sujet);
        Assert.Equal(RedactionLettre.LongueurPremiereLigne + 1, precedente.PremiereLigne.Length);
    }
}
