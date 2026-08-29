using System.Security.Claims;
using HouseOs.Api.Domaine;
using HouseOs.Api.Infrastructure;
using HouseOs.Api.Infrastructure.Journalisation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HouseOs.Api.Features.Auth;

public record ConnexionRequete(string NomUtilisateur, string MotDePasse);
public record UtilisateurDto(Guid Id, string NomUtilisateur, string NomAffichage);

public static class AuthEndpoints
{
    /// <summary>Politique de rate limiting de la connexion (fenêtre fixe par IP, Program.cs).</summary>
    public const string PolitiqueLimiteConnexion = "auth-connexion";

    public static IEndpointRouteBuilder MapAuth(this IEndpointRouteBuilder app)
    {
        var groupe = app.MapGroup("/api/auth");
        // Le seul journal à portée sécurité du système : qui entre, qui échoue, d'où.
        var journal = app.JournalPour("Auth");

        groupe.MapPost("/connexion", async (
            ConnexionRequete requete,
            HouseOsDbContext db,
            HttpContext http) =>
        {
            var adresse = http.Connection.RemoteIpAddress?.ToString();

            // Corps partiel ({} ou champ manquant) : le binding laisse les membres à
            // null malgré le type non-nullable — sans cette garde, NRE → 500 anonyme.
            if (string.IsNullOrWhiteSpace(requete.NomUtilisateur)
                || string.IsNullOrWhiteSpace(requete.MotDePasse))
            {
                return ResultatsApi.Erreur(
                    journal, "connexion", "Nom d'utilisateur et mot de passe requis.");
            }

            var nom = requete.NomUtilisateur.Trim().ToLowerInvariant();
            var utilisateur = await db.Utilisateurs.SingleOrDefaultAsync(u => u.NomUtilisateur == nom);
            if (utilisateur is null)
            {
                journal.LogWarning(
                    "Connexion refusée — compte {NomUtilisateur} inconnu, depuis {AdresseClient}.",
                    nom, adresse);
                return Results.Unauthorized();
            }

            var hasher = new PasswordHasher<Utilisateur>();
            var verdict = hasher.VerifyHashedPassword(utilisateur, utilisateur.MotDePasseHash, requete.MotDePasse);
            if (verdict == PasswordVerificationResult.Failed)
            {
                journal.LogWarning(
                    "Connexion refusée — mot de passe invalide pour {NomUtilisateur}, depuis {AdresseClient}.",
                    nom, adresse);
                return Results.Unauthorized();
            }
            if (verdict == PasswordVerificationResult.SuccessRehashNeeded)
            {
                // Paramètres de hachage renforcés depuis : réencoder pendant qu'on
                // tient le mot de passe en clair — seule occasion de le faire.
                utilisateur.MotDePasseHash = hasher.HashPassword(utilisateur, requete.MotDePasse);
                await db.SaveChangesAsync();
                journal.LogInformation(
                    "Mot de passe de {NomUtilisateur} réencodé avec les paramètres courants.", nom);
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, utilisateur.Id.ToString()),
                new(ClaimTypes.Name, utilisateur.NomAffichage),
            };
            var identite = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await http.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identite),
                new AuthenticationProperties { IsPersistent = true });

            journal.LogInformation(
                "Connexion réussie — {NomUtilisateur} ({UtilisateurId}) depuis {AdresseClient}.",
                nom, utilisateur.Id, adresse);
            return Results.Ok(new UtilisateurDto(utilisateur.Id, utilisateur.NomUtilisateur, utilisateur.NomAffichage));
        }).AllowAnonymous().RequireRateLimiting(PolitiqueLimiteConnexion);

        groupe.MapPost("/deconnexion", async (ClaimsPrincipal principal, HttpContext http) =>
        {
            journal.LogInformation("Déconnexion de {Utilisateur}.", principal.Identity?.Name);
            await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.NoContent();
        });

        groupe.MapGet("/moi", async (ClaimsPrincipal principal, HouseOsDbContext db, HttpContext http) =>
        {
            var id = principal.IdUtilisateur();
            var utilisateur = await db.Utilisateurs.FindAsync(id);
            if (utilisateur is null)
            {
                // Cookie valide mais compte disparu : purger le cookie, sinon le client
                // boucle sur un 401 impossible à sortir sans vider le navigateur.
                journal.LogWarning(
                    "Session orpheline — cookie valide pour {UtilisateurId}, compte introuvable ; cookie purgé.",
                    id);
                await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Results.Unauthorized();
            }
            return Results.Ok(new UtilisateurDto(utilisateur.Id, utilisateur.NomUtilisateur, utilisateur.NomAffichage));
        });

        return app;
    }

    public static Guid IdUtilisateur(this ClaimsPrincipal principal) =>
        Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
