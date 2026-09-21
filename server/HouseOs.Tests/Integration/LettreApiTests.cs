using System.Net;
using System.Net.Http.Json;
using HouseOs.Api.Domaine.Lettre;
using HouseOs.Api.Features.Affichage;
using HouseOs.Api.Features.FondsDeTiroir;
using HouseOs.Api.Features.Lettre;
using HouseOs.Api.Features.Mcp;
using HouseOs.Api.Features.Meteo;
using HouseOs.Api.Infrastructure;
using HouseOs.Tests.Features.Lettre;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HouseOs.Tests.Integration;

/// <summary>La lettre pour les humains et pour le MCP, sur les mêmes fictifs : lire,
/// réécrire, envoyer, l'essai à moi.</summary>
[Collection("integration")]
public class LettreApiTests(HouseOsFactory factory)
{
    private static readonly TexteDeLettre Prose = new("Demain, le camion",
        ["Un dodo. Demain matin, le camion, et aujourd'hui une seule chose qui ne se négocie pas : la garde de l'animal.",
         "Dehors, trois degrés ce matin et onze cet après-midi, du soleil, assez pour charger et pas assez pour flâner.",
         "Demain, une autre maison prend le relais ; couchez-vous tôt, c'est tout ce que je demande."]);

    private RedacteurLettreFictif Redacteur => factory.Services.GetRequiredService<RedacteurLettreFictif>();
    private EnvoyeurFictif Envoyeur => factory.Services.GetRequiredService<EnvoyeurFictif>();

    private async Task Nettoyer(DateOnly date)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HouseOsDbContext>();
        await db.Lettres.Where(l => l.Date == date).ExecuteDeleteAsync();
        Envoyeur.Envoyes.Clear();
        Envoyeur.Actif = true;
    }

    [Fact]
    public async Task Sans_lettre_ecrite_le_GET_rend_un_apercu_en_note_sans_appeler_le_modele()
    {
        var date = new DateOnly(2031, 4, 1);
        await Nettoyer(date);
        var client = await factory.ClientConnecte();
        var appels = Redacteur.Appels;

        var lettre = (await client.GetFromJsonAsync<LettreDto>($"/api/lettre?date={date:yyyy-MM-dd}"))!;

        Assert.False(lettre.Ecrite);
        Assert.Equal("Gabarit", lettre.Source);
        Assert.Equal(appels, Redacteur.Appels);
        Assert.Contains("Bonjour vous deux.", lettre.Texte);
        Assert.Equal(["alain@exemple.tld"], lettre.DestinatairesPrevus);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/lettre?date=hier")).StatusCode);
    }

    [Fact]
    public async Task Regenerer_ecrit_la_lettre_et_l_envoie_seulement_si_demande()
    {
        var date = new DateOnly(2031, 4, 2);
        await Nettoyer(date);
        var client = await factory.ClientConnecte();
        Redacteur.Texte = Prose;

        var ecrite = (await (await client.PostAsJsonAsync("/api/lettre/regenerer",
            new RegenererLettreRequete(date.ToString("yyyy-MM-dd"), false))).Content.ReadFromJsonAsync<LettreDto>())!;
        Assert.True(ecrite.Ecrite);
        Assert.Equal("Llm", ecrite.Source);
        Assert.Equal("Demain, le camion", ecrite.Sujet);
        Assert.Null(ecrite.EnvoyeeLe);
        Assert.Empty(Envoyeur.Envoyes);

        var relue = (await client.GetFromJsonAsync<LettreDto>($"/api/lettre?date={date:yyyy-MM-dd}"))!;
        Assert.True(relue.Ecrite);
        Assert.Equal("Demain, le camion", relue.Sujet);

        var envoyee = (await (await client.PostAsJsonAsync("/api/lettre/regenerer",
            new RegenererLettreRequete(date.ToString("yyyy-MM-dd"), true))).Content.ReadFromJsonAsync<LettreDto>())!;
        Assert.NotNull(envoyee.EnvoyeeLe);
        Assert.Equal(["alain@exemple.tld"], envoyee.Destinataires);
        var courriel = Assert.Single(Envoyeur.Envoyes);
        Assert.Equal("Demain, le camion", courriel.Sujet);
        Assert.Contains("Ouvrir la journée", courriel.Texte);
        await Nettoyer(date);
    }

    [Fact]
    public async Task L_essai_part_a_moi_seulement_et_ne_marque_pas_la_journee()
    {
        var date = new DateOnly(2031, 4, 3);
        await Nettoyer(date);
        var client = await factory.ClientConnecte();
        Redacteur.Texte = Prose;

        var reponse = await client.PostAsJsonAsync("/api/lettre/essai", new EssaiLettreRequete(date.ToString("yyyy-MM-dd")));
        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        var courriel = Assert.Single(Envoyeur.Envoyes);
        Assert.Equal(["alain@exemple.tld"], courriel.Destinataires.Select(d => d.Adresse));
        var lettre = (await client.GetFromJsonAsync<LettreDto>($"/api/lettre?date={date:yyyy-MM-dd}"))!;
        Assert.False(lettre.Ecrite);

        // Ariane n'a pas d'adresse : l'essai le dit plutôt que de se taire.
        var ariane = await factory.ClientConnecte("ariane");
        Assert.Equal(HttpStatusCode.BadRequest,
            (await ariane.PostAsJsonAsync("/api/lettre/essai", new EssaiLettreRequete(null))).StatusCode);

        Envoyeur.Actif = false;
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync("/api/lettre/essai", new EssaiLettreRequete(date.ToString("yyyy-MM-dd")))).StatusCode);
        await Nettoyer(date);
    }

    [Fact]
    public async Task Le_MCP_lit_et_reecrit_la_meme_lettre_que_l_API()
    {
        var date = new DateOnly(2031, 4, 4);
        await Nettoyer(date);
        Redacteur.Texte = Prose;
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<HouseOsDbContext>();

        var apercu = await OutilsMaison.LireLettreDuMatin(
            db, Redacteur, Envoyeur, sp.GetRequiredService<IOptions<LettreOptions>>(),
            sp.GetRequiredService<IOptions<MeteoOptions>>(), sp.GetRequiredService<BanqueDuHasard>(),
            sp.GetRequiredService<IOptions<AffichageOptions>>(), sp.GetRequiredService<ILoggerFactory>(),
            CancellationToken.None, date.ToString("yyyy-MM-dd"));
        Assert.False(apercu.Ecrite);

        var ecrite = await OutilsMaison.RegenererLettreDuMatin(
            db, Redacteur, Envoyeur, sp.GetRequiredService<IOptions<LettreOptions>>(),
            sp.GetRequiredService<IOptions<MeteoOptions>>(), sp.GetRequiredService<BanqueDuHasard>(),
            sp.GetRequiredService<IOptions<AffichageOptions>>(), sp.GetRequiredService<ILoggerFactory>(),
            CancellationToken.None, date.ToString("yyyy-MM-dd"), envoyer: true);
        Assert.True(ecrite.Ecrite);
        Assert.NotNull(ecrite.EnvoyeeLe);
        Assert.Single(Envoyeur.Envoyes);

        var client = await factory.ClientConnecte();
        var parApi = (await client.GetFromJsonAsync<LettreDto>($"/api/lettre?date={date:yyyy-MM-dd}"))!;
        Assert.Equal(ecrite.Sujet, parApi.Sujet);
        Assert.Equal(ecrite.EnvoyeeLe, parApi.EnvoyeeLe);
        await Nettoyer(date);
    }
}
