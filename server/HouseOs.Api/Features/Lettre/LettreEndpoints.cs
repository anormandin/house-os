using System.Security.Claims;
using HouseOs.Api.Features.Affichage;
using HouseOs.Api.Features.Auth;
using HouseOs.Api.Features.FondsDeTiroir;
using HouseOs.Api.Features.Meteo;
using HouseOs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HouseOs.Api.Features.Lettre;

public record RegenererLettreRequete(string? Date, bool Envoyer);
public record EssaiLettreRequete(string? Date);

/// <summary>La lettre du matin pour les humains (vault : Features/Lettre Du Matin) ;
/// parité MCP : lire_lettre_du_matin, regenerer_lettre_du_matin. L'essai à moi n'a
/// pas d'équivalent MCP : il n'y a pas de « moi » en MCP.</summary>
public static class LettreEndpoints
{
    public static IEndpointRouteBuilder MapLettre(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/lettre", async (
            string? date, HouseOsDbContext db, IRedacteurLettre redacteur, IEnvoyeurDeCourriel envoyeur,
            IOptions<LettreOptions> options, IOptions<MeteoOptions> meteo, BanqueDuHasard banque,
            IOptions<AffichageOptions> affichage, ILoggerFactory fabrique, CancellationToken ct) =>
        {
            if (OperationsLettre.LireDate(date, out var jour) == false)
            {
                return Results.BadRequest(new { erreur = OperationsLettre.ErreurDate });
            }
            var maintenant = DateTime.Now;
            var c = Contexte(db, redacteur, envoyeur, options, meteo, banque, affichage, fabrique);
            return Results.Ok(await OperationsLettre.LireAsync(c, jour ?? DateOnly.FromDateTime(maintenant), maintenant, ct));
        });

        app.MapPost("/api/lettre/regenerer", async (
            RegenererLettreRequete requete, HouseOsDbContext db, IRedacteurLettre redacteur,
            IEnvoyeurDeCourriel envoyeur, IOptions<LettreOptions> options, IOptions<MeteoOptions> meteo,
            BanqueDuHasard banque, IOptions<AffichageOptions> affichage, ILoggerFactory fabrique,
            CancellationToken ct) =>
        {
            if (OperationsLettre.LireDate(requete.Date, out var jour) == false)
            {
                return Results.BadRequest(new { erreur = OperationsLettre.ErreurDate });
            }
            var maintenant = DateTime.Now;
            var c = Contexte(db, redacteur, envoyeur, options, meteo, banque, affichage, fabrique);
            return Results.Ok(await OperationsLettre.RegenererAsync(
                c, jour ?? DateOnly.FromDateTime(maintenant), requete.Envoyer, maintenant, ct));
        });

        app.MapPost("/api/lettre/essai", async (
            EssaiLettreRequete requete, ClaimsPrincipal principal, HouseOsDbContext db,
            IRedacteurLettre redacteur, IEnvoyeurDeCourriel envoyeur, IOptions<LettreOptions> options,
            IOptions<MeteoOptions> meteo, BanqueDuHasard banque, IOptions<AffichageOptions> affichage,
            ILoggerFactory fabrique, CancellationToken ct) =>
        {
            if (OperationsLettre.LireDate(requete.Date, out var jour) == false)
            {
                return Results.BadRequest(new { erreur = OperationsLettre.ErreurDate });
            }
            var moi = await db.Utilisateurs.AsNoTracking().SingleAsync(u => u.Id == principal.IdUtilisateur(), ct);
            if (string.IsNullOrWhiteSpace(moi.Courriel))
            {
                return Results.BadRequest(new { erreur = "Ton compte n'a pas d'adresse de courriel (COMPTE_n_COURRIEL)." });
            }
            var maintenant = DateTime.Now;
            var c = Contexte(db, redacteur, envoyeur, options, meteo, banque, affichage, fabrique);
            var adresse = await OperationsLettre.EssaiAsync(
                c, jour ?? DateOnly.FromDateTime(maintenant), new Destinataire(moi.NomAffichage, moi.Courriel), maintenant, ct);
            return adresse is null
                ? Results.BadRequest(new { erreur = "L'envoi de courriel n'est pas configuré (LETTRE_SMTP_*)." })
                : Results.Ok(new { envoyeA = adresse });
        });

        return app;
    }

    public static OperationsLettre.Contexte Contexte(
        HouseOsDbContext db, IRedacteurLettre redacteur, IEnvoyeurDeCourriel envoyeur,
        IOptions<LettreOptions> options, IOptions<MeteoOptions> meteo, BanqueDuHasard? banque,
        IOptions<AffichageOptions> affichage, ILoggerFactory fabrique) =>
        new(db, redacteur, envoyeur, options.Value, meteo.Value, banque, affichage.Value.Lieu,
            fabrique.CreateLogger("HouseOs.Lettre"));
}
