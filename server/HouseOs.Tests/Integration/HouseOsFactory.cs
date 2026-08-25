using System.Net.Http.Json;
using HouseOs.Api.Features.Auth;
using HouseOs.Api.Features.FluxExternes;
using HouseOs.Api.Features.Humeur;
using HouseOs.Api.Features.Meteo;
using HouseOs.Api.Features.Taches;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;

namespace HouseOs.Tests.Integration;

/// <summary>
/// Hôte d'intégration : la vraie pile HTTP (auth cookie, DTO, ProblemDetails) sur un
/// Postgres Testcontainers (vraies migrations, vrai jsonb). Les services d'arrière-plan
/// (rollover, météo, humeur, flux externes) sont retirés — les tests pilotent tout via
/// l'API. Partagé par collection xunit : un seul conteneur pour toute la suite.
/// </summary>
public sealed class HouseOsFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string CleMcp = "cle-de-test-mcp";

    private readonly PostgreSqlContainer _postgres =
        new PostgreSqlBuilder("postgres:17-alpine").Build();

    /// <summary>Dossier fichiers isolé : sans lui, les tests d'upload écriraient
    /// dans donnees/fichiers du dépôt.</summary>
    public string DossierFichiers { get; } =
        Directory.CreateTempSubdirectory("houseos-fichiers-").FullName;

    public async Task InitializeAsync() => await _postgres.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
        Directory.Delete(DossierFichiers, recursive: true);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:HouseOs", _postgres.GetConnectionString());
        builder.UseSetting("Mcp:Cle", CleMcp);
        builder.UseSetting("Fichiers:Chemin", DossierFichiers);
        builder.UseSetting("Seed:Utilisateurs:0:NomUtilisateur", "alain");
        builder.UseSetting("Seed:Utilisateurs:0:NomAffichage", "Alain");
        builder.UseSetting("Seed:Utilisateurs:0:MotDePasse", "test-alain");
        builder.UseSetting("Seed:Utilisateurs:1:NomUtilisateur", "ariane");
        builder.UseSetting("Seed:Utilisateurs:1:NomAffichage", "Ariane");
        builder.UseSetting("Seed:Utilisateurs:1:MotDePasse", "test-ariane");

        builder.ConfigureTestServices(services =>
        {
            var arrierePlan = services
                .Where(d => d.ServiceType == typeof(IHostedService) &&
                    (d.ImplementationType == typeof(RolloverService) ||
                     d.ImplementationType == typeof(MeteoIngestionService) ||
                     d.ImplementationType == typeof(HumeurService) ||
                     d.ImplementationType == typeof(FluxExternesRafraichissement)))
                .ToList();
            foreach (var descripteur in arrierePlan)
            {
                services.Remove(descripteur);
            }
        });
    }

    /// <summary>Client avec une vraie session cookie (POST connexion).</summary>
    public async Task<HttpClient> ClientConnecte(string nomUtilisateur = "alain")
    {
        var client = CreateClient();
        var reponse = await client.PostAsJsonAsync(
            "/api/auth/connexion",
            new ConnexionRequete(nomUtilisateur, $"test-{nomUtilisateur}"));
        reponse.EnsureSuccessStatusCode();
        return client;
    }
}

[CollectionDefinition("integration")]
public sealed class CollectionIntegration : ICollectionFixture<HouseOsFactory>;
