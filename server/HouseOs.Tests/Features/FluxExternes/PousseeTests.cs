using HouseOs.Api.Domaine;
using HouseOs.Api.Features.FluxExternes;

namespace HouseOs.Tests.Features.FluxExternes;

/// <summary>
/// La règle d'acceptation d'une poussée, sans base ni HTTP
/// (vault : D-2026-09-20 Flux Externe Poussé).
/// </summary>
public class PousseeTests
{
    private static FluxExterne Flux(SourceFluxExterne source) =>
        new() { Id = Guid.NewGuid(), Nom = "Ville", Url = null, Source = source };

    private static List<EvenementPousse> Evenements(int combien) =>
        [.. Enumerable.Range(0, combien).Select(i =>
            new EvenementPousse($"Événement {i}", new DateOnly(2026, 10, 1)))];

    [Fact]
    public void Un_flux_pousse_accepte_une_liste_vide()
    {
        // « La ville n'annonce rien cette semaine » est une réponse, pas une panne.
        Assert.Null(OperationsPoussee.PourquoiRefuser(Flux(SourceFluxExterne.Poussee), []));
    }

    [Fact]
    public void Un_abonnement_ics_refuse_la_poussee()
    {
        var refus = OperationsPoussee.PourquoiRefuser(Flux(SourceFluxExterne.Ics), Evenements(1));

        // Accepter viderait le flux à la prochaine passe de six heures.
        Assert.NotNull(refus);
        Assert.Contains("abonnement iCal", refus);
    }

    [Fact]
    public void Au_dela_du_plafond_ce_n_est_plus_un_calendrier_de_ville()
    {
        var flux = Flux(SourceFluxExterne.Poussee);

        Assert.Null(OperationsPoussee.PourquoiRefuser(flux, Evenements(OperationsPoussee.MaxEvenements)));
        var refus = OperationsPoussee.PourquoiRefuser(flux, Evenements(OperationsPoussee.MaxEvenements + 1));
        Assert.NotNull(refus);
        Assert.Contains($"{OperationsPoussee.MaxEvenements}", refus);
    }
}
