using HouseOs.Api.Domaine;
using HouseOs.Api.Features.Taches;

namespace HouseOs.Tests.Features.Taches;

public class ConversionRecurrenceTests
{
    // La sonde de cohérence s'ancre sur aujourd'hui (issue #31) : une date épinglée
    // garde les tests déterministes. Le 2026-09-14 (lundi) est à 17 jours.
    private static readonly DateOnly Aujourdhui = new(2026, 8, 28);

    private static (SpecRecurrence Spec, string? Erreur) Convertir(RecurrenceDto? dto) =>
        OperationsTaches.ConvertirRecurrence(dto, Aujourdhui);

    private static RecurrenceDto Dto(
        string mode = "Fixe",
        string? fixeType = null,
        int[]? joursSemaine = null,
        int? jourDuMois = null,
        int? moisAnnuel = null,
        int? jourAnnuel = null,
        int? intervalleJours = null,
        int? fenetreDebutMois = null,
        int? fenetreDebutJour = null,
        int? fenetreFinMois = null,
        int? fenetreFinJour = null,
        bool? rollover = null) =>
        new(mode, fixeType, joursSemaine, jourDuMois, moisAnnuel, jourAnnuel, intervalleJours,
            fenetreDebutMois, fenetreDebutJour, fenetreFinMois, fenetreFinJour, rollover);

    [Fact]
    public void DtoNull_DonnePonctuelleSansErreur()
    {
        var (spec, erreur) = Convertir(null);
        Assert.Null(erreur);
        Assert.Equal(ModeRecurrence.Ponctuelle, spec.Mode);
    }

    [Fact]
    public void ModeInconnu_DonneErreur()
    {
        var (_, erreur) = Convertir(Dto(mode: "Hebdomadaire"));
        Assert.NotNull(erreur);
        Assert.Contains("Mode inconnu", erreur);
    }

    [Fact]
    public void IntervalleSansJours_DonneErreur()
    {
        var (_, erreur) = Convertir(Dto(mode: "Intervalle"));
        Assert.NotNull(erreur);
    }

    [Fact]
    public void IntervalleValide_DonneSpec()
    {
        var (spec, erreur) = Convertir(Dto(mode: "Intervalle", intervalleJours: 7));
        Assert.Null(erreur);
        Assert.Equal(ModeRecurrence.Intervalle, spec.Mode);
        Assert.Equal(7, spec.IntervalleJours);
    }

    [Fact]
    public void FixeSansType_DonneErreur()
    {
        var (_, erreur) = Convertir(Dto());
        Assert.NotNull(erreur);
    }

    [Fact]
    public void JoursSemaineVides_DonneErreur()
    {
        var (_, erreur) = Convertir(
            Dto(fixeType: "JoursSemaine", joursSemaine: []));
        Assert.NotNull(erreur);
    }

    [Fact]
    public void JourSemaineHorsBornes_DonneErreur()
    {
        var (_, erreur) = Convertir(
            Dto(fixeType: "JoursSemaine", joursSemaine: [1, 9]));
        Assert.NotNull(erreur);
    }

    [Fact]
    public void JoursSemaineValides_DonnentMasque()
    {
        var (spec, erreur) = Convertir(
            Dto(fixeType: "JoursSemaine", joursSemaine: [1, 3]));
        Assert.Null(erreur);
        Assert.Equal(SpecRecurrence.MasqueDe(DayOfWeek.Monday, DayOfWeek.Wednesday), spec.JoursSemaineMasque);
    }

    [Fact]
    public void JourDuMoisManquant_DonneErreur()
    {
        var (_, erreur) = Convertir(Dto(fixeType: "JourDuMois"));
        Assert.NotNull(erreur);
    }

    [Fact]
    public void AnnuelleValide_DonneSpec()
    {
        var (spec, erreur) = Convertir(
            Dto(fixeType: "Annuelle", moisAnnuel: 6, jourAnnuel: 15));
        Assert.Null(erreur);
        Assert.Equal(6, spec.MoisAnnuel);
        Assert.Equal(15, spec.JourAnnuel);
    }

    [Fact]
    public void FenetreIncomplete_DonneErreur()
    {
        var (_, erreur) = Convertir(
            Dto(mode: "Intervalle", intervalleJours: 7, fenetreDebutMois: 4, fenetreDebutJour: 1));
        Assert.NotNull(erreur);
        Assert.Contains("Fenêtre", erreur);
    }

    [Fact]
    public void FenetreComplete_EstAppliquee()
    {
        var (spec, erreur) = Convertir(Dto(
            mode: "Intervalle", intervalleJours: 7,
            fenetreDebutMois: 4, fenetreDebutJour: 1, fenetreFinMois: 10, fenetreFinJour: 31));
        Assert.Null(erreur);
        Assert.Equal(4, spec.FenetreDebutMois);
        Assert.Equal(31, spec.FenetreFinJour);
    }

    // T9 (issue #53) : le flag rollover est réservé au mode fixe — le défaut ne
    // s'observe donc que là.
    [Fact]
    public void RolloverOmis_EstVraiParDefautEnModeFixe()
    {
        var (spec, erreur) = Convertir(Dto(fixeType: "JoursSemaine", joursSemaine: [1]));
        Assert.Null(erreur);
        Assert.True(spec.Rollover);
    }

    [Fact]
    public void RolloverSurIntervalle_DonneErreur()
    {
        var (_, erreur) = Convertir(Dto(mode: "Intervalle", intervalleJours: 3, rollover: true));
        Assert.NotNull(erreur);
        Assert.Contains("mode fixe", erreur);
    }

    [Fact]
    public void RolloverSurPonctuelle_DonneErreur()
    {
        var (_, erreur) = Convertir(Dto(mode: "Ponctuelle", rollover: true));
        Assert.NotNull(erreur);
        Assert.Contains("mode fixe", erreur);
    }

    [Fact]
    public void RolloverFauxSurIntervalle_ResteAccepte()
    {
        var (_, erreur) = Convertir(Dto(mode: "Intervalle", intervalleJours: 3, rollover: false));
        Assert.Null(erreur);
    }

    [Fact]
    public void StrategieNulle_DonneFixe()
    {
        var (strategie, erreur) = OperationsTaches.ConvertirStrategie(null);
        Assert.Null(erreur);
        Assert.Equal(StrategieAssignation.Fixe, strategie);
    }

    [Fact]
    public void StrategieValide_EstConvertie()
    {
        var (strategie, erreur) = OperationsTaches.ConvertirStrategie("Alternance");
        Assert.Null(erreur);
        Assert.Equal(StrategieAssignation.Alternance, strategie);
    }

    [Fact]
    public void StrategieInconnue_DonneErreur()
    {
        var (_, erreur) = OperationsTaches.ConvertirStrategie("AuHasard");
        Assert.NotNull(erreur);
    }

    // --- Validation croisée spec × fenêtre : chaque champ peut être valide isolément
    // alors que la combinaison ne planifie jamais rien (sinon : 500 à la création). ---

    [Fact]
    public void AnnuelleHorsDeLaFenetre_DonneErreur()
    {
        var (_, erreur) = Convertir(Dto(
            fixeType: "Annuelle", moisAnnuel: 1, jourAnnuel: 15,
            fenetreDebutMois: 5, fenetreDebutJour: 1, fenetreFinMois: 10, fenetreFinJour: 31));
        Assert.NotNull(erreur);
        Assert.Contains("jamais", erreur);
    }

    [Fact]
    public void FenetreAvecJourInexistant_DonneErreur()
    {
        var (_, erreur) = Convertir(Dto(
            mode: "Intervalle", intervalleJours: 7,
            fenetreDebutMois: 4, fenetreDebutJour: 31, fenetreFinMois: 4, fenetreFinJour: 31));
        Assert.NotNull(erreur);
        Assert.Contains("n'existe pas", erreur);
    }

    [Fact]
    public void Fenetre29Fevrier_EstAcceptee()
    {
        var (_, erreur) = Convertir(Dto(
            mode: "Intervalle", intervalleJours: 7,
            fenetreDebutMois: 2, fenetreDebutJour: 29, fenetreFinMois: 3, fenetreFinJour: 31));
        Assert.Null(erreur);
    }

    [Fact]
    public void AnnuelleDansLaFenetre_EstAcceptee()
    {
        var (_, erreur) = Convertir(Dto(
            fixeType: "Annuelle", moisAnnuel: 6, jourAnnuel: 15,
            fenetreDebutMois: 5, fenetreDebutJour: 1, fenetreFinMois: 10, fenetreFinJour: 31));
        Assert.Null(erreur);
    }

    [Fact]
    public void RecurrenceTropRareDansLaFenetre_DonneErreur()
    {
        // Issue #31 : « lundi + fenêtre 14/09–14/09 » — le 14 septembre 2026 est bien
        // un lundi, mais le suivant est en 2037 : accepter ferait perdre la complétion
        // dans un 500. La sonde en deux temps refuse dès la création.
        var (_, erreur) = Convertir(Dto(
            fixeType: "JoursSemaine", joursSemaine: [1],
            fenetreDebutMois: 9, fenetreDebutJour: 14, fenetreFinMois: 9, fenetreFinJour: 14));
        Assert.NotNull(erreur);
        Assert.Contains("fenêtre", erreur);
    }

    [Fact]
    public void AnnuelleDansFenetreDUnJour_EstAcceptee()
    {
        // Contre-épreuve de la sonde en deux temps : une annuelle à fenêtre d'un jour
        // revient chaque année — elle doit rester acceptée.
        var (_, erreur) = Convertir(Dto(
            fixeType: "Annuelle", moisAnnuel: 9, jourAnnuel: 14,
            fenetreDebutMois: 9, fenetreDebutJour: 14, fenetreFinMois: 9, fenetreFinJour: 14));
        Assert.Null(erreur);
    }
}
