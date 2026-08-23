using HouseOs.Api.Domaine;

namespace HouseOs.Tests.Domaine;

public class MoteurRecurrenceTests
{
    private static SpecRecurrence Hebdo(params DayOfWeek[] jours) => new()
    {
        Mode = ModeRecurrence.Fixe,
        FixeType = TypeFixe.JoursSemaine,
        JoursSemaineMasque = SpecRecurrence.MasqueDe(jours),
    };

    private static SpecRecurrence Mensuelle(int jour) => new()
    {
        Mode = ModeRecurrence.Fixe,
        FixeType = TypeFixe.JourDuMois,
        JourDuMois = jour,
    };

    private static SpecRecurrence Annuelle(int mois, int jour) => new()
    {
        Mode = ModeRecurrence.Fixe,
        FixeType = TypeFixe.Annuelle,
        MoisAnnuel = mois,
        JourAnnuel = jour,
    };

    private static SpecRecurrence Intervalle(int jours) => new()
    {
        Mode = ModeRecurrence.Intervalle,
        IntervalleJours = jours,
    };

    private static void AvecFenetre(SpecRecurrence spec, int debutMois, int debutJour, int finMois, int finJour)
    {
        spec.FenetreDebutMois = debutMois;
        spec.FenetreDebutJour = debutJour;
        spec.FenetreFinMois = finMois;
        spec.FenetreFinJour = finJour;
    }

    // --- Ponctuelle ---

    [Fact]
    public void Ponctuelle_NaPasDeSuite()
    {
        var prochaine = MoteurRecurrence.ProchaineEcheance(
            SpecRecurrence.Ponctuelle(), new DateOnly(2026, 8, 23));
        Assert.Null(prochaine);
    }

    // --- Fixe : jours de semaine ---

    [Fact]
    public void Hebdo_CompleteeLeLundi_ProchainLundi()
    {
        // 2026-08-24 est un lundi
        var prochaine = MoteurRecurrence.ProchaineEcheance(
            Hebdo(DayOfWeek.Monday), new DateOnly(2026, 8, 24), new DateOnly(2026, 8, 24));
        Assert.Equal(new DateOnly(2026, 8, 31), prochaine);
    }

    [Fact]
    public void Hebdo_DeuxJours_ProchainJourPlanifie()
    {
        // lun + jeu, complétée lundi 24 → jeudi 27
        var prochaine = MoteurRecurrence.ProchaineEcheance(
            Hebdo(DayOfWeek.Monday, DayOfWeek.Thursday), new DateOnly(2026, 8, 24), new DateOnly(2026, 8, 24));
        Assert.Equal(new DateOnly(2026, 8, 27), prochaine);
    }

    [Fact]
    public void Hebdo_CompleteeEnRetard_PartDeLaCompletion()
    {
        // Échéance lundi 24, complétée mercredi 26 → prochain lundi 31 (pas d'empilement)
        var prochaine = MoteurRecurrence.ProchaineEcheance(
            Hebdo(DayOfWeek.Monday), new DateOnly(2026, 8, 26), new DateOnly(2026, 8, 24));
        Assert.Equal(new DateOnly(2026, 8, 31), prochaine);
    }

    [Fact]
    public void Hebdo_CompleteeEnAvance_PartDeLEcheance()
    {
        // Échéance lundi 31, complétée samedi 29 (en avance) → lundi 7 sept, pas le 31
        var prochaine = MoteurRecurrence.ProchaineEcheance(
            Hebdo(DayOfWeek.Monday), new DateOnly(2026, 8, 29), new DateOnly(2026, 8, 31));
        Assert.Equal(new DateOnly(2026, 9, 7), prochaine);
    }

    // --- Fixe : jour du mois ---

    [Fact]
    public void Mensuelle_Le15_ProchainMois()
    {
        var prochaine = MoteurRecurrence.ProchaineEcheance(
            Mensuelle(15), new DateOnly(2026, 8, 15), new DateOnly(2026, 8, 15));
        Assert.Equal(new DateOnly(2026, 9, 15), prochaine);
    }

    [Fact]
    public void Mensuelle_Le31_ClampeEnFevrier()
    {
        var prochaine = MoteurRecurrence.ProchaineEcheance(
            Mensuelle(31), new DateOnly(2027, 1, 31), new DateOnly(2027, 1, 31));
        Assert.Equal(new DateOnly(2027, 2, 28), prochaine);
    }

    [Fact]
    public void Mensuelle_Le31_FevrierBissextile()
    {
        var prochaine = MoteurRecurrence.ProchaineEcheance(
            Mensuelle(31), new DateOnly(2028, 1, 31), new DateOnly(2028, 1, 31));
        Assert.Equal(new DateOnly(2028, 2, 29), prochaine);
    }

    // --- Fixe : annuelle ---

    [Fact]
    public void Annuelle_ProchaineAnnee()
    {
        var prochaine = MoteurRecurrence.ProchaineEcheance(
            Annuelle(10, 15), new DateOnly(2026, 10, 15), new DateOnly(2026, 10, 15));
        Assert.Equal(new DateOnly(2027, 10, 15), prochaine);
    }

    [Fact]
    public void Annuelle_29Fevrier_Clampe28HorsBissextile()
    {
        var prochaine = MoteurRecurrence.ProchaineEcheance(
            Annuelle(2, 29), new DateOnly(2028, 2, 29), new DateOnly(2028, 2, 29));
        Assert.Equal(new DateOnly(2029, 2, 28), prochaine);
    }

    // --- Intervalle ---

    [Fact]
    public void Intervalle_CompletionPlusN()
    {
        var prochaine = MoteurRecurrence.ProchaineEcheance(
            Intervalle(7), new DateOnly(2026, 8, 23));
        Assert.Equal(new DateOnly(2026, 8, 30), prochaine);
    }

    [Fact]
    public void Intervalle_CompleteeEnRetard_PartToujoursDeLaCompletion()
    {
        // Peu importe l'échéance manquée : tondre = 7 jours après la DERNIÈRE tonte
        var prochaine = MoteurRecurrence.ProchaineEcheance(
            Intervalle(7), new DateOnly(2026, 8, 28), new DateOnly(2026, 8, 20));
        Assert.Equal(new DateOnly(2026, 9, 4), prochaine);
    }

    // --- Fenêtres saisonnières ---

    [Fact]
    public void Intervalle_FinDeFenetre_GlisseAuDebutDeLaProchaine()
    {
        // Tonte aux 7 jours, fenêtre 1er mai → 31 octobre ; complétée le 28 octobre
        var spec = Intervalle(7);
        AvecFenetre(spec, 5, 1, 10, 31);
        var prochaine = MoteurRecurrence.ProchaineEcheance(spec, new DateOnly(2026, 10, 28));
        Assert.Equal(new DateOnly(2027, 5, 1), prochaine);
    }

    [Fact]
    public void Intervalle_DansLaFenetre_ResteNormal()
    {
        var spec = Intervalle(7);
        AvecFenetre(spec, 5, 1, 10, 31);
        var prochaine = MoteurRecurrence.ProchaineEcheance(spec, new DateOnly(2026, 7, 10));
        Assert.Equal(new DateOnly(2026, 7, 17), prochaine);
    }

    [Fact]
    public void Fixe_HorsFenetre_PremierJourPlanifieDeLaProchaine()
    {
        // Hebdo lundi, fenêtre mai→octobre, complétée le lundi 26 octobre 2026 →
        // premier lundi dans la fenêtre 2027 = lundi 3 mai 2027
        var spec = Hebdo(DayOfWeek.Monday);
        AvecFenetre(spec, 5, 1, 10, 31);
        var prochaine = MoteurRecurrence.ProchaineEcheance(
            spec, new DateOnly(2026, 10, 26), new DateOnly(2026, 10, 26));
        Assert.Equal(new DateOnly(2027, 5, 3), prochaine);
    }

    [Fact]
    public void Fenetre_ChevauchantLAnnee_NovembreAMars()
    {
        // Déneigement : fenêtre 1er novembre → 31 mars
        var spec = Intervalle(3);
        AvecFenetre(spec, 11, 1, 3, 31);
        Assert.True(spec.DansFenetre(new DateOnly(2026, 12, 25)));
        Assert.True(spec.DansFenetre(new DateOnly(2027, 2, 10)));
        Assert.False(spec.DansFenetre(new DateOnly(2026, 7, 1)));

        // Complétée le 30 mars → hors fenêtre le 2 avril → glisse au 1er novembre
        var prochaine = MoteurRecurrence.ProchaineEcheance(spec, new DateOnly(2027, 3, 30));
        Assert.Equal(new DateOnly(2027, 11, 1), prochaine);
    }

    // --- Première échéance à la création ---

    [Fact]
    public void PremiereEcheance_Fixe_AujourdhuiSiPlanifie()
    {
        // Créée un lundi, tâche du lundi → due aujourd'hui même
        var premiere = MoteurRecurrence.PremiereEcheance(Hebdo(DayOfWeek.Monday), new DateOnly(2026, 8, 24));
        Assert.Equal(new DateOnly(2026, 8, 24), premiere);
    }

    [Fact]
    public void PremiereEcheance_Intervalle_AujourdhuiPlusN()
    {
        var premiere = MoteurRecurrence.PremiereEcheance(Intervalle(30), new DateOnly(2026, 8, 23));
        Assert.Equal(new DateOnly(2026, 9, 22), premiere);
    }

    [Fact]
    public void PremiereEcheance_FixeHorsFenetre_DebutDeFenetre()
    {
        var spec = Hebdo(DayOfWeek.Monday);
        AvecFenetre(spec, 5, 1, 10, 31);
        // Créée en décembre → premier lundi de mai 2027 (le 3)
        var premiere = MoteurRecurrence.PremiereEcheance(spec, new DateOnly(2026, 12, 15));
        Assert.Equal(new DateOnly(2027, 5, 3), premiere);
    }

    // --- Rollover (glissement d'une occurrence fixe manquée) ---

    [Fact]
    public void Rollover_OccurrenceManquee_GlisseALaProchaineDatePlanifiee()
    {
        // Échéance lundi 24 manquée ; on est jeudi 27 → glisse au lundi 31
        var prochaine = MoteurRecurrence.ProchainePlanifiee(Hebdo(DayOfWeek.Monday), new DateOnly(2026, 8, 27));
        Assert.Equal(new DateOnly(2026, 8, 31), prochaine);
    }

    [Fact]
    public void Rollover_LeJourMeme_ResteAujourdhui()
    {
        // On est lundi : la tâche du lundi est due aujourd'hui, pas glissée
        var prochaine = MoteurRecurrence.ProchainePlanifiee(Hebdo(DayOfWeek.Monday), new DateOnly(2026, 8, 24));
        Assert.Equal(new DateOnly(2026, 8, 24), prochaine);
    }
}
