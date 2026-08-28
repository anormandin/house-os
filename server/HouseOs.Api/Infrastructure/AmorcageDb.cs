using HouseOs.Api.Domaine;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HouseOs.Api.Infrastructure;

public record UtilisateurSeed(string NomUtilisateur, string NomAffichage, string MotDePasse = "");

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
            if (string.IsNullOrWhiteSpace(seed.MotDePasse))
            {
                // Jamais de compte à secret vide. Le mot de passe vient de
                // appsettings.Development.json en dev et de SEED_MDP_* (exigé par le
                // compose) en prod — ce garde-fou couvre les lancements hors compose.
                app.Logger.LogWarning(
                    "Amorçage : aucun mot de passe fourni pour {Nom} — compte non créé.", nom);
                continue;
            }
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
}
