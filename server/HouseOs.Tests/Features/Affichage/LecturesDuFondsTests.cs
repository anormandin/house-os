using HouseOs.Api.Domaine;
using HouseOs.Api.Features.Affichage;
using HouseOs.Tests.Features.Taches;

namespace HouseOs.Tests.Features.Affichage;

/// <summary>
/// Les lectures qui alimentent les familles « la maison » et « le calendrier ». Les
/// faits eux-mêmes se testent sans base (vault :
/// D-2026-09-20 Fonds De Tiroir Séparé Du Journal) ; ce qui se teste ici, c'est ce que
/// la base rend — et c'est là que les erreurs de jointure se cachent.
/// </summary>
public class LecturesDuFondsTests : TestAvecSqlite
{
    private static readonly DateTime Maintenant = new(2026, 9, 20, 14, 30, 0);
    private static readonly DateOnly Aujourdhui = new(2026, 9, 20);
    private readonly Guid _utilisateur = Guid.NewGuid();

    public LecturesDuFondsTests()
    {
        Db.Utilisateurs.Add(new Utilisateur
        {
            Id = _utilisateur,
            NomUtilisateur = "alain",
            NomAffichage = "Alain",
            MotDePasseHash = "x",
        });
        Db.SaveChanges();
    }

    /// <summary>Une complétion consignée à dix heures du matin, le jour dit.</summary>
    private void Consigner(Guid tacheId, DateOnly jour, decimal? cout = null) =>
        Db.Journal.Add(new EntreeJournal
        {
            Id = Guid.NewGuid(),
            TacheId = tacheId,
            OccurrenceId = Guid.NewGuid(),
            UtilisateurId = _utilisateur,
            CompleteeLe = new DateTimeOffset(jour.ToDateTime(new TimeOnly(10, 0))),
            Cout = cout,
        });

    private Guid AjouterTache(string titre, Guid? zoneId = null, DateOnly? echeance = null)
    {
        var tache = new Tache { Id = Guid.NewGuid(), Titre = titre, ZoneId = zoneId, CreeLe = new DateTimeOffset(Maintenant) };
        Db.Taches.Add(tache);
        if (echeance is { } date)
        {
            Db.Occurrences.Add(new Occurrence { Id = Guid.NewGuid(), TacheId = tache.Id, Echeance = date });
        }
        return tache.Id;
    }

    private async Task<IReadOnlyList<FaitEcranDto>> Faits() =>
        (await ComposerDonneesEcran.LireAsync(Db, Maintenant)).Faits;

    /// <summary>
    /// Le journal de complétion <b>survit à la suppression d'une tâche</b> : le
    /// HouseOsDbContext le dit en toutes lettres (« pas de FK vers Tache/Occurrence,
    /// les ids restent comme références historiques »). Une jointure interne perdrait
    /// ces entrées <b>en silence</b>, et le mur afficherait une série fausse sans que
    /// rien ne le signale. C'est le genre d'erreur qu'aucun test du fonds ne peut voir,
    /// parce qu'elle est dans la lecture et non dans la règle.
    /// </summary>
    [Fact]
    public async Task Une_completion_dont_la_tache_a_ete_effacee_compte_encore_dans_la_serie()
    {
        var vivante = AjouterTache("Boîtes!");
        var effacee = Guid.NewGuid(); // aucune tâche de cet id : elle a été supprimée.

        // Six jours d'affilée, terminés hier — mais les trois derniers appartiennent à
        // une tâche qui n'existe plus. En jointure interne, la série tomberait à zéro.
        foreach (var age in new[] { 6, 5, 4 })
        {
            Consigner(vivante, Aujourdhui.AddDays(-age));
        }
        foreach (var age in new[] { 3, 2, 1 })
        {
            Consigner(effacee, Aujourdhui.AddDays(-age));
        }
        await Db.SaveChangesAsync();

        var serie = Assert.Single(await Faits(), f => f.Cle == "maison.serie" || f.Cle == "maison.record");
        Assert.Equal("6 jours d'affilée", serie.Valeur);
    }

    [Fact]
    public async Task Les_seances_ne_nomment_que_les_taches_qui_existent_encore()
    {
        var vivante = AjouterTache("Boîtes!");
        var effacee = Guid.NewGuid();
        // La tâche effacée mène au compte, mais elle n'a plus de nom : elle ne peut pas
        // faire « 12 séances de quoi ? ».
        for (var i = 0; i < 12; i++)
        {
            Consigner(effacee, Aujourdhui.AddDays(-i));
        }
        for (var i = 0; i < 10; i++)
        {
            Consigner(vivante, Aujourdhui.AddDays(-i));
        }
        await Db.SaveChangesAsync();

        var seances = Assert.Single(await Faits(), f => f.Cle == "maison.seances");
        Assert.Equal("10 séances", seances.Valeur);
        Assert.Equal("Toutes pour la même tâche, Boîtes.", seances.Texte);
    }

    [Fact]
    public async Task La_zone_negligee_se_lit_sur_les_taches_de_la_zone_et_non_sur_la_zone_seule()
    {
        var vide = new Zone { Id = Guid.NewGuid(), Nom = "Salon" };
        var habitee = new Zone { Id = Guid.NewGuid(), Nom = "Sous-sol" };
        Db.Zones.AddRange(vide, habitee);
        var tache = AjouterTache("Nettoyer le bassin", habitee.Id);
        Consigner(tache, Aujourdhui.AddDays(-94));
        await Db.SaveChangesAsync();

        var oubliee = Assert.Single(await Faits(), f => f.Cle == "maison.piece-oubliee");
        // Le Salon n'a aucune tâche : il est vide, pas négligé, et on ne le nomme pas.
        Assert.Equal("94 jours", oubliee.Valeur);
        Assert.Equal("Sous-sol : rien de coché depuis le 18 juin.", oubliee.Texte);
    }

    [Fact]
    public async Task Le_cout_de_l_annee_ne_compte_que_l_annee_civile_en_cours()
    {
        var tache = AjouterTache("Plomberie");
        Consigner(tache, new DateOnly(2025, 11, 3), 900m);
        Consigner(tache, new DateOnly(2026, 3, 9), 528.89m);
        Consigner(tache, new DateOnly(2026, 7, 6), 1217.10m);
        Consigner(tache, new DateOnly(2026, 8, 1));
        await Db.SaveChangesAsync();

        var cout = Assert.Single(await Faits(), f => f.Cle == "maison.cout");
        Assert.Equal("Depuis le 1er janvier", cout.Etiquette);
        // 528,89 + 1 217,10, sans les 900 $ de l'an dernier ni l'entrée sans coût.
        Assert.Contains("1", cout.Valeur);
        Assert.Contains("746", cout.Valeur.Replace(" ", "").Replace(" ", "").Replace(" ", ""));
        Assert.Equal("Réparti sur 2 interventions consignées.", cout.Texte);
    }

    [Fact]
    public async Task Le_prochain_entretien_du_doyen_vient_de_ses_occurrences_ouvertes()
    {
        var equipement = new Equipement
        {
            Id = Guid.NewGuid(),
            Nom = "Chauffe-eau électrique",
            DateAchat = new DateOnly(2010, 6, 15),
        };
        Db.Equipements.Add(equipement);
        var tache = new Tache
        {
            Id = Guid.NewGuid(),
            Titre = "Vidanger le chauffe-eau",
            EquipementId = equipement.Id,
            CreeLe = new DateTimeOffset(Maintenant),
        };
        Db.Taches.Add(tache);
        // Une échéance passée et une à venir : c'est la plus proche encore ouverte qui
        // se dit, jamais celle qu'on a déjà manquée.
        Db.Occurrences.Add(new Occurrence
        {
            Id = Guid.NewGuid(), TacheId = tache.Id, Echeance = new DateOnly(2026, 3, 1),
            Statut = StatutOccurrence.Completee, CompleteeLe = new DateTimeOffset(Maintenant),
        });
        Db.Occurrences.Add(new Occurrence { Id = Guid.NewGuid(), TacheId = tache.Id, Echeance = new DateOnly(2026, 10, 31) });
        await Db.SaveChangesAsync();

        var doyen = Assert.Single(await Faits(), f => f.Cle == "maison.doyen");
        Assert.Equal("16 ans", doyen.Valeur);
        Assert.Equal("Chauffe-eau électrique, dont le prochain entretien est le 31 octobre.", doyen.Texte);
    }

    [Fact]
    public async Task Le_calendrier_lit_les_comptes_les_echeances_les_fenetres_et_les_papiers()
    {
        Db.ComptesARebours.Add(new CompteARebours
        {
            Id = Guid.NewGuid(), Titre = "Déménagement", DateCible = new DateOnly(2026, 10, 6),
        });
        // Deux échéances dans la fenêtre 7–30 jours, une trop proche pour y entrer.
        AjouterTache("Changer le filtre", echeance: Aujourdhui.AddDays(11));
        AjouterTache("Lubrifier les coupe-froid", echeance: Aujourdhui.AddDays(25));
        AjouterTache("Signer chez le notaire", echeance: Aujourdhui.AddDays(3));

        // La fenêtre saisonnière du moteur, exposée comme deux dates.
        var saisonniere = new Tache
        {
            Id = Guid.NewGuid(),
            Titre = "Rentrer les boyaux",
            CreeLe = new DateTimeOffset(Maintenant),
            Recurrence = new SpecRecurrence
            {
                Mode = ModeRecurrence.Fixe,
                FixeType = TypeFixe.Annuelle,
                MoisAnnuel = 8,
                JourAnnuel = 1,
                FenetreDebutMois = 8,
                FenetreDebutJour = 1,
                FenetreFinMois = 9,
                FenetreFinJour = 29,
            },
        };
        Db.Taches.Add(saisonniere);

        Db.Documents.Add(new Document
        {
            Id = Guid.NewGuid(),
            Titre = "Attestation d'assurance habitation",
            NomFichier = "a.pdf",
            CheminDisque = "a.pdf",
            TypeMime = "application/pdf",
            Echeance = Aujourdhui.AddDays(23),
            CreeLe = new DateTimeOffset(Maintenant),
        });
        await Db.SaveChangesAsync();

        var faits = await Faits();

        var compte = Assert.Single(faits, f => f.Cle == "calendrier.compte-a-rebours");
        Assert.Equal("Déménagement", compte.Etiquette);
        Assert.Equal("Dans 16 jours", compte.Valeur);

        var vient = Assert.Single(faits, f => f.Cle == "calendrier.ca-s-en-vient");
        Assert.Equal("2 échéances", vient.Valeur);
        Assert.Equal("La première le 1er octobre : Changer le filtre.", vient.Texte);

        var saison = Assert.Single(faits, f => f.Cle == "calendrier.saison");
        Assert.Equal("La saison se ferme", saison.Etiquette);
        Assert.Equal("Il reste 9 jours", saison.Valeur);

        var papier = Assert.Single(faits, f => f.Cle == "calendrier.expiration");
        Assert.Equal("Un papier expire", papier.Etiquette);
        Assert.Equal("Dans 23 jours", papier.Valeur);
    }

    [Fact]
    public async Task Une_installation_neuve_sort_sans_le_moindre_fait_et_sans_tomber()
    {
        // Ni tâche, ni journal, ni équipement, ni compte à rebours : chaque famille se
        // tait, et la composition tient debout. C'est le premier jour d'un foyer qui
        // vient d'installer House OS.
        Assert.Empty(await Faits());
    }
}
