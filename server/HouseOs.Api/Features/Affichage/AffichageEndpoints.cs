using HouseOs.Api.Features.Editorial;
using HouseOs.Api.Features.FondsDeTiroir;
using HouseOs.Api.Domaine.Humeur;
using HouseOs.Api.Features.Humeur;
using HouseOs.Api.Features.Meteo;
using HouseOs.Api.Infrastructure;
using Microsoft.Extensions.Options;

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
            HttpContext contexte, JetonRendu jeton, HouseOsDbContext db,
            IOptions<AffichageOptions> options, IOptions<MeteoOptions> meteo,
            IOptions<HumeurOptions> humeur, BanqueDuHasard banqueDuHasard, SignalDeReedition signal,
            ILoggerFactory fabrique, CancellationToken ct, string? maintenant) =>
        {
            if (jeton.Autorise(contexte) == false)
            {
                return Results.Unauthorized();
            }
            // L'horloge d'essai arrive par l'URL que la page a reçue du navigateur de
            // rendu : c'est ainsi qu'un tirage « du matin » traverse la capture.
            if (MomentDEssai.Lire(maintenant, DateTime.Now, out var horloge) == false)
            {
                return Results.BadRequest(new { erreur = MomentDEssai.Erreur });
            }
            // Le créneau que cette horloge commande : c'est lui qui choisit la phrase.
            TirageDuMur.LireMoment(null, horloge, humeur.Value, out _, out var creneau);
            // L'édition ne se matérialise que pour la journée vraie : un aperçu daté
            // d'un autre jour la compose sans l'écrire.
            var persister = DateOnly.FromDateTime(horloge) == DateOnly.FromDateTime(DateTime.Now);
            return Results.Ok(await ComposerDonneesEcran.LireAsync(
                db, horloge, options.Value.Lieu, meteo.Value, banqueDuHasard, creneau,
                persister, signal, fabrique.CreateLogger("HouseOs.Editorial"), ct));
        }).AllowAnonymous();

        // L'outil de conception avant la livraison, de diagnostic ensuite : le PNG
        // exact (seuillé) — ou la capture brute avec ?brut=1 pour comparer.
        app.MapGet("/api/affichage/apercu.png", async (
            int? largeur, int? hauteur, int? pile, string? brut, string? accueil, string? moment,
            IRenduEcran rendu, ILoggerFactory fabrique, CancellationToken ct) =>
        {
            var (l, h) = Dimensions(largeur, hauteur);
            // « brut=1 » comme « brut=true » : un humain tape ça dans une barre d'adresse.
            var sansSeuillage = brut is "1" or "true";
            // « ?moment=matin » : le mur du matin, tout de suite. Ici et sur le tirage
            // demandé à la main, jamais sur /api/display.
            if (MomentDEssai.Lire(moment, DateTime.Now, out var horloge) == false)
            {
                return Results.BadRequest(new { erreur = MomentDEssai.Erreur });
            }
            try
            {
                var capture = await rendu.CapturerAsync(
                    new DemandeCapture(l, h, pile, accueil, moment is null ? null : horloge), ct);
                return Results.File(sansSeuillage ? capture : Seuillage.EnUnBit(capture), "image/png");
            }
            catch (RenduEcranException ex)
            {
                fabrique.CreateLogger("HouseOs.Affichage").LogError(ex, "Aperçu e-ink impossible.");
                return Results.Problem(title: "Rendu de l'écran impossible", detail: ex.Message,
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        });

        // Régénérer le journal du mur à la demande : réécrire l'édition et la phrase du
        // créneau (appels LLM compris) puis tirer l'image par le chemin de l'appareil.
        // Parité MCP : regenerer_journal_mural.
        app.MapPost("/api/affichage/regenerer", async (
            string? moment, string? date, HouseOsDbContext db, IOptions<HumeurOptions> humeur,
            IRedacteurEdition redacteur, IOptions<MeteoOptions> meteo, BanqueDuHasard banque,
            IOptions<AffichageOptions> affichage,
            IRenduEcran rendu, CacheImages cache, ILoggerFactory fabrique, CancellationToken ct) =>
        {
            var maintenant = DateTime.Now;
            if (TirageDuMur.LireMoment(moment, maintenant, humeur.Value, out var jour, out var creneau) == false)
            {
                return Results.BadRequest(new { erreur = TirageDuMur.Erreur });
            }
            if (TirageDuMur.LireDate(date, out var autreJour) == false)
            {
                return Results.BadRequest(new { erreur = TirageDuMur.ErreurDate });
            }
            jour = autreJour ?? jour;
            var journal = fabrique.CreateLogger("HouseOs.Affichage");
            try
            {
                return Results.Ok(await TirageDuMur.RegenererAsync(
                    db, humeur.Value, redacteur, meteo.Value, banque, affichage.Value.Lieu,
                    rendu, cache, journal, jour, creneau,
                    HorlogeDuCreneau(jour, creneau, humeur.Value, maintenant), maintenant, ct));
            }
            catch (RenduEcranException ex)
            {
                // L'édition et la phrase sont écrites quand même : c'est l'image qui a manqué.
                journal.LogError(ex, "Tirage du mur impossible.");
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

    /// <summary>
    /// L'heure à laquelle dater le tirage : celle du créneau demandé, pour que le
    /// surtitre (« Édition du matin ») et l'heure d'impression concordent avec la
    /// phrase qu'on vient d'écrire. Le créneau qu'on vit garde l'heure vraie — le pied
    /// du journal dit quand il a été imprimé, et une heure ronde y mentirait.
    /// </summary>
    internal static DateTime HorlogeDuCreneau(
        DateOnly date, MomentJournee moment, HumeurOptions humeur, DateTime maintenant)
    {
        TirageDuMur.LireMoment(null, maintenant, humeur, out var dateCourante, out var courant);
        if (dateCourante == date && courant == moment)
        {
            return maintenant;
        }
        return date.ToDateTime(moment == MomentJournee.Soir ? MomentDEssai.Soir : MomentDEssai.Matin);
    }

    /// <summary>Taille demandée bornée au raisonnable ; sans indication, l'E1003 en paysage.</summary>
    public static (int Largeur, int Hauteur) Dimensions(int? largeur, int? hauteur) => (
        Math.Clamp(largeur ?? AffichageOptions.LargeurParDefaut, 200, 4000),
        Math.Clamp(hauteur ?? AffichageOptions.HauteurParDefaut, 200, 4000));
}
