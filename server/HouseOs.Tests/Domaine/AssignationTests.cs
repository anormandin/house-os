using HouseOs.Api.Domaine;

namespace HouseOs.Tests.Domaine;

public class AssignationTests
{
    private static readonly Guid Ariane = Guid.NewGuid();
    private static readonly Guid Alain = Guid.NewGuid();
    private static readonly List<Guid> Duo = [Ariane, Alain];

    private static Dictionary<Guid, int> Comptes(int ariane, int alain) => new()
    {
        [Ariane] = ariane,
        [Alain] = alain,
    };

    [Fact]
    public void Fixe_RetourneLAssigneParDefaut()
    {
        var choisi = Assignation.ChoisirAssigne(
            StrategieAssignation.Fixe, Alain, Ariane, Duo, Comptes(0, 0));
        Assert.Equal(Alain, choisi);
    }

    [Fact]
    public void Fixe_SansDefaut_RetourneNull()
    {
        var choisi = Assignation.ChoisirAssigne(
            StrategieAssignation.Fixe, null, Ariane, Duo, Comptes(0, 0));
        Assert.Null(choisi);
    }

    [Fact]
    public void Alternance_LAutreQueLeDernierCompleteur()
    {
        var choisi = Assignation.ChoisirAssigne(
            StrategieAssignation.Alternance, null, Ariane, Duo, Comptes(0, 0));
        Assert.Equal(Alain, choisi);
    }

    [Fact]
    public void Alternance_SansHistorique_RepliSurLeDefaut()
    {
        var choisi = Assignation.ChoisirAssigne(
            StrategieAssignation.Alternance, Ariane, null, Duo, Comptes(0, 0));
        Assert.Equal(Ariane, choisi);
    }

    [Fact]
    public void MoinsLAFait_CeluiQuiEnAFaitLeMoins()
    {
        var choisi = Assignation.ChoisirAssigne(
            StrategieAssignation.MoinsLAFait, null, null, Duo, Comptes(5, 2));
        Assert.Equal(Alain, choisi);
    }

    [Fact]
    public void MoinsLAFait_Egalite_Alterne()
    {
        // 3–3, Alain a complété en dernier → Ariane
        var choisi = Assignation.ChoisirAssigne(
            StrategieAssignation.MoinsLAFait, null, Alain, Duo, Comptes(3, 3));
        Assert.Equal(Ariane, choisi);
    }

    [Fact]
    public void MoinsLAFait_UtilisateurSansCompletion_CompteZero()
    {
        var comptes = new Dictionary<Guid, int> { [Ariane] = 4 };
        var choisi = Assignation.ChoisirAssigne(
            StrategieAssignation.MoinsLAFait, null, null, Duo, comptes);
        Assert.Equal(Alain, choisi);
    }

    [Theory]
    [InlineData(StrategieAssignation.Alternance)]
    [InlineData(StrategieAssignation.MoinsLAFait)]
    public void ListeDUtilisateursVide_RetourneNull(StrategieAssignation strategie)
    {
        var choisi = Assignation.ChoisirAssigne(
            strategie, Alain, Alain, [], new Dictionary<Guid, int>());
        Assert.Null(choisi);
    }

    [Fact]
    public void Alternance_DernierCompleteurHorsListe_PrendLeDefautPuisLePremier()
    {
        var inconnu = Guid.NewGuid();
        // Le dernier compléteur n'est pas un candidat : « l'autre » = toute la liste,
        // donc le premier candidat sort.
        var choisi = Assignation.ChoisirAssigne(
            StrategieAssignation.Alternance, null, inconnu, Duo, new Dictionary<Guid, int>());
        Assert.Equal(Duo[0], choisi);
    }

    [Fact]
    public void Alternance_UtilisateurUnique_SeReassigneLuiMeme()
    {
        // Cas dégénéré à un seul compte : personne d'autre — le repli retombe sur lui.
        var choisi = Assignation.ChoisirAssigne(
            StrategieAssignation.Alternance, Alain, Alain, [Alain], new Dictionary<Guid, int>());
        Assert.Equal(Alain, choisi);
    }
}
