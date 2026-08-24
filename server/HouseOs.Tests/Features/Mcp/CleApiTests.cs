using HouseOs.Api.Features.Mcp;

namespace HouseOs.Tests.Features.Mcp;

public class CleApiTests
{
    [Fact]
    public void ClesIdentiques_SontEgales()
    {
        Assert.True(AuthentificationCleApiHandler.ClesEgales("secret-123", "secret-123"));
    }

    [Fact]
    public void ClesDifferentes_SontRefusees()
    {
        Assert.False(AuthentificationCleApiHandler.ClesEgales("secret-123", "secret-456"));
    }

    [Fact]
    public void LongueursDifferentes_SontRefusees()
    {
        Assert.False(AuthentificationCleApiHandler.ClesEgales("secret", "secret-123"));
    }

    [Fact]
    public void CleConfigureeVide_RefuseTout()
    {
        Assert.False(AuthentificationCleApiHandler.ClesEgales("secret", ""));
        Assert.False(AuthentificationCleApiHandler.ClesEgales("", ""));
    }

    [Fact]
    public void CleConfigureeNulle_RefuseTout()
    {
        Assert.False(AuthentificationCleApiHandler.ClesEgales("secret", null));
    }

    [Fact]
    public void CleFournieVide_EstRefusee()
    {
        Assert.False(AuthentificationCleApiHandler.ClesEgales("", "secret"));
        Assert.False(AuthentificationCleApiHandler.ClesEgales(null, "secret"));
    }
}
