using HouseOs.Api.Domaine;
using HouseOs.Api.Features.Taches;

namespace HouseOs.Tests.Features.Taches;

public class OperationsTachesTests : TestAvecSqlite
{
    private readonly Utilisateur _alain;
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
        Db.Utilisateurs.Add(_alain);
        Db.Utilisateurs.Add(new Utilisateur
        {
            Id = Guid.NewGuid(),
            NomUtilisateur = "ariane",
            NomAffichage = "Ariane",
            MotDePasseHash = "x",
        });
        Db.SaveChanges();
    }

    private Tache CreerIntervalle(int jours = 7)
    {
        var spec = new SpecRecurrence { Mode = ModeRecurrence.Intervalle, IntervalleJours = jours };
        var tache = Tache.CreerRecurrente(
            "Tondre", null, spec, StrategieAssignation.Fixe, _alain.Id,
            Aujourdhui, _alain.Id, _maintenant, Aujourdhui);
        Db.Taches.Add(tache);
        Db.SaveChanges();
        return tache;
    }

    private Tache CreerPonctuelle()
    {
        var tache = Tache.CreerPonctuelle(
            "Changer l'adresse", null, Aujourdhui, _alain.Id, _alain.Id, _maintenant);
        Db.Taches.Add(tache);
        Db.SaveChanges();
        return tache;
    }

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
}
