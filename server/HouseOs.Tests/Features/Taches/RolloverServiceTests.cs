using HouseOs.Api.Domaine;
using HouseOs.Api.Features.Taches;

namespace HouseOs.Tests.Features.Taches;

/// <summary>
/// Le cœur du glissement quotidien : seules les occurrences FIXES, rollover actif,
/// à échéance strictement passée, glissent. Une inversion de condition ici change
/// silencieusement le comportement de toute la maison.
/// </summary>
public class RolloverServiceTests : TestAvecSqlite
{
    private readonly Utilisateur _alain;
    private readonly DateTimeOffset _maintenant = new(2026, 9, 15, 14, 0, 0, TimeSpan.Zero);
    private DateOnly Aujourdhui => DateOnly.FromDateTime(_maintenant.LocalDateTime);

    public RolloverServiceTests()
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

    private Occurrence Creer(SpecRecurrence spec, DateOnly echeance)
    {
        var tache = spec.Mode == ModeRecurrence.Ponctuelle
            ? Tache.CreerPonctuelle("T", null, echeance, null, _alain.Id, _maintenant)
            : Tache.CreerRecurrente("T", null, spec, StrategieAssignation.Fixe, null,
                echeance, _alain.Id, _maintenant, Aujourdhui);
        Db.Taches.Add(tache);
        Db.SaveChanges();
        var occurrence = Db.Occurrences.Single(o => o.TacheId == tache.Id);
        occurrence.Echeance = echeance;
        Db.SaveChanges();
        return occurrence;
    }

    private static SpecRecurrence HebdoLundi(bool rollover = true) => new()
    {
        Mode = ModeRecurrence.Fixe,
        FixeType = TypeFixe.JoursSemaine,
        JoursSemaineMasque = SpecRecurrence.MasqueDe(DayOfWeek.Monday),
        Rollover = rollover,
    };

    [Fact]
    public async Task Une_fixe_manquee_glisse_a_la_prochaine_date_planifiee()
    {
        // Le 2026-09-15 est un mardi : lundi manqué hier → prochain lundi 21.
        var occurrence = Creer(HebdoLundi(), Aujourdhui.AddDays(-1));

        var glissees = await RolloverService.GlisserOccurrencesManquees(Db, Aujourdhui);

        Assert.Equal(1, glissees);
        Assert.Equal(new DateOnly(2026, 9, 21), occurrence.Echeance);
    }

    [Fact]
    public async Task Une_intervalle_manquee_ne_glisse_pas()
    {
        // « Tondre » reste dû tant que ce n'est pas fait.
        var occurrence = Creer(
            new SpecRecurrence { Mode = ModeRecurrence.Intervalle, IntervalleJours = 7 },
            Aujourdhui.AddDays(-3));

        var glissees = await RolloverService.GlisserOccurrencesManquees(Db, Aujourdhui);

        Assert.Equal(0, glissees);
        Assert.Equal(Aujourdhui.AddDays(-3), occurrence.Echeance);
    }

    /// <summary>Tondre aux 7 jours, fenêtre mai–octobre — le cas type de la décision T7.</summary>
    private static SpecRecurrence TondreMaiOctobre(bool rollover = false) => new()
    {
        Mode = ModeRecurrence.Intervalle,
        IntervalleJours = 7,
        FenetreDebutMois = 5,
        FenetreDebutJour = 1,
        FenetreFinMois = 10,
        FenetreFinJour = 31,
        Rollover = rollover,
    };

    [Fact]
    public async Task Une_intervalle_echue_HorsFenetre_glisse_au_debut_de_la_prochaine_fenetre()
    {
        // Décision T7 (2026-08-28) : échue le 28 octobre, jamais tondue, on est en
        // novembre — la fenêtre est refermée, l'occurrence glisse au 1ᵉʳ mai au lieu
        // de rester « en retard » tout l'hiver.
        var occurrence = Creer(TondreMaiOctobre(), new DateOnly(2026, 10, 28));

        var glissees = await RolloverService.GlisserOccurrencesManquees(Db, new DateOnly(2026, 11, 5));

        Assert.Equal(1, glissees);
        Assert.Equal(new DateOnly(2027, 5, 1), occurrence.Echeance);
    }

    [Fact]
    public async Task Le_glissement_HorsFenetre_d_une_intervalle_ignore_le_flag_rollover()
    {
        // Décision T7 : ce glissement est indépendant du flag rollover, réservé au
        // mode fixe (T9).
        var occurrence = Creer(TondreMaiOctobre(rollover: true), new DateOnly(2026, 10, 28));

        var glissees = await RolloverService.GlisserOccurrencesManquees(Db, new DateOnly(2026, 11, 5));

        Assert.Equal(1, glissees);
        Assert.Equal(new DateOnly(2027, 5, 1), occurrence.Echeance);
    }

    [Fact]
    public async Task Une_intervalle_echue_dont_la_fenetre_est_encore_ouverte_reste_due()
    {
        // Tant que la fenêtre est ouverte, « tondre » reste dû (la décision T7 ne
        // joue qu'à la fermeture).
        var occurrence = Creer(TondreMaiOctobre(), new DateOnly(2026, 10, 20));

        var glissees = await RolloverService.GlisserOccurrencesManquees(Db, new DateOnly(2026, 10, 30));

        Assert.Equal(0, glissees);
        Assert.Equal(new DateOnly(2026, 10, 20), occurrence.Echeance);
    }

    [Fact]
    public async Task Le_flag_desactive_laisse_l_occurrence_en_retard()
    {
        var occurrence = Creer(HebdoLundi(rollover: false), Aujourdhui.AddDays(-1));

        var glissees = await RolloverService.GlisserOccurrencesManquees(Db, Aujourdhui);

        Assert.Equal(0, glissees);
        Assert.Equal(Aujourdhui.AddDays(-1), occurrence.Echeance);
    }

    [Fact]
    public async Task Une_echeance_d_aujourdhui_n_est_pas_touchee()
    {
        var occurrence = Creer(HebdoLundi(), Aujourdhui);

        var glissees = await RolloverService.GlisserOccurrencesManquees(Db, Aujourdhui);

        Assert.Equal(0, glissees);
        Assert.Equal(Aujourdhui, occurrence.Echeance);
    }

    [Fact]
    public async Task Une_occurrence_completee_n_est_jamais_glissee()
    {
        var occurrence = Creer(HebdoLundi(), Aujourdhui.AddDays(-1));
        occurrence.Completer(_alain.Id, _maintenant);
        Db.SaveChanges();

        var glissees = await RolloverService.GlisserOccurrencesManquees(Db, Aujourdhui);

        Assert.Equal(0, glissees);
    }

    [Fact]
    public void Le_delai_du_prochain_passage_est_toujours_positif_et_vise_la_nuit()
    {
        // Un délai négatif (horloge reculée, config farfelue) tuerait le service via
        // ArgumentOutOfRangeException — le plancher est d'une minute.
        var maintenant = DateTimeOffset.Now;

        var delai = RolloverService.DelaiProchainPassage(maintenant);

        Assert.True(delai >= TimeSpan.FromMinutes(1));
        Assert.True(delai <= TimeSpan.FromHours(25)); // marge changement d'heure
        var cible = maintenant + delai;
        Assert.Equal(0, cible.LocalDateTime.Hour);
        Assert.InRange(cible.LocalDateTime.Minute, 5, 7);
    }
}
