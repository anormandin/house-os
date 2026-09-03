using HouseOs.Api.Infrastructure;

namespace HouseOs.Api.Features.Affichage;

/// <summary>
/// La vue e-ink (vault : Features/Affichage E-ink) : les données de la page /ecran,
/// l'aperçu du bitmap tel que l'appareil le recevra ; les routes du protocole TRMNL
/// suivent.
/// </summary>
public record RenommerAppareilRequete(string? Nom);

public static class AffichageEndpoints
{
    public static IEndpointRouteBuilder MapAffichage(this IEndpointRouteBuilder app)
    {
        // Anonyme pour la policy globale, mais gardé à la main : le cookie d'un humain
        // (aperçu dans le navigateur) ou le jeton du navigateur de rendu du serveur.
        app.MapGet("/api/affichage/donnees", async (
            HttpContext contexte, JetonRendu jeton, HouseOsDbContext db) =>
        {
            if (jeton.Autorise(contexte) == false)
            {
                return Results.Unauthorized();
            }
            return Results.Ok(await ComposerDonneesEcran.LireAsync(db, DateTime.Now));
        }).AllowAnonymous();

        // L'outil de conception avant la livraison, de diagnostic ensuite : le PNG
        // exact (seuillé) — ou la capture brute avec ?brut=1 pour comparer.
        app.MapGet("/api/affichage/apercu.png", async (
            int? largeur, int? hauteur, int? pile, string? brut, string? accueil,
            IRenduEcran rendu, ILoggerFactory fabrique, CancellationToken ct) =>
        {
            var (l, h) = Dimensions(largeur, hauteur);
            // « brut=1 » comme « brut=true » : un humain tape ça dans une barre d'adresse.
            var sansSeuillage = brut is "1" or "true";
            try
            {
                var capture = await rendu.CapturerAsync(new DemandeCapture(l, h, pile, accueil), ct);
                return Results.File(sansSeuillage ? capture : Seuillage.EnUnBit(capture), "image/png");
            }
            catch (RenduEcranException ex)
            {
                fabrique.CreateLogger("HouseOs.Affichage").LogError(ex, "Aperçu e-ink impossible.");
                return Results.Problem(title: "Rendu de l'écran impossible", detail: ex.Message,
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        });

        // Les appareils enrôlés, pour les humains (parité MCP : lister_appareils_affichage,
        // gerer_appareil_affichage).
        app.MapGet("/api/affichage/appareils", async (HouseOsDbContext db) =>
            Results.Ok(await OperationsAppareils.ListerAsync(db)));

        app.MapPut("/api/affichage/appareils/{id:guid}", async (
            Guid id, RenommerAppareilRequete requete, HouseOsDbContext db) =>
        {
            var dto = await OperationsAppareils.RenommerAsync(db, id, requete.Nom);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        });

        app.MapDelete("/api/affichage/appareils/{id:guid}", async (
            Guid id, HouseOsDbContext db, CacheImages cache, ILoggerFactory fabrique) =>
        {
            if (await OperationsAppareils.SupprimerAsync(db, id) == false)
            {
                return Results.NotFound();
            }
            cache.Oublier(id);
            fabrique.CreateLogger("HouseOs.Affichage").LogInformation("Appareil {AppareilId} révoqué.", id);
            return Results.NoContent();
        });

        app.MapProtocoleTrmnl();
        return app;
    }

    /// <summary>Taille demandée bornée au raisonnable ; sans indication, l'E1003 en paysage.</summary>
    public static (int Largeur, int Hauteur) Dimensions(int? largeur, int? hauteur) => (
        Math.Clamp(largeur ?? AffichageOptions.LargeurParDefaut, 200, 4000),
        Math.Clamp(hauteur ?? AffichageOptions.HauteurParDefaut, 200, 4000));
}
