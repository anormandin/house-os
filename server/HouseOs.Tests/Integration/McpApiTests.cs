using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace HouseOs.Tests.Integration;

[Collection("integration")]
public class McpApiTests(HouseOsFactory factory)
{
    private static HttpRequestMessage RequetePing() => new(HttpMethod.Post, "/mcp")
    {
        Content = new StringContent(
            """{"jsonrpc":"2.0","id":1,"method":"ping"}""", Encoding.UTF8, "application/json"),
        Headers = { Accept = { new("application/json"), new("text/event-stream") } },
    };

    [Fact]
    public async Task SansCle_McpRepond401()
    {
        var reponse = await factory.CreateClient().SendAsync(RequetePing());
        Assert.Equal(HttpStatusCode.Unauthorized, reponse.StatusCode);
    }

    [Fact]
    public async Task AvecLaCle_LePingMcpRepond()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", HouseOsFactory.CleMcp);

        var reponse = await client.SendAsync(RequetePing());

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.Contains(""""result":{}"""", await reponse.Content.ReadAsStringAsync());
    }
}
