using HouseOs.Api.Features.Lettre;
using MailKit.Security;

namespace HouseOs.Tests.Features.Lettre;

/// <summary>La porte de sortie : ce qui l'ouvre, et ce que le port commande.</summary>
public class EnvoyeurSmtpTests
{
    [Fact]
    public void Sans_hote_ni_expediteur_la_porte_est_fermee()
    {
        Assert.False(new SmtpOptions().Actif);
        Assert.False(new SmtpOptions { Hote = "smtp.exemple.tld" }.Actif);
        Assert.False(new SmtpOptions { Expediteur = "maison@exemple.tld" }.Actif);
        Assert.True(new SmtpOptions { Hote = "smtp.exemple.tld", Expediteur = "maison@exemple.tld" }.Actif);
    }

    [Fact]
    public void Un_relais_sans_usager_se_connecte_sans_authentification()
    {
        Assert.False(new SmtpOptions { Hote = "relais.local", Expediteur = "maison@exemple.tld" }.AvecAuthentification);
        Assert.True(new SmtpOptions { Usager = "maison" }.AvecAuthentification);
    }

    [Theory]
    [InlineData(465, SecureSocketOptions.SslOnConnect)]
    [InlineData(587, SecureSocketOptions.StartTls)]
    [InlineData(25, SecureSocketOptions.Auto)]
    [InlineData(2525, SecureSocketOptions.Auto)]
    public void Le_port_commande_le_TLS(int port, SecureSocketOptions attendu) =>
        Assert.Equal(attendu, EnvoyeurSmtp.OptionsDeSocket(port));

    [Theory]
    [InlineData("La maison <maison@exemple.tld>", "La maison", "maison@exemple.tld")]
    [InlineData("maison@exemple.tld", "", "maison@exemple.tld")]
    public void L_expediteur_se_lit_avec_ou_sans_nom(string brut, string nom, string adresse)
    {
        var lu = EnvoyeurSmtp.LireExpediteur(brut);
        Assert.Equal(nom, lu.Name ?? "");
        Assert.Equal(adresse, lu.Address);
    }

    [Theory]
    [InlineData("")]
    [InlineData("la maison")]
    public void Un_expediteur_qui_n_est_pas_une_adresse_est_refuse(string brut) =>
        Assert.Throws<InvalidOperationException>(() => EnvoyeurSmtp.LireExpediteur(brut));

    [Fact]
    public async Task Sans_destinataire_rien_ne_part()
    {
        var envoyeur = new EnvoyeurSmtp(
            new SmtpOptions { Hote = "smtp.exemple.tld", Expediteur = "maison@exemple.tld" },
            Microsoft.Extensions.Logging.Abstractions.NullLogger<EnvoyeurSmtp>.Instance);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            envoyeur.EnvoyerAsync(new CourrielAEnvoyer([], "Sujet", "texte", "<p>html</p>"), CancellationToken.None));
    }

    [Fact]
    public async Task L_envoyeur_inactif_ne_promet_rien()
    {
        var inactif = new EnvoyeurInactif();
        Assert.False(inactif.Actif);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            inactif.EnvoyerAsync(new CourrielAEnvoyer([new("A", "a@x")], "s", "t", "h"), CancellationToken.None));
    }
}
