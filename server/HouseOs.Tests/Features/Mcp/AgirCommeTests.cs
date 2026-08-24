using HouseOs.Api.Features.Mcp;

namespace HouseOs.Tests.Features.Mcp;

public class AgirCommeTests
{
    private static readonly Guid AlainId = Guid.NewGuid();
    private static readonly Guid ArianeId = Guid.NewGuid();
    private static readonly (Guid, string)[] Duo = [(AlainId, "alain"), (ArianeId, "ariane")];

    [Fact]
    public void NomExact_EstResolu()
    {
        Assert.Equal(AlainId, AgirComme.Resoudre("alain", Duo));
    }

    [Fact]
    public void CasseEtEspaces_SontIgnores()
    {
        Assert.Equal(ArianeId, AgirComme.Resoudre("  ARIANE ", Duo));
    }

    [Fact]
    public void Null_DonneNull()
    {
        Assert.Null(AgirComme.Resoudre(null, Duo));
    }

    [Fact]
    public void Vide_DonneNull()
    {
        Assert.Null(AgirComme.Resoudre("  ", Duo));
    }

    [Fact]
    public void NomInconnu_DonneNull()
    {
        Assert.Null(AgirComme.Resoudre("bob", Duo));
    }
}
