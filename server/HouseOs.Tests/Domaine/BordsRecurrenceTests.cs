using HouseOs.Api.Domaine;

namespace HouseOs.Tests.Domaine;

/// <summary>
/// Cas de bord du moteur et de la fenêtre saisonnière relevés par l'audit de
/// couverture du 2026-08-25 : bornes exactes, specs dégénérées, chemins de repli.
/// </summary>
public class BordsRecurrenceTests
{
    private static SpecRecurrence Hebdo(params DayOfWeek[] jours) => new()
    {
        Mode = ModeRecurrence.Fixe,
        FixeType = TypeFixe.JoursSemaine,
        JoursSemaineMasque = SpecRecurrence.MasqueDe(jours),
    };

    private static SpecRecurrence AvecFenetre(SpecRecurrence spec,
        int debutMois, int debutJour, int finMois, int finJour)
    {
        spec.FenetreDebutMois = debutMois;
        spec.FenetreDebutJour = debutJour;
        spec.FenetreFinMois = finMois;
        spec.FenetreFinJour = finJour;
        return spec;
    }

    // --- Bornes de la fenêtre (incluses des deux côtés) ---

    [Theory]
    [InlineData(5, 1, true)]   // premier jour
    [InlineData(10, 31, true)] // dernier jour
    [InlineData(4, 30, false)] // veille du début
    [InlineData(11, 1, false)] // lendemain de la fin
    public void Fenetre_BornesIncluses(int mois, int jour, bool attendu)
    {
        var spec = AvecFenetre(new SpecRecurrence(), 5, 1, 10, 31);
        Assert.Equal(attendu, spec.DansFenetre(new DateOnly(2026, mois, jour)));
    }

    [Theory]
    [InlineData(11, 1, true)]  // premier jour (chevauche l'an)
    [InlineData(3, 31, true)]  // dernier jour
    [InlineData(10, 31, false)]
    [InlineData(4, 1, false)]
    public void Fenetre_Chevauchante_BornesIncluses(int mois, int jour, bool attendu)
    {
        var spec = AvecFenetre(new SpecRecurrence(), 11, 1, 3, 31);
        Assert.Equal(attendu, spec.DansFenetre(new DateOnly(2026, mois, jour)));
    }

    [Fact]
    public void Fenetre_DUnSeulJour_EstValide()
    {
        var spec = AvecFenetre(new SpecRecurrence(), 7, 14, 7, 14);
        Assert.True(spec.DansFenetre(new DateOnly(2026, 7, 14)));
        Assert.False(spec.DansFenetre(new DateOnly(2026, 7, 13)));
        Assert.False(spec.DansFenetre(new DateOnly(2026, 7, 15)));
    }

    // --- Premières échéances et chemins de repli du moteur ---

    [Fact]
    public void PremiereEcheance_IntervalleHorsFenetre_DebutDeFenetre()
    {
        // « Tondre aux 7 jours », fenêtre mai→octobre, créée le 15 décembre.
        var spec = AvecFenetre(
            new SpecRecurrence { Mode = ModeRecurrence.Intervalle, IntervalleJours = 7 },
            5, 1, 10, 31);

        var echeance = MoteurRecurrence.PremiereEcheance(spec, new DateOnly(2026, 12, 15));

        Assert.Equal(new DateOnly(2027, 5, 1), echeance);
    }

    [Fact]
    public void Fixe_SansEcheanceCourante_PartDeLaCompletion()
    {
        // Une occurrence sans échéance (héritée d'un passage par Ponctuelle) : la
        // référence du calcul est la date de complétion.
        var spec = Hebdo(DayOfWeek.Monday);
        var lundi = new DateOnly(2026, 9, 14);

        var prochaine = MoteurRecurrence.ProchaineEcheance(spec, lundi, echeanceCourante: null);

        Assert.Equal(lundi.AddDays(7), prochaine);
    }

    [Fact]
    public void Fixe_MasqueDeJoursVide_LeveUneErreurExplicite()
    {
        // Le chemin API est protégé par ConvertirRecurrence ; le chemin domaine
        // (seed, migration) doit au moins échouer bruyamment, jamais boucler sans fin.
        var spec = new SpecRecurrence
        {
            Mode = ModeRecurrence.Fixe,
            FixeType = TypeFixe.JoursSemaine,
            JoursSemaineMasque = 0,
        };

        Assert.Throws<InvalidOperationException>(() =>
            MoteurRecurrence.ProchainePlanifiee(spec, new DateOnly(2026, 9, 14)));
    }

    [Fact]
    public void Passer_UneOccurrenceFixeFuture_PlanifieApresSonEcheance()
    {
        // « Passer » saute l'occurrence visée : la suivante part de l'échéance
        // sautée (même reportée dans le futur), pas de la date du clic.
        var spec = Hebdo(DayOfWeek.Monday);
        var mercredi = new DateOnly(2026, 8, 26);
        var lundiReporte = new DateOnly(2026, 8, 31);

        var prochaine = MoteurRecurrence.ProchaineEcheance(spec, mercredi, lundiReporte);

        Assert.Equal(new DateOnly(2026, 9, 7), prochaine);
    }

    // --- Rollover : glissement hors fenêtre ---

    [Fact]
    public void Rollover_OccurrenceHorsFenetre_GlisseAuDebutDeLaProchaineFenetre()
    {
        // Hebdo lundi en fenêtre mai→octobre, manquée fin octobre, service le 2 nov :
        // l'échéance saute au premier lundi de mai suivant — comportement assumé.
        var spec = AvecFenetre(Hebdo(DayOfWeek.Monday), 5, 1, 10, 31);

        var glissee = MoteurRecurrence.ProchainePlanifiee(spec, new DateOnly(2026, 11, 2));

        Assert.Equal(new DateOnly(2027, 5, 3), glissee); // premier lundi de mai 2027
        Assert.True(glissee > new DateOnly(2026, 11, 2));
    }
}
