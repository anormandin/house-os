using HouseOs.Api.Domaine;
using HouseOs.Api.Features.Mcp;
using HouseOs.Api.Features.Taches;
using HouseOs.Api.Features.Synchro;
using HouseOs.Tests.Features.Synchro;
using HouseOs.Tests.Features.Taches;
using ModelContextProtocol;
using Microsoft.Extensions.Logging.Abstractions;

namespace HouseOs.Tests.Features.Mcp;

/// <summary>
/// Les outils MCP tâches exécutés pour vrai (harnais Sqlite) : atomicité des lots,
/// garde-fous du modifier, tolérance des actions — le client est un LLM, chaque
/// refus doit être actionnable et aucune fiche partielle ne doit détruire de données.
/// </summary>
public class OutilsTachesTests : TestAvecSqlite
{
    private readonly Utilisateur _alain;
    private DateOnly Aujourdhui => DateOnly.FromDateTime(DateTime.Now);

    /// <summary>Les outils diffusent leurs gestes ; ce mouchard les recueille.</summary>
    protected DiffuseurMouchard Mouchard { get; } = new();

    public OutilsTachesTests()
    {
        _alain = new Utilisateur
        {
            Id = Guid.NewGuid(),
            NomUtilisateur = "alain",
            NomAffichage = "Alain",
            MotDePasseHash = "x",
        };
        Db.Utilisateurs.Add(_alain);
        Db.SaveChanges();
    }

    private static TacheAPlanifier Item(
        string titre = "Tâche",
        string? echeance = null,
        string? assigneA = null,
        Guid? zoneId = null,
        RecurrenceDto? recurrence = null) =>
        new(titre, null, echeance, assigneA, zoneId, null, null, recurrence);

    private async Task<Guid> CreerUne(TacheAPlanifier item)
    {
        await OutilsTaches.CreerTaches(Db, Mouchard,"alain", [item]);
        // Tri côté client : Sqlite ne sait pas ordonner un DateTimeOffset en SQL.
        return Db.Taches.AsEnumerable().MaxBy(t => t.CreeLe)!.Id;
    }

    // --- creer_taches : atomicité tout-ou-rien ---

    [Fact]
    public async Task Un_lot_partiellement_invalide_ne_cree_rien_meme_apres_un_save_ulterieur()
    {
        var lot = new[]
        {
            Item("Valide 1"),
            Item("Échéance cassée", echeance: "2026-13-45"),
            Item("Valide 2"),
        };

        var exception = await Assert.ThrowsAsync<McpException>(
            () => OutilsTaches.CreerTaches(Db, Mouchard,"alain", lot));

        Assert.Contains("[1]", exception.Message);
        // La promesse tout-ou-rien doit survivre à un SaveChanges ultérieur du même
        // scope : sans purge du change tracker, les deux valides seraient flushées.
        await Db.SaveChangesAsync();
        Assert.Empty(Db.Taches);
    }

    [Fact]
    public async Task Les_erreurs_d_un_lot_sont_listees_par_index()
    {
        var lot = new[]
        {
            Item("", echeance: null),                       // titre vide
            Item("Correcte"),
            Item("Assigné fantôme", assigneA: "gertrude"),  // inconnu
        };

        var exception = await Assert.ThrowsAsync<McpException>(
            () => OutilsTaches.CreerTaches(Db, Mouchard,"alain", lot));

        Assert.Contains("[0]", exception.Message);
        Assert.Contains("[2]", exception.Message);
        Assert.DoesNotContain("[1]", exception.Message);
    }

    [Fact]
    public async Task Un_lot_avec_zone_supprimee_est_refuse_avec_un_message_actionnable()
    {
        var exception = await Assert.ThrowsAsync<McpException>(
            () => OutilsTaches.CreerTaches(Db, Mouchard,"alain", [Item(zoneId: Guid.NewGuid())]));

        Assert.Contains("lister_zones", exception.Message);
    }

    // --- gerer_tache : garde-fous du modifier ---

    [Fact]
    public async Task Modifier_une_recurrente_sans_champ_recurrence_est_refuse()
    {
        var id = await CreerUne(Item("Hebdo", recurrence: new RecurrenceDto(
            "Fixe", "JoursSemaine", [1, 3, 5], null, null, null, null,
            null, null, null, null, true)));

        var exception = await Assert.ThrowsAsync<McpException>(() =>
            OutilsTaches.GererTache(Db, "modifier", id, Item("Hebdo renommée")));

        // Une fiche partielle (« change juste le titre ») aurait détruit l'horaire.
        Assert.Contains("recurrence", exception.Message);
        Assert.Equal(ModeRecurrence.Fixe, Db.Taches.Single().Recurrence.Mode);
    }

    [Fact]
    public async Task Modifier_sans_echeance_conserve_celle_de_l_occurrence_en_attente()
    {
        var echeance = Aujourdhui.AddDays(3);
        var id = await CreerUne(Item("Ponctuelle", echeance: echeance.ToString("yyyy-MM-dd")));

        await OutilsTaches.GererTache(Db, "modifier", id, Item("Ponctuelle renommée"));

        var occurrence = Db.Occurrences.Single(o => o.Statut == StatutOccurrence.EnAttente);
        Assert.Equal(echeance, occurrence.Echeance);
        Assert.Equal("Ponctuelle renommée", Db.Taches.Single().Titre);
    }

    [Fact]
    public async Task Modifier_avec_echeance_aucune_l_efface_explicitement()
    {
        var id = await CreerUne(Item("Ponctuelle", echeance: Aujourdhui.ToString("yyyy-MM-dd")));

        await OutilsTaches.GererTache(Db, "modifier", id, Item("Ponctuelle", echeance: "aucune"));

        Assert.Null(Db.Occurrences.Single(o => o.Statut == StatutOccurrence.EnAttente).Echeance);
    }

    [Fact]
    public async Task L_action_est_acceptee_quelle_qu_en_soit_la_casse()
    {
        var id = await CreerUne(Item("Casse"));

        var dto = await OutilsTaches.GererTache(Db, " OBTENIR ", id);

        Assert.IsType<TacheDto>(dto);
    }

    [Fact]
    public async Task Une_action_inconnue_enumere_les_actions_valides()
    {
        var exception = await Assert.ThrowsAsync<McpException>(() =>
            OutilsTaches.GererTache(Db, "archiver", Guid.NewGuid()));

        Assert.Contains("obtenir", exception.Message);
        Assert.Contains("modifier", exception.Message);
        Assert.Contains("supprimer", exception.Message);
    }

    // --- lister_occurrences / gerer_occurrence : paramètres ---

    [Fact]
    public async Task Un_filtre_inconnu_est_refuse_au_lieu_de_tout_retourner()
    {
        var exception = await Assert.ThrowsAsync<McpException>(() =>
            OutilsTaches.ListerOccurrences(Db, filtre: "aujourd'hui"));

        Assert.Contains("Filtre inconnu", exception.Message);
        Assert.Contains("aujourdhui", exception.Message);
    }

    [Fact]
    public async Task Passer_sans_agirComme_est_refuse_avec_un_message_explicite()
    {
        var exception = await Assert.ThrowsAsync<McpException>(() =>
            OutilsTaches.GererOccurrence(Db, Mouchard,"passer", Guid.NewGuid()));

        Assert.Contains("agirComme", exception.Message);
    }

    [Fact]
    public async Task Reporter_sans_echeance_est_refuse()
    {
        var exception = await Assert.ThrowsAsync<McpException>(() =>
            OutilsTaches.GererOccurrence(Db, Mouchard,"reporter", Guid.NewGuid(), agirComme: "alain"));

        Assert.Contains("echeance", exception.Message);
    }

    [Fact]
    public async Task Completer_une_occurrence_d_une_tache_supprimee_repond_introuvable()
    {
        var id = await CreerUne(Item("Éphémère"));
        var occurrenceId = Db.Occurrences.Single().Id;
        await OutilsTaches.GererTache(Db, "supprimer", id);

        var exception = await Assert.ThrowsAsync<McpException>(() =>
            OutilsTaches.CompleterOccurrence(Db, Mouchard, NullLoggerFactory.Instance, occurrenceId, "alain"));

        Assert.Contains("introuvable", exception.Message);
    }

    [Fact]
    public async Task Lister_taches_retourne_les_definitions_avec_echeance_en_attente()
    {
        var id = await CreerUne(Item(
            "Tondre", assigneA: "alain",
            // Rollover null : le flag est réservé au mode fixe (T9, issue #53).
            recurrence: new RecurrenceDto("Intervalle", null, null, null, null, null, 7,
                null, null, null, null, null)));

        var resumes = await OutilsTaches.ListerTaches(Db);

        var resume = Assert.Single(resumes);
        Assert.Equal(id, resume.Id);
        Assert.Equal("Intervalle", resume.Recurrence.Mode);
        // Sans échéance de départ, la première occurrence tombe à aujourd'hui + intervalle.
        Assert.Equal(Aujourdhui.AddDays(7), resume.Echeance);
        Assert.Equal("alain", resume.AssigneA?.NomUtilisateur);
        Assert.False(resume.Completee);
    }

    [Fact]
    public async Task AgirComme_inconnu_enumere_les_noms_valides()
    {
        var exception = await Assert.ThrowsAsync<McpException>(() =>
            OutilsTaches.CreerTaches(Db, Mouchard,"gertrude", [Item()]));

        Assert.Contains("alain", exception.Message);
    }

    [Fact]
    public async Task UnLotDeDixTaches_neDiffuseQuUnSeulEvenementCompte()
    {
        // Le point du lot : dix tâches poussées d'un coup doivent faire une annonce
        // (« Claude a créé 10 tâches »), pas dix toasts empilés.
        var lot = Enumerable.Range(1, 10).Select(i => Item($"Tâche {i}")).ToArray();

        await OutilsTaches.CreerTaches(Db, Mouchard, "alain", lot);

        var evenement = Assert.Single(Mouchard.Fins);
        Assert.Equal(EvenementSynchro.GenreTachesCreees, evenement.Genre);
        Assert.Equal(EvenementSynchro.SourceMcp, evenement.Source);
        Assert.Equal(10, evenement.Nombre);
        Assert.Equal("Alain", evenement.ActeurNom);
        // Au pluriel, aucun titre : le libellé affiché sera le décompte.
        Assert.Null(evenement.Libelle);
    }

    [Fact]
    public async Task UnLotDUneSeuleTache_porteSonTitre()
    {
        await OutilsTaches.CreerTaches(Db, Mouchard, "alain", [Item("Changer le filtre")]);

        var evenement = Assert.Single(Mouchard.Fins);
        Assert.Equal(1, evenement.Nombre);
        Assert.Equal("Changer le filtre", evenement.Libelle);
    }

    [Fact]
    public async Task UnLotRefuse_neDiffuseRien()
    {
        // Tout-ou-rien : rien n'est créé, donc rien à annoncer.
        var lot = new[] { Item("Bonne"), Item(zoneId: Guid.NewGuid()) };

        await Assert.ThrowsAsync<McpException>(() =>
            OutilsTaches.CreerTaches(Db, Mouchard, "alain", lot));

        Assert.Empty(Mouchard.Recus);
    }

    [Fact]
    public async Task CompleterViaMcp_diffuseLaSourceMcp()
    {
        // Sans la source, une complétion faite par Claude « au nom d'Alain » serait
        // muette dans l'onglet d'Alain — le scénario que la synchro doit couvrir.
        var tacheId = await CreerUne(Item("Litière", echeance: Aujourdhui.ToString("yyyy-MM-dd")));
        var occurrence = Db.Occurrences.Single(o => o.TacheId == tacheId);
        Mouchard.Recus.Clear();

        await OutilsTaches.CompleterOccurrence(Db, Mouchard, NullLoggerFactory.Instance, occurrence.Id, "alain");

        var evenement = Assert.Single(Mouchard.Fins);
        Assert.Equal(EvenementSynchro.GenreOccurrenceCompletee, evenement.Genre);
        Assert.Equal(EvenementSynchro.SourceMcp, evenement.Source);
        Assert.Equal("Alain", evenement.ActeurNom);
        Assert.Equal("Litière", evenement.Libelle);
    }
}
