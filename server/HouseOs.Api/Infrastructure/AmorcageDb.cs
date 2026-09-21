using HouseOs.Api.Domaine;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HouseOs.Api.Infrastructure;

public record UtilisateurSeed(
    string NomUtilisateur = "", string NomAffichage = "", string MotDePasse = "", string Courriel = "");

public static class AmorcageDb
{
    /// <summary>Applique les migrations puis crée les comptes manquants depuis la config Seed:Utilisateurs.</summary>
    public static async Task MigrerEtAmorcer(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HouseOsDbContext>();
        await db.Database.MigrateAsync();

        var seeds = app.Configuration.GetSection("Seed:Utilisateurs").Get<List<UtilisateurSeed>>() ?? [];
        await AmorcerUtilisateursAsync(db, seeds, app.Logger);
    }

    /// <summary>
    /// Crée les comptes manquants et aligne ce que le .env est seul à dire : l'adresse
    /// de courriel suit la config à chaque démarrage (vide = effacée), là où le mot de
    /// passe n'est posé qu'à la création (D-2026-09-21 Adresse De Courriel Sur
    /// L'Utilisateur).
    /// </summary>
    public static async Task AmorcerUtilisateursAsync(
        HouseOsDbContext db, IReadOnlyList<UtilisateurSeed> seeds, ILogger logger)
    {
        var hasher = new PasswordHasher<Utilisateur>();
        foreach (var seed in seeds)
        {
            if (string.IsNullOrWhiteSpace(seed.NomUtilisateur))
            {
                // Le compose déclare toujours deux entrées ; la seconde est facultative
                // (COMPTE_2_NOM vide) — une entrée sans nom n'est pas un compte.
                continue;
            }
            var nom = seed.NomUtilisateur.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(seed.MotDePasse))
            {
                // Jamais de compte à secret vide. Le mot de passe vient de
                // appsettings.Development.json en dev et de COMPTE_n_MDP (exigé par le
                // compose) en prod — ce garde-fou couvre les lancements hors compose.
                logger.LogWarning(
                    "Amorçage : aucun mot de passe fourni pour {Nom} — compte non créé.", nom);
                continue;
            }
            var courriel = CourrielNormalise(seed.Courriel);
            var existant = await db.Utilisateurs.SingleOrDefaultAsync(u => u.NomUtilisateur == nom);
            if (existant is not null)
            {
                existant.Courriel = courriel;
                continue;
            }

            var utilisateur = new Utilisateur
            {
                Id = Guid.NewGuid(),
                NomUtilisateur = nom,
                NomAffichage = string.IsNullOrWhiteSpace(seed.NomAffichage) ? seed.NomUtilisateur.Trim() : seed.NomAffichage,
                MotDePasseHash = string.Empty,
                Courriel = courriel,
            };
            utilisateur.MotDePasseHash = hasher.HashPassword(utilisateur, seed.MotDePasse);
            db.Utilisateurs.Add(utilisateur);
        }

        // Chaque compte reçoit un jeton iCal secret (URL du flux personnel).
        foreach (var utilisateur in await db.Utilisateurs.ToListAsync())
        {
            utilisateur.JetonIcal ??= Features.FluxIcal.JetonIcal.Generer();
        }
        foreach (var utilisateur in db.ChangeTracker.Entries<Utilisateur>()
                     .Where(e => e.State == EntityState.Added)
                     .Select(e => e.Entity))
        {
            utilisateur.JetonIcal ??= Features.FluxIcal.JetonIcal.Generer();
        }

        await db.SaveChangesAsync();
    }

    internal static string? CourrielNormalise(string? brut) =>
        string.IsNullOrWhiteSpace(brut) ? null : brut.Trim().ToLowerInvariant();
}
