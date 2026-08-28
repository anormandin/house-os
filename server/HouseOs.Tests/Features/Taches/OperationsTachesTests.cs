using HouseOs.Api.Domaine;
using HouseOs.Api.Features.Taches;

namespace HouseOs.Tests.Features.Taches;

public class OperationsTachesTests : TestAvecSqlite
{
    private readonly Utilisateur _alain;
    private readonly Utilisateur _ariane;
    private readonly DateTimeOffset _maintenant = new(2026, 9, 15, 14, 0, 0, TimeSpan.Zero);
    private DateOnly Aujourdhui => DateOnly.FromDateTime(_maintenant.LocalDateTime);

    public OperationsTachesTests()
    {
        _alain = new Utilisateur
        {
            Id = Guid.NewGuid(),
            NomUtilisateur = "alain",
            NomAffichage = "Alain",
            MotDePasseHash = "x",
        };
        _ariane = new Utilisateur
        {
            Id = Guid.NewGuid(),
            NomUtilisateur = "ariane",
            NomAffichage = "Ariane",
            MotDePasseHash = "x",
        };
        Db.Utilisateurs.Add(_alain);
        Db.Utilisateurs.Add(_ariane);
        Db.SaveChanges();
    }

    private Tache CreerIntervalle(int jours = 7, StrategieAssignation strategie = StrategieAssignation.Fixe)
    {
        var spec = new SpecRecurrence { Mode = ModeRecurrence.Intervalle, IntervalleJours = jours };
        var tache = Tache.CreerRecurrente(
            "Tondre", null, spec, strategie, _alain.Id,
            Aujourdhui, _alain.Id, _maintenant, Aujourdhui);
        Db.Taches.Add(tache);
        Db.SaveChanges();
        return tache;
    }

    private Tache CreerPonctuelle(DateOnly? echeance = null, bool avecEcheance = true)
    {
        var tache = Tache.CreerPonctuelle(
            "Changer l'adresse", null, avecEcheance ? (echeance ?? Aujourdhui) : null,
            _alain.Id, _alain.Id, _maintenant);
        Db.Taches.Add(tache);
        Db.SaveChanges();
        return tache;
    }

    private Tache CreerHebdoLundi()
    {
        var spec = new SpecRecurrence
        {
            Mode = ModeRecurrence.Fixe,
            FixeType = TypeFixe.JoursSemaine,
            JoursSemaineMasque = SpecRecurrence.MasqueDe(DayOfWeek.Monday),
        };
        var tache = Tache.CreerRecurrente(
            "Poubelles", null, spec, StrategieAssignation.Fixe, _alain.Id,
            Aujourdhui, _alain.Id, _maintenant, Aujourdhui);
        Db.Taches.Add(tache);
        Db.SaveChanges();
        return tache;
    }

    // Rollover null : le flag est réservé au mode fixe (T9, issue #53).
    private static RecurrenceDto RecIntervalle(int jours = 7) =>
        new("Intervalle", null, null, null, null, null, jours, null, null, null, null, null);

    private static ModifierTacheRequete Requete(
        string titre = "Tondre",
        DateOnly? echeance = null,
        Guid? assigneAId = null,
        string? strategie = null,
        RecurrenceDto? recurrence = null) =>
        new(titre, null, echeance, assigneAId, null, null, strategie, recurrence);

    private Occurrence EnAttenteDe(Tache tache) =>
        Db.Occurrences.Single(o => o.TacheId == tache.Id && o.Statut == StatutOccurrence.EnAttente);

    [Fact]
    public async Task Bilan_retourne_les_completions_de_la_fenetre_seulement()
    {
        var tache = CreerIntervalle(jours: 7);
        var premiere = EnAttenteDe(tache);
        await OperationsTaches.CompleterAsync(Db, premiere.Id, _alain.Id, null, _maintenant);
        var suivante = EnAttenteDe(tache);
        await OperationsTaches.CompleterAsync(Db, suivante.Id, _alain.Id, null, _maintenant.AddDays(14));

        var instants = await OperationsTaches.BilanCompletionsAsync(
            Db, _maintenant.AddDays(-1), _maintenant.AddDays(7));

        Assert.Equal([_maintenant], instants);
    }

    [Fact]
    public async Task Annuler_la_derniere_completion_restaure_tout()
    {
        var tache = CreerIntervalle();
        var occurrence = EnAttenteDe(tache);
        await OperationsTaches.CompleterAsync(Db, occurrence.Id, _alain.Id, null, _maintenant);

        var statut = await OperationsTaches.AnnulerCompletionAsync(Db, occurrence.Id);

        Assert.Equal(StatutAnnulation.Ok, statut);
        Assert.Equal(StatutOccurrence.EnAttente, occurrence.Statut);
        Assert.Null(occurrence.CompleteeParId);
        Assert.Equal(Aujourdhui, occurrence.Echeance);
        Assert.Empty(Db.Journal);
        // La suivante matérialisée a été supprimée : une seule occurrence reste.
        Assert.Single(Db.Occurrences.Where(o => o.TacheId == tache.Id));
    }

    [Fact]
    public async Task Annuler_une_completion_plus_ancienne_est_refuse()
    {
        var tache = CreerIntervalle();
        var premiere = EnAttenteDe(tache);
        await OperationsTaches.CompleterAsync(Db, premiere.Id, _alain.Id, null, _maintenant);
        var suivante = EnAttenteDe(tache);
        await OperationsTaches.CompleterAsync(Db, suivante.Id, _alain.Id, null, _maintenant.AddDays(7));

        var statut = await OperationsTaches.AnnulerCompletionAsync(Db, premiere.Id);

        Assert.Equal(StatutAnnulation.PasLaDerniere, statut);
        Assert.Equal(StatutOccurrence.Completee, premiere.Statut);
    }

    [Fact]
    public async Task Annuler_une_ponctuelle_ne_supprime_aucune_autre_occurrence()
    {
        var tache = CreerPonctuelle();
        var occurrence = EnAttenteDe(tache);
        await OperationsTaches.CompleterAsync(Db, occurrence.Id, _alain.Id, null, _maintenant);

        var statut = await OperationsTaches.AnnulerCompletionAsync(Db, occurrence.Id);

        Assert.Equal(StatutAnnulation.Ok, statut);
        Assert.Equal(StatutOccurrence.EnAttente, occurrence.Statut);
        Assert.Empty(Db.Journal);
        Assert.Single(Db.Occurrences.Where(o => o.TacheId == tache.Id));
    }

    [Fact]
    public async Task Annuler_une_occurrence_en_attente_est_refuse()
    {
        var tache = CreerIntervalle();
        var occurrence = EnAttenteDe(tache);

        var statut = await OperationsTaches.AnnulerCompletionAsync(Db, occurrence.Id);

        Assert.Equal(StatutAnnulation.PasCompletee, statut);
    }

    [Fact]
    public async Task Passer_materialise_la_suivante_sans_journal()
    {
        var tache = CreerIntervalle(jours: 7);
        var occurrence = EnAttenteDe(tache);

        var resultat = await OperationsTaches.PasserAsync(Db, occurrence.Id, _alain.Id, _maintenant);

        Assert.Equal(StatutPasse.Ok, resultat.Statut);
        Assert.Equal(StatutOccurrence.Passee, occurrence.Statut);
        Assert.Equal(_maintenant, occurrence.PasseeLe);
        Assert.Empty(Db.Journal);
        Assert.NotNull(resultat.Prochaine);
        Assert.Equal(Aujourdhui.AddDays(7), resultat.Prochaine!.Echeance);
        Assert.Equal(resultat.Prochaine.Id, EnAttenteDe(tache).Id);
    }

    [Fact]
    public async Task Passer_une_ponctuelle_est_refuse()
    {
        var tache = CreerPonctuelle();
        var occurrence = EnAttenteDe(tache);

        var resultat = await OperationsTaches.PasserAsync(Db, occurrence.Id, _alain.Id, _maintenant);

        Assert.Equal(StatutPasse.TachePonctuelle, resultat.Statut);
        Assert.Equal(StatutOccurrence.EnAttente, occurrence.Statut);
    }

    [Fact]
    public async Task Completer_annuler_recompleter_preserve_une_seule_en_attente()
    {
        var tache = CreerIntervalle();
        var occurrence = EnAttenteDe(tache);

        await OperationsTaches.CompleterAsync(Db, occurrence.Id, _alain.Id, null, _maintenant);
        Assert.Single(Db.Occurrences.Where(
            o => o.TacheId == tache.Id && o.Statut == StatutOccurrence.EnAttente));

        await OperationsTaches.AnnulerCompletionAsync(Db, occurrence.Id);
        Assert.Equal(occurrence.Id, EnAttenteDe(tache).Id);

        var resultat = await OperationsTaches.CompleterAsync(
            Db, occurrence.Id, _alain.Id, null, _maintenant.AddHours(1));
        Assert.Equal(StatutCompletion.Ok, resultat.Statut);
        Assert.Single(Db.Occurrences.Where(
            o => o.TacheId == tache.Id && o.Statut == StatutOccurrence.EnAttente));
        Assert.Single(Db.Journal);
    }

    [Fact]
    public async Task Reporter_glisse_l_echeance_et_refuse_le_passe()
    {
        var tache = CreerIntervalle();
        var occurrence = EnAttenteDe(tache);

        var refus = await OperationsTaches.ReporterAsync(
            Db, occurrence.Id, Aujourdhui.AddDays(-1), Aujourdhui);
        Assert.Equal(StatutReport.DateInvalide, refus);

        var statut = await OperationsTaches.ReporterAsync(
            Db, occurrence.Id, Aujourdhui.AddDays(3), Aujourdhui);
        Assert.Equal(StatutReport.Ok, statut);
        Assert.Equal(Aujourdhui.AddDays(3), occurrence.Echeance);
    }

    [Fact]
    public async Task Ajouter_notes_met_a_jour_le_journal()
    {
        var tache = CreerIntervalle();
        var occurrence = EnAttenteDe(tache);
        await OperationsTaches.CompleterAsync(Db, occurrence.Id, _alain.Id, null, _maintenant);

        var statut = await OperationsTaches.AjouterNotesAsync(Db, occurrence.Id, "  lame affûtée  ");

        Assert.Equal(StatutNotes.Ok, statut);
        Assert.Equal("lame affûtée", Db.Journal.Single().Notes);
    }

    [Fact]
    public async Task Ajouter_notes_sur_une_occurrence_non_completee_est_refuse()
    {
        var tache = CreerIntervalle();
        var occurrence = EnAttenteDe(tache);

        var statut = await OperationsTaches.AjouterNotesAsync(Db, occurrence.Id, "trop tôt");

        Assert.Equal(StatutNotes.PasCompletee, statut);
    }

    [Fact]
    public async Task Completer_normalise_les_notes()
    {
        // Issue #33 : mêmes règles qu'AjouterNotesAsync — Trim, et blanc → null (sinon
        // le DTO expose Notes: "" que l'UI traite comme « note présente »).
        var avecNotes = CreerIntervalle();
        await OperationsTaches.CompleterAsync(
            Db, EnAttenteDe(avecNotes).Id, _alain.Id, "  lame affûtée  ", _maintenant);
        Assert.Equal("lame affûtée",
            Db.Journal.Single(j => j.TacheId == avecNotes.Id).Notes);

        var notesBlanches = CreerPonctuelle();
        await OperationsTaches.CompleterAsync(
            Db, EnAttenteDe(notesBlanches).Id, _alain.Id, "   ", _maintenant);
        Assert.Null(Db.Journal.Single(j => j.TacheId == notesBlanches.Id).Notes);
    }

    // --- Édition (PUT) : transitions de mode et réalignement de l'occurrence ---

    [Fact]
    public async Task Convertir_une_ponctuelle_completee_en_recurrente_materialise_une_occurrence()
    {
        var tache = CreerPonctuelle();
        var occurrence = EnAttenteDe(tache);
        await OperationsTaches.CompleterAsync(Db, occurrence.Id, _alain.Id, null, _maintenant);

        var erreur = await OperationsTaches.ModifierTacheAsync(
            Db, tache, Requete(recurrence: RecIntervalle(7)), Aujourdhui);
        await Db.SaveChangesAsync();

        Assert.Null(erreur);
        // Sans matérialisation, la tâche deviendrait invisible de tous les filtres.
        var enAttente = EnAttenteDe(tache);
        Assert.Equal(Aujourdhui.AddDays(7), enAttente.Echeance);
    }

    [Fact]
    public async Task Modifier_une_ponctuelle_sans_echeance_efface_l_echeance()
    {
        var tache = CreerPonctuelle();

        var erreur = await OperationsTaches.ModifierTacheAsync(
            Db, tache, Requete(echeance: null), Aujourdhui);
        await Db.SaveChangesAsync();

        // Comportement documenté au vault : le PUT remplace tout, omission = effacement —
        // c'est pour ça que tout client doit recharger l'échéance avant d'enregistrer.
        Assert.Null(erreur);
        Assert.Null(EnAttenteDe(tache).Echeance);
    }

    [Fact]
    public async Task Modifier_vers_intervalle_sans_completion_part_d_aujourdhui()
    {
        var tache = CreerPonctuelle();

        var erreur = await OperationsTaches.ModifierTacheAsync(
            Db, tache, Requete(recurrence: RecIntervalle(7)), Aujourdhui);
        await Db.SaveChangesAsync();

        Assert.Null(erreur);
        Assert.Equal(Aujourdhui.AddDays(7), EnAttenteDe(tache).Echeance);
    }

    [Fact]
    public async Task Modifier_vers_ponctuelle_prend_l_echeance_fournie_telle_quelle()
    {
        var tache = CreerIntervalle();

        var erreur = await OperationsTaches.ModifierTacheAsync(
            Db, tache, Requete(echeance: Aujourdhui.AddDays(2)), Aujourdhui);
        await Db.SaveChangesAsync();

        Assert.Null(erreur);
        Assert.Equal(ModeRecurrence.Ponctuelle, tache.Recurrence.Mode);
        Assert.Equal(Aujourdhui.AddDays(2), EnAttenteDe(tache).Echeance);
    }

    [Fact]
    public async Task Retirer_l_assigne_en_strategie_fixe_desassigne_aussi_l_occurrence()
    {
        var tache = CreerPonctuelle();
        Assert.Equal(_alain.Id, EnAttenteDe(tache).AssigneAId);

        var erreur = await OperationsTaches.ModifierTacheAsync(
            Db, tache, Requete(echeance: Aujourdhui, assigneAId: null), Aujourdhui);
        await Db.SaveChangesAsync();

        Assert.Null(erreur);
        Assert.Null(tache.AssigneAId);
        Assert.Null(EnAttenteDe(tache).AssigneAId);
    }

    [Fact]
    public async Task Modifier_en_strategie_tournante_conserve_l_assigne_de_l_occurrence()
    {
        var tache = CreerIntervalle(strategie: StrategieAssignation.Alternance);
        var occurrence = EnAttenteDe(tache);
        occurrence.AssigneAId = _ariane.Id; // choisi par la stratégie au cycle précédent
        Db.SaveChanges();

        var erreur = await OperationsTaches.ModifierTacheAsync(
            Db, tache,
            Requete(assigneAId: null, strategie: "Alternance", recurrence: RecIntervalle()),
            Aujourdhui);
        await Db.SaveChangesAsync();

        Assert.Null(erreur);
        Assert.Equal(_ariane.Id, EnAttenteDe(tache).AssigneAId);
    }

    [Fact]
    public async Task Modifier_avec_recurrence_invalide_ne_mutile_pas_la_tache()
    {
        var tache = CreerIntervalle(jours: 7);

        var erreur = await OperationsTaches.ModifierTacheAsync(
            Db, tache,
            new ModifierTacheRequete("Nouveau titre", null, null, null, null, null, null,
                new RecurrenceDto("Intervalle", null, null, null, null, null, 0,
                    null, null, null, null, null)),
            Aujourdhui);

        Assert.NotNull(erreur);
        // La validation précède toute assignation : rien n'a été touché.
        Assert.Equal("Tondre", tache.Titre);
        Assert.Equal(7, tache.Recurrence.IntervalleJours);
    }

    [Fact]
    public async Task Modifier_avec_zone_inexistante_est_refuse()
    {
        var tache = CreerPonctuelle();

        var erreur = await OperationsTaches.ModifierTacheAsync(
            Db, tache,
            new ModifierTacheRequete("Tondre", null, Aujourdhui, null, Guid.NewGuid(), null, null, null),
            Aujourdhui);

        Assert.NotNull(erreur);
        Assert.Equal("zoneId", erreur!.Champ);
    }

    [Fact]
    public async Task Creer_avec_references_inexistantes_est_refuse()
    {
        var requete = new CreerTacheRequete(
            "Tâche", null, Aujourdhui, Guid.NewGuid(), null, null, null, null);

        var (tache, erreur) = await OperationsTaches.PreparerTacheAsync(
            Db, requete, _alain.Id, _maintenant, Aujourdhui);

        Assert.Null(tache);
        Assert.Equal("assigneAId", erreur!.Champ);
    }

    [Fact]
    public async Task Creer_avec_titre_trop_long_est_refuse()
    {
        var requete = new CreerTacheRequete(
            new string('x', 201), null, Aujourdhui, null, null, null, null, null);

        var (tache, erreur) = await OperationsTaches.PreparerTacheAsync(
            Db, requete, _alain.Id, _maintenant, Aujourdhui);

        Assert.Null(tache);
        Assert.Equal("titre", erreur!.Champ);
    }

    // --- Annulation : chemins de garde ---

    [Fact]
    public async Task Annuler_apres_un_passer_est_refuse()
    {
        var tache = CreerIntervalle();
        var premiere = EnAttenteDe(tache);
        await OperationsTaches.CompleterAsync(Db, premiere.Id, _alain.Id, null, _maintenant);
        var deuxieme = EnAttenteDe(tache);
        await OperationsTaches.PasserAsync(Db, deuxieme.Id, _alain.Id, _maintenant.AddHours(1));
        var troisieme = EnAttenteDe(tache);

        var statut = await OperationsTaches.AnnulerCompletionAsync(Db, premiere.Id);

        // Annuler aurait supprimé la suivante DU PASSAGE et laissé la passée orpheline.
        Assert.Equal(StatutAnnulation.ProchaineDejaTraitee, statut);
        Assert.Equal(StatutOccurrence.Completee, premiere.Statut);
        Assert.Equal(StatutOccurrence.Passee, deuxieme.Statut);
        Assert.Equal(troisieme.Id, EnAttenteDe(tache).Id);
    }

    [Fact]
    public async Task Annuler_sans_entree_de_journal_signale_le_journal_manquant()
    {
        var tache = CreerIntervalle();
        var occurrence = EnAttenteDe(tache);
        await OperationsTaches.CompleterAsync(Db, occurrence.Id, _alain.Id, null, _maintenant);
        Db.Journal.RemoveRange(Db.Journal);
        Db.SaveChanges();

        var statut = await OperationsTaches.AnnulerCompletionAsync(Db, occurrence.Id);

        Assert.Equal(StatutAnnulation.JournalManquant, statut);
    }

    // --- Listes : filtres, tri, validation ---

    [Fact]
    public async Task Filtres_partitionnent_les_occurrences()
    {
        var retard = CreerPonctuelle(echeance: Aujourdhui.AddDays(-2));
        var future = CreerPonctuelle(echeance: Aujourdhui.AddDays(3));
        var sansEcheance = CreerPonctuelle(avecEcheance: false);
        var faite = CreerPonctuelle();
        await OperationsTaches.CompleterAsync(Db, EnAttenteDe(faite).Id, _alain.Id, null, _maintenant);
        var passee = CreerIntervalle();
        await OperationsTaches.PasserAsync(Db, EnAttenteDe(passee).Id, _alain.Id, _maintenant);

        var aujourdhui = await OperationsTaches.ListerOccurrencesAsync(Db, "aujourdhui", Aujourdhui, null, null);
        var avenir = await OperationsTaches.ListerOccurrencesAsync(Db, "avenir", Aujourdhui, null, null);
        var enAttente = await OperationsTaches.ListerOccurrencesAsync(Db, "en-attente", Aujourdhui, null, null);
        var completees = await OperationsTaches.ListerOccurrencesAsync(Db, "completees", Aujourdhui, null, null);
        var tout = await OperationsTaches.ListerOccurrencesAsync(Db, null, Aujourdhui, null, null);

        Assert.Equal([retard.Id], aujourdhui.Select(o => o.TacheId));
        // Avenir : la future, la sans-échéance, et la suivante matérialisée du passage (+7 j).
        Assert.Equal(
            new[] { future.Id, sansEcheance.Id, passee.Id }.Order(),
            avenir.Select(o => o.TacheId).Order());
        // en-attente : le retard, la future, la sans-échéance et la suivante du passage.
        Assert.Equal(4, enAttente.Count);
        Assert.Equal([faite.Id], completees.Select(o => o.TacheId));
        // Sans filtre : tout, y compris l'occurrence au statut Passee.
        Assert.Contains(tout, o => o.Statut == nameof(StatutOccurrence.Passee));
        Assert.Equal(6, tout.Count);
    }

    [Fact]
    public async Task Les_completees_sortent_des_plus_recentes_aux_plus_anciennes()
    {
        foreach (var decalage in new[] { 0, 2, 1 })
        {
            var tache = CreerPonctuelle();
            await OperationsTaches.CompleterAsync(
                Db, EnAttenteDe(tache).Id, _alain.Id, $"j+{decalage}", _maintenant.AddDays(decalage));
        }

        var completees = await OperationsTaches.ListerOccurrencesAsync(Db, "completees", Aujourdhui, null, null);

        Assert.Equal(["j+2", "j+1", "j+0"], completees.Select(o => o.Notes));
    }

    [Theory]
    [InlineData("pending")]
    [InlineData("aujourd'hui")]
    [InlineData("complétées")]
    public void Un_filtre_inconnu_est_refuse(string filtre)
    {
        var erreur = OperationsTaches.ValiderFiltre(filtre, null, null);

        Assert.NotNull(erreur);
        Assert.Contains("Filtre inconnu", erreur!.Message);
    }

    [Fact]
    public void Le_filtre_faites_exige_ses_deux_bornes()
    {
        Assert.NotNull(OperationsTaches.ValiderFiltre("faites", null, null));
        Assert.NotNull(OperationsTaches.ValiderFiltre("faites", _maintenant, null));
        Assert.Null(OperationsTaches.ValiderFiltre("faites", _maintenant, _maintenant.AddDays(1)));
        Assert.Null(OperationsTaches.ValiderFiltre(null, null, null));
    }

    [Fact]
    public void Le_filtre_faites_refuse_les_bornes_inversees()
    {
        // T6 (issue #53) : bornes inversées ou fenêtre vide donnaient un 200 []
        // silencieux qui cachait l'erreur du client.
        Assert.NotNull(OperationsTaches.ValiderFiltre("faites", _maintenant.AddDays(1), _maintenant));
        Assert.NotNull(OperationsTaches.ValiderFiltre("faites", _maintenant, _maintenant));
    }

    // --- Stratégies d'assignation : bornes de la fenêtre 90 jours ---

    [Fact]
    public async Task MoinsLAFait_compte_la_completion_en_cours()
    {
        // Issue #2 : l'entrée de journal en vol était invisible de la requête SQL —
        // le compléteur se faisait réassigner une fois sur deux (motif A, B, B, A, A…
        // au lieu de l'équité).
        var tache = CreerIntervalle(strategie: StrategieAssignation.MoinsLAFait);

        var premiere = await OperationsTaches.CompleterAsync(
            Db, EnAttenteDe(tache).Id, _alain.Id, null, _maintenant);
        // Alain 1 (en vol) / Ariane 0 → Ariane l'a moins fait.
        Assert.Equal(_ariane.Id, premiere.Prochaine!.AssigneAId);

        var deuxieme = await OperationsTaches.CompleterAsync(
            Db, premiere.Prochaine.Id, _ariane.Id, null, _maintenant.AddDays(7));
        // Alain 1 / Ariane 1 (en vol) → égalité → l'autre que la dernière compléteure.
        // Avant le correctif, la requête lisait Alain 1 / Ariane 0 et réassignait
        // Ariane alors qu'elle venait de faire la tâche.
        Assert.Equal(_alain.Id, deuxieme.Prochaine!.AssigneAId);
    }

    [Fact]
    public async Task Completer_en_alternance_decale_l_assigne()
    {
        // Contrepartie de la décision T8 : compléter, lui, prend le tour.
        var tache = CreerIntervalle(strategie: StrategieAssignation.Alternance);

        var resultat = await OperationsTaches.CompleterAsync(
            Db, EnAttenteDe(tache).Id, _alain.Id, null, _maintenant);

        Assert.Equal(_ariane.Id, resultat.Prochaine!.AssigneAId);
    }

    [Fact]
    public async Task PasserConserve_l_assigne_en_alternance()
    {
        // Décision T8 (2026-08-28) : passer ne prend pas le tour — la prochaine
        // occurrence garde l'assigné, peu importe qui a cliqué.
        var tache = CreerIntervalle(strategie: StrategieAssignation.Alternance);
        var occurrence = EnAttenteDe(tache);
        occurrence.AssigneAId = _ariane.Id;
        Db.SaveChanges();

        var resultat = await OperationsTaches.PasserAsync(Db, occurrence.Id, _alain.Id, _maintenant);

        Assert.Equal(StatutPasse.Ok, resultat.Statut);
        Assert.Equal(_ariane.Id, resultat.Prochaine!.AssigneAId);
    }

    [Fact]
    public async Task PasserConserve_l_assigne_en_moins_l_a_fait()
    {
        // Décision T8 (2026-08-28) : même règle pour MoinsLAFait — le tour n'a pas
        // été pris.
        var tache = CreerIntervalle(strategie: StrategieAssignation.MoinsLAFait);
        var occurrence = EnAttenteDe(tache);
        occurrence.AssigneAId = _ariane.Id;
        Db.SaveChanges();

        var resultat = await OperationsTaches.PasserAsync(Db, occurrence.Id, _alain.Id, _maintenant);

        Assert.Equal(StatutPasse.Ok, resultat.Statut);
        Assert.Equal(_ariane.Id, resultat.Prochaine!.AssigneAId);
    }

    [Fact]
    public async Task MoinsLAFait_une_completion_a_exactement_90_jours_compte_encore()
    {
        var tache = CreerIntervalle(strategie: StrategieAssignation.MoinsLAFait);
        Db.Journal.Add(new EntreeJournal
        {
            Id = Guid.NewGuid(),
            TacheId = tache.Id,
            OccurrenceId = Guid.NewGuid(),
            UtilisateurId = _ariane.Id,
            CompleteeLe = _maintenant.AddDays(-90),
        });
        Db.SaveChanges();

        var assigne = await OperationsTaches.ChoisirProchainAssigne(Db, tache, _alain.Id, _maintenant);

        // Ariane a 1 complétion dans la fenêtre, Alain 0 → Alain l'a moins fait.
        Assert.Equal(_alain.Id, assigne);
    }

    [Fact]
    public async Task MoinsLAFait_une_completion_a_91_jours_est_oubliee()
    {
        var tache = CreerIntervalle(strategie: StrategieAssignation.MoinsLAFait);
        Db.Journal.Add(new EntreeJournal
        {
            Id = Guid.NewGuid(),
            TacheId = tache.Id,
            OccurrenceId = Guid.NewGuid(),
            UtilisateurId = _ariane.Id,
            CompleteeLe = _maintenant.AddDays(-90).AddSeconds(-1),
        });
        Db.SaveChanges();

        var assigne = await OperationsTaches.ChoisirProchainAssigne(Db, tache, _alain.Id, _maintenant);

        // Fenêtre vide → égalité → repli alternance : l'autre que le dernier compléteur.
        Assert.Equal(_ariane.Id, assigne);
    }

    [Fact]
    public async Task Lister_taches_expose_definition_et_occurrence_en_attente()
    {
        var tache = CreerIntervalle(jours: 7);
        var document = new Document
        {
            Id = Guid.NewGuid(),
            Titre = "Guide de la tondeuse",
            NomFichier = "guide.pdf",
            CheminDisque = "docs/guide.pdf",
            TypeMime = "application/pdf",
        };
        Db.Documents.Add(document);
        tache.Documents.Add(document);
        Db.SaveChanges();

        var resumes = await OperationsTaches.ListerTachesAsync(Db);

        var resume = Assert.Single(resumes);
        Assert.Equal(tache.Id, resume.Id);
        Assert.Equal(EnAttenteDe(tache).Id, resume.OccurrenceId);
        Assert.Equal(Aujourdhui, resume.Echeance);
        Assert.Equal(_alain.Id, resume.AssigneA?.Id);
        Assert.Equal("Intervalle", resume.Recurrence.Mode);
        Assert.Equal(7, resume.Recurrence.IntervalleJours);
        Assert.Equal(1, resume.NbDocuments);
        Assert.False(resume.Completee);
    }

    [Fact]
    public async Task Le_rollover_n_est_expose_que_pour_le_mode_fixe()
    {
        // T9 (issue #53) : exposer le flag sur une intervalle laisserait croire
        // qu'il y agit.
        CreerIntervalle();
        CreerHebdoLundi();

        var resumes = await OperationsTaches.ListerTachesAsync(Db);

        Assert.Null(Assert.Single(resumes, r => r.Recurrence.Mode == "Intervalle").Recurrence.Rollover);
        Assert.True(Assert.Single(resumes, r => r.Recurrence.Mode == "Fixe").Recurrence.Rollover);
    }

    [Fact]
    public async Task Lister_taches_marque_la_ponctuelle_faite_et_trie_par_echeance()
    {
        var faite = CreerPonctuelle(echeance: Aujourdhui.AddDays(1));
        await OperationsTaches.CompleterAsync(Db, EnAttenteDe(faite).Id, _alain.Id, null, _maintenant);
        var lointaine = CreerIntervalle(jours: 30);
        EnAttenteDe(lointaine).Echeance = Aujourdhui.AddDays(30);
        Db.SaveChanges();

        var resumes = await OperationsTaches.ListerTachesAsync(Db);

        Assert.Equal(2, resumes.Count);
        // L'intervalle (échéance +30) passe devant : la ponctuelle faite n'a plus d'échéance.
        Assert.Equal(lointaine.Id, resumes[0].Id);
        Assert.False(resumes[0].Completee);
        Assert.Equal(faite.Id, resumes[1].Id);
        Assert.True(resumes[1].Completee);
        Assert.Null(resumes[1].OccurrenceId);
        Assert.Null(resumes[1].Echeance);
        Assert.Null(resumes[1].AssigneA);
    }
}
