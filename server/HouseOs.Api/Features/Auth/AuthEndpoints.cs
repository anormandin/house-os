using System.Security.Claims;
using HouseOs.Api.Domaine;
using HouseOs.Api.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HouseOs.Api.Features.Auth;

public record ConnexionRequete(string NomUtilisateur, string MotDePasse);
public record UtilisateurDto(Guid Id, string NomUtilisateur, string NomAffichage);

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuth(this IEndpointRouteBuilder app)
    {
        var groupe = app.MapGroup("/api/auth");

        groupe.MapPost("/connexion", async (
            ConnexionRequete requete,
            HouseOsDbContext db,
            HttpContext http) =>
        {
            // Corps partiel ({} ou champ manquant) : le binding laisse les membres à
            // null malgré le type non-nullable — sans cette garde, NRE → 500 anonyme.
            if (string.IsNullOrWhiteSpace(requete.NomUtilisateur)
                || string.IsNullOrWhiteSpace(requete.MotDePasse))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["connexion"] = ["Nom d'utilisateur et mot de passe requis."],
                });
            }

            var nom = requete.NomUtilisateur.Trim().ToLowerInvariant();
            var utilisateur = await db.Utilisateurs.SingleOrDefaultAsync(u => u.NomUtilisateur == nom);
            if (utilisateur is null)
            {
                return Results.Unauthorized();
            }

            var hasher = new PasswordHasher<Utilisateur>();
            var verdict = hasher.VerifyHashedPassword(utilisateur, utilisateur.MotDePasseHash, requete.MotDePasse);
            if (verdict == PasswordVerificationResult.Failed)
            {
                return Results.Unauthorized();
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

            return Results.Ok(new UtilisateurDto(utilisateur.Id, utilisateur.NomUtilisateur, utilisateur.NomAffichage));
        }).AllowAnonymous();

        groupe.MapPost("/deconnexion", async (HttpContext http) =>
        {
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
