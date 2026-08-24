using HouseOs.Api.Domaine;
using HouseOs.Api.Features.Taches;

namespace HouseOs.Tests.Features.Taches;

public class ConversionRecurrenceTests
{
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
        var (spec, erreur) = OperationsTaches.ConvertirRecurrence(null);
        Assert.Null(erreur);
        Assert.Equal(ModeRecurrence.Ponctuelle, spec.Mode);
    }

    [Fact]
    public void ModeInconnu_DonneErreur()
    {
        var (_, erreur) = OperationsTaches.ConvertirRecurrence(Dto(mode: "Hebdomadaire"));
        Assert.NotNull(erreur);
        Assert.Contains("Mode inconnu", erreur);
    }

    [Fact]
    public void IntervalleSansJours_DonneErreur()
    {
        var (_, erreur) = OperationsTaches.ConvertirRecurrence(Dto(mode: "Intervalle"));
        Assert.NotNull(erreur);
    }

    [Fact]
    public void IntervalleValide_DonneSpec()
    {
        var (spec, erreur) = OperationsTaches.ConvertirRecurrence(Dto(mode: "Intervalle", intervalleJours: 7));
        Assert.Null(erreur);
        Assert.Equal(ModeRecurrence.Intervalle, spec.Mode);
        Assert.Equal(7, spec.IntervalleJours);
    }

    [Fact]
    public void FixeSansType_DonneErreur()
    {
        var (_, erreur) = OperationsTaches.ConvertirRecurrence(Dto());
        Assert.NotNull(erreur);
    }

    [Fact]
    public void JoursSemaineVides_DonneErreur()
    {
        var (_, erreur) = OperationsTaches.ConvertirRecurrence(
            Dto(fixeType: "JoursSemaine", joursSemaine: []));
        Assert.NotNull(erreur);
    }

    [Fact]
    public void JourSemaineHorsBornes_DonneErreur()
    {
        var (_, erreur) = OperationsTaches.ConvertirRecurrence(
            Dto(fixeType: "JoursSemaine", joursSemaine: [1, 9]));
        Assert.NotNull(erreur);
    }

    [Fact]
    public void JoursSemaineValides_DonnentMasque()
    {
        var (spec, erreur) = OperationsTaches.ConvertirRecurrence(
            Dto(fixeType: "JoursSemaine", joursSemaine: [1, 3]));
        Assert.Null(erreur);
        Assert.Equal(SpecRecurrence.MasqueDe(DayOfWeek.Monday, DayOfWeek.Wednesday), spec.JoursSemaineMasque);
    }

    [Fact]
    public void JourDuMoisManquant_DonneErreur()
    {
        var (_, erreur) = OperationsTaches.ConvertirRecurrence(Dto(fixeType: "JourDuMois"));
        Assert.NotNull(erreur);
    }

    [Fact]
    public void AnnuelleValide_DonneSpec()
    {
        var (spec, erreur) = OperationsTaches.ConvertirRecurrence(
            Dto(fixeType: "Annuelle", moisAnnuel: 6, jourAnnuel: 15));
        Assert.Null(erreur);
        Assert.Equal(6, spec.MoisAnnuel);
        Assert.Equal(15, spec.JourAnnuel);
    }

    [Fact]
    public void FenetreIncomplete_DonneErreur()
    {
        var (_, erreur) = OperationsTaches.ConvertirRecurrence(
            Dto(mode: "Intervalle", intervalleJours: 7, fenetreDebutMois: 4, fenetreDebutJour: 1));
        Assert.NotNull(erreur);
        Assert.Contains("Fenêtre", erreur);
    }

    [Fact]
    public void FenetreComplete_EstAppliquee()
    {
        var (spec, erreur) = OperationsTaches.ConvertirRecurrence(Dto(
            mode: "Intervalle", intervalleJours: 7,
            fenetreDebutMois: 4, fenetreDebutJour: 1, fenetreFinMois: 10, fenetreFinJour: 31));
        Assert.Null(erreur);
        Assert.Equal(4, spec.FenetreDebutMois);
        Assert.Equal(31, spec.FenetreFinJour);
    }

    [Fact]
    public void RolloverOmis_EstVraiParDefaut()
    {
        var (spec, _) = OperationsTaches.ConvertirRecurrence(Dto(mode: "Intervalle", intervalleJours: 3));
        Assert.True(spec.Rollover);
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
}
