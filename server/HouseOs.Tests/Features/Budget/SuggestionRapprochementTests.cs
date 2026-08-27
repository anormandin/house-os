using HouseOs.Api.Features.Budget;

namespace HouseOs.Tests.Features.Budget;

public class SuggestionRapprochementTests
{
    [Fact]
    public void Marchand_normalise_sans_chiffres_ni_dieses()
    {
        Assert.Equal(
            SuggestionRapprochement.NormaliserMarchand("CANADIAN TIRE #213"),
            SuggestionRapprochement.NormaliserMarchand("Canadian Tire #078"));
    }

    [Fact]
    public void Suggere_l_enveloppe_la_plus_frequente_du_meme_marchand()
    {
        var reserve = Guid.NewGuid();
        var bureau = Guid.NewGuid();
        var suggestion = SuggestionRapprochement.Suggerer("CANADIAN TIRE #300",
        [
            ("CANADIAN TIRE #213", reserve),
            ("CANADIAN TIRE #078", reserve),
            ("CANADIAN TIRE #213", bureau),
            ("BMR MATERIAUX", bureau),
        ]);
        Assert.Equal(reserve, suggestion);
    }

    [Fact]
    public void Marchand_jamais_vu_ne_suggere_rien()
    {
        Assert.Null(SuggestionRapprochement.Suggerer("HYDRO QUEBEC", [("BMR", Guid.NewGuid())]));
    }

    [Theory]
    [InlineData(675, 675, true)]
    [InlineData(650, 675, true)]     // −3,7 %
    [InlineData(742, 675, true)]     // +9,9 %
    [InlineData(745, 675, false)]    // +10,4 %
    [InlineData(100, 675, false)]
    [InlineData(675, 0, false)]      // aucun virement suggéré → jamais proche
    public void Depot_proche_du_virement_a_dix_pourcent(
        decimal depot, decimal virement, bool attendu) =>
        Assert.Equal(attendu, SuggestionRapprochement.EstProcheDuVirement(depot, virement));
}
