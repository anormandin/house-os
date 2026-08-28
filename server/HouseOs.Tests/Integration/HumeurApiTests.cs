using System.Net;
using System.Net.Http.Json;
using HouseOs.Api.Domaine.Humeur;
using HouseOs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HouseOs.Tests.Integration;

/// <summary>
/// /api/phrase-du-jour lit la table matérialisée, sans jamais appeler de LLM dans le
/// chemin de requête : table vide (aucune clé Anthropic, service pas encore passé)
/// → 404 propre — le client retombe sur sa banque locale — jamais un 500.
/// </summary>
[Collection("integration")]
public class HumeurApiTests(HouseOsFactory factory)
{
    private sealed record Phrase(string Titre, string SousTitre, string Source, DateTimeOffset GenereLe);

    private async Task PurgerLesPhrases()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HouseOsDbContext>();
        await db.PhrasesDuJour.ExecuteDeleteAsync();
    }

    [Fact]
    public async Task SansPhraseMaterialisee_Repond404_JamaisUneErreur()
    {
        await PurgerLesPhrases();
        var client = await factory.ClientConnecte();

        var reponse = await client.GetAsync("/api/phrase-du-jour");

        Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
    }

    [Fact]
    public async Task AvecUnePhraseDuJour_LaSertAvecSaForme()
    {
        await PurgerLesPhrases();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HouseOsDbContext>();
            db.PhrasesDuJour.Add(new PhraseDuJour
            {
                Date = DateOnly.FromDateTime(DateTime.Now),
                Moment = MomentJournee.Matin,
                Titre = "La maison respire.",
                SousTitre = "Rien aujourd'hui.",
                Source = SourcePhrase.Gabarit,
                GenereLe = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync();
        }
        var client = await factory.ClientConnecte();

        var phrase = await client.GetFromJsonAsync<Phrase>("/api/phrase-du-jour");

        Assert.Equal("La maison respire.", phrase!.Titre);
        Assert.Equal("Rien aujourd'hui.", phrase.SousTitre);
        Assert.Equal("Gabarit", phrase.Source);
    }
}
