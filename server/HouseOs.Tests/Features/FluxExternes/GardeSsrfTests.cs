using System.Net;
using HouseOs.Api.Features.FluxExternes;

namespace HouseOs.Tests.Features.FluxExternes;

/// <summary>
/// La garde SSRF du téléchargement ICS : adresses internes refusées (URL directe et
/// redirections), calendriers publics acceptés, redirections bornées — sans jamais
/// toucher au réseau (hôtes en IP littérale + handler interne factice).
/// </summary>
public class GardeSsrfTests
{
    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("127.8.8.8")]
    [InlineData("10.0.0.5")]
    [InlineData("172.16.0.1")]
    [InlineData("172.31.255.254")]
    [InlineData("192.168.4.50")]
    [InlineData("169.254.169.254")]
    [InlineData("100.64.0.1")]     // CGNAT — la plage Tailscale
    [InlineData("100.127.255.254")]
    [InlineData("0.0.0.0")]
    [InlineData("224.0.0.1")]      // multicast
    [InlineData("255.255.255.255")]
    [InlineData("::1")]
    [InlineData("fe80::1")]
    [InlineData("fd00::1")]        // unique local
    [InlineData("ff02::1")]        // multicast v6
    [InlineData("::ffff:192.168.1.1")]
    public void Les_adresses_internes_sont_interdites(string adresse) =>
        Assert.True(GardeSsrfHandler.EstAdresseInterne(IPAddress.Parse(adresse)));

    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("203.0.113.10")]
    [InlineData("172.15.0.1")]     // juste sous 172.16/12
    [InlineData("172.32.0.1")]     // juste au-dessus
    [InlineData("100.63.255.254")] // juste sous 100.64/10
    [InlineData("100.128.0.1")]    // juste au-dessus
    [InlineData("2606:4700::1111")]
    public void Les_adresses_publiques_sont_permises(string adresse) =>
        Assert.False(GardeSsrfHandler.EstAdresseInterne(IPAddress.Parse(adresse)));

    private sealed class ReponsesEnSequence(params HttpResponseMessage[] reponses) : HttpMessageHandler
    {
        private int _index;
        public List<Uri> UrisVues { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage requete, CancellationToken ct)
        {
            UrisVues.Add(requete.RequestUri!);
            return Task.FromResult(reponses[Math.Min(_index++, reponses.Length - 1)]);
        }
    }

    private static HttpResponseMessage Redirection(string vers) =>
        new(HttpStatusCode.Found) { Headers = { Location = new Uri(vers) } };

    private static HttpClient Client(ReponsesEnSequence interne) =>
        new(new GardeSsrfHandler { InnerHandler = interne });

    [Fact]
    public async Task Une_url_interne_est_refusee_avant_tout_appel()
    {
        var interne = new ReponsesEnSequence(new HttpResponseMessage(HttpStatusCode.OK));

        var ex = await Assert.ThrowsAsync<HttpRequestException>(
            () => Client(interne).GetStringAsync("http://192.168.4.50/cal.ics"));

        Assert.Contains("réseau interne", ex.Message);
        Assert.Empty(interne.UrisVues); // rien n'est parti sur le fil
    }

    [Fact]
    public async Task Une_url_publique_passe()
    {
        var interne = new ReponsesEnSequence(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("BEGIN:VCALENDAR"),
        });

        var corps = await Client(interne).GetStringAsync("http://203.0.113.10/cal.ics");

        Assert.Equal("BEGIN:VCALENDAR", corps);
    }

    [Fact]
    public async Task Une_redirection_vers_une_adresse_interne_est_refusee()
    {
        // Le vecteur classique : une URL publique qui redirige vers le LAN.
        var interne = new ReponsesEnSequence(
            Redirection("http://127.0.0.1/secret"),
            new HttpResponseMessage(HttpStatusCode.OK));

        var ex = await Assert.ThrowsAsync<HttpRequestException>(
            () => Client(interne).GetStringAsync("http://203.0.113.10/cal.ics"));

        Assert.Contains("réseau interne", ex.Message);
        Assert.Single(interne.UrisVues); // la cible interne n'a jamais été appelée
    }

    [Fact]
    public async Task Les_redirections_sont_suivies_puis_bornees()
    {
        var interne = new ReponsesEnSequence(
            Redirection("http://203.0.113.11/a"),
            Redirection("http://203.0.113.12/b"),
            Redirection("http://203.0.113.13/c"),
            Redirection("http://203.0.113.14/d"));

        var ex = await Assert.ThrowsAsync<HttpRequestException>(
            () => Client(interne).GetStringAsync("http://203.0.113.10/cal.ics"));

        Assert.Contains("redirections", ex.Message);
        Assert.Equal(1 + GardeSsrfHandler.MaxRedirections, interne.UrisVues.Count);
    }

    [Fact]
    public async Task Une_redirection_normale_aboutit()
    {
        var interne = new ReponsesEnSequence(
            Redirection("http://203.0.113.11/vrai.ics"),
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") });

        var corps = await Client(interne).GetStringAsync("http://203.0.113.10/cal.ics");

        Assert.Equal("ok", corps);
        Assert.Equal(new Uri("http://203.0.113.11/vrai.ics"), interne.UrisVues[^1]);
    }
}
