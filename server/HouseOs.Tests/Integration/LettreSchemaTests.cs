using System.Net.Http.Json;
using HouseOs.Api.Domaine.Lettre;
using HouseOs.Api.Features.Auth;
using HouseOs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace HouseOs.Tests.Integration;

/// <summary>Le schéma de la lettre du matin : l'adresse sur l'utilisateur, et une ligne
/// par date (D-2026-09-21 Adresse De Courriel Sur L'Utilisateur, D-2026-09-21 Lettre
/// Matérialisée Et Rattrapée Le Jour Même).</summary>
[Collection("integration")]
public class LettreSchemaTests(HouseOsFactory factory)
{
    [Fact]
    public async Task L_adresse_seedee_est_servie_normalisee_et_nulle_sans_config()
    {
        var client = await factory.ClientConnecte();
        var utilisateurs = (await client.GetFromJsonAsync<List<UtilisateurDto>>("/api/utilisateurs"))!;

        Assert.Equal("alain@exemple.tld", utilisateurs.Single(u => u.NomUtilisateur == "alain").Courriel);
        Assert.Null(utilisateurs.Single(u => u.NomUtilisateur == "ariane").Courriel);
    }

    [Fact]
    public async Task L_adresse_suit_la_config_a_chaque_amorcage_sans_toucher_au_mot_de_passe()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HouseOsDbContext>();
        var avant = await db.Utilisateurs.AsNoTracking().SingleAsync(u => u.NomUtilisateur == "ariane");

        await AmorcageDb.AmorcerUtilisateursAsync(db,
            [new UtilisateurSeed("ariane", "Ariane", "autre-mot-de-passe", " Ariane@Exemple.tld ")],
            NullLogger.Instance);
        var avec = await db.Utilisateurs.AsNoTracking().SingleAsync(u => u.NomUtilisateur == "ariane");
        Assert.Equal("ariane@exemple.tld", avec.Courriel);
        Assert.Equal(avant.MotDePasseHash, avec.MotDePasseHash);

        await AmorcageDb.AmorcerUtilisateursAsync(db,
            [new UtilisateurSeed("ariane", "Ariane", "autre-mot-de-passe", "")],
            NullLogger.Instance);
        var sans = await db.Utilisateurs.AsNoTracking().SingleAsync(u => u.NomUtilisateur == "ariane");
        Assert.Null(sans.Courriel);
    }

    [Fact]
    public async Task Une_seule_lettre_par_date()
    {
        var date = new DateOnly(2031, 1, 15);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HouseOsDbContext>();
            db.Lettres.Add(new LettreDuMatin
            {
                Date = date, Sujet = "Première", Paragraphes = ["a", "b", "c"],
                Source = SourceLettre.Gabarit, ComposeeLe = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync();
        }
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HouseOsDbContext>();
            db.Lettres.Add(new LettreDuMatin
            {
                Date = date, Sujet = "Seconde", Paragraphes = ["a", "b", "c"],
                Source = SourceLettre.Gabarit, ComposeeLe = DateTimeOffset.UtcNow,
            });
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HouseOsDbContext>();
            var relue = await db.Lettres.SingleAsync(l => l.Date == date);
            Assert.Equal(["a", "b", "c"], relue.Paragraphes);
            Assert.False(relue.Envoyee);
            db.Lettres.Remove(relue);
            await db.SaveChangesAsync();
        }
    }
}
