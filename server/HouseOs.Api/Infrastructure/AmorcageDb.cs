using HouseOs.Api.Domaine;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HouseOs.Api.Infrastructure;

public record UtilisateurSeed(string NomUtilisateur, string NomAffichage, string MotDePasse);

public static class AmorcageDb
{
    /// <summary>Applique les migrations puis crée les comptes manquants depuis la config Seed:Utilisateurs.</summary>
    public static async Task MigrerEtAmorcer(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HouseOsDbContext>();
        await db.Database.MigrateAsync();

        var seeds = app.Configuration.GetSection("Seed:Utilisateurs").Get<List<UtilisateurSeed>>() ?? [];
        var hasher = new PasswordHasher<Utilisateur>();
        foreach (var seed in seeds)
        {
            var nom = seed.NomUtilisateur.ToLowerInvariant();
            var existe = await db.Utilisateurs.AnyAsync(u => u.NomUtilisateur == nom);
            if (existe)
            {
                continue;
            }

            var utilisateur = new Utilisateur
            {
                Id = Guid.NewGuid(),
                NomUtilisateur = nom,
                NomAffichage = seed.NomAffichage,
                MotDePasseHash = string.Empty,
            };
            utilisateur.MotDePasseHash = hasher.HashPassword(utilisateur, seed.MotDePasse);
            db.Utilisateurs.Add(utilisateur);
        }

        // Chaque compte reçoit un jeton iCal secret (URL du flux personnel).
        foreach (var utilisateur in await db.Utilisateurs.ToListAsync())
        {
            utilisateur.JetonIcal ??= GenererJeton();
        }
        foreach (var utilisateur in db.ChangeTracker.Entries<Utilisateur>()
                     .Where(e => e.State == EntityState.Added)
                     .Select(e => e.Entity))
        {
            utilisateur.JetonIcal ??= GenererJeton();
        }

        await db.SaveChangesAsync();
    }

    private static string GenererJeton() =>
        Convert.ToHexStringLower(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24));
}
