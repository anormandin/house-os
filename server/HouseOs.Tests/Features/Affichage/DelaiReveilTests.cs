using HouseOs.Api.Features.Affichage;

namespace HouseOs.Tests.Features.Affichage;

public class DelaiReveilTests
{
    private static readonly AffichageOptions Options = new()
    {
        CadenceJourSecondes = 300,
        NuitDebut = new TimeOnly(22, 0),
        NuitFin = new TimeOnly(5, 30),
        PlafondSecondes = 3600,
    };

    [Fact]
    public void Le_jour_c_est_la_cadence()
    {
        Assert.Equal(300, DelaiReveil.Calculer(new DateTime(2026, 9, 3, 14, 30, 0), Options));
        Assert.Equal(300, DelaiReveil.Calculer(new DateTime(2026, 9, 3, 21, 59, 59), Options));
        Assert.Equal(300, DelaiReveil.Calculer(new DateTime(2026, 9, 3, 5, 30, 0), Options));
    }

    [Fact]
    public void La_nuit_on_dort_jusqu_au_matin_sous_le_plafond()
    {
        // 22 h → 5 h 30 = 7 h 30 : plafonné à une heure.
        Assert.Equal(3600, DelaiReveil.Calculer(new DateTime(2026, 9, 3, 22, 0, 0), Options));
        // 5 h → 5 h 30 : trente minutes exactement.
        Assert.Equal(1800, DelaiReveil.Calculer(new DateTime(2026, 9, 4, 5, 0, 0), Options));
        // 5 h 28 : deux minutes, mais jamais moins que la cadence du jour.
        Assert.Equal(300, DelaiReveil.Calculer(new DateTime(2026, 9, 4, 5, 28, 0), Options));
    }

    [Fact]
    public void Une_nuit_qui_ne_franchit_pas_minuit_marche_aussi()
    {
        var sieste = new AffichageOptions { NuitDebut = new TimeOnly(13, 0), NuitFin = new TimeOnly(14, 0) };
        Assert.True(DelaiReveil.EstLaNuit(new TimeOnly(13, 30), sieste.NuitDebut, sieste.NuitFin));
        Assert.False(DelaiReveil.EstLaNuit(new TimeOnly(23, 0), sieste.NuitDebut, sieste.NuitFin));
        Assert.False(DelaiReveil.EstLaNuit(new TimeOnly(23, 0), new TimeOnly(7, 0), new TimeOnly(7, 0)));
    }

    [Fact]
    public void Les_defauts_livres_menagent_la_pile()
    {
        // Régression : la cadence de 5 min du 2026-09-03 vidait la pile du reTerminal
        // en quelques semaines (0,25 V en 16 jours). Le réveil Wi-Fi est ce qui coûte.
        var defauts = new AffichageOptions();
        Assert.Equal(900, DelaiReveil.Calculer(new DateTime(2026, 9, 20, 14, 0, 0), defauts));
        // La nuit reste plafonnée : tant que 3600 s est la seule valeur éprouvée sur le
        // firmware, elle se découpe en sommeils d'une heure plutôt qu'un seul.
        Assert.Equal(3600, DelaiReveil.Calculer(new DateTime(2026, 9, 20, 22, 0, 0), defauts));
    }

    [Fact]
    public void Les_reglages_absurdes_sont_bornes()
    {
        var farfelu = new AffichageOptions { CadenceJourSecondes = 5, PlafondSecondes = 10 };
        // Plancher d'une minute : le firmware et la pile ne survivraient pas à 5 s.
        Assert.Equal(60, DelaiReveil.Calculer(new DateTime(2026, 9, 3, 14, 0, 0), farfelu));
    }
}
