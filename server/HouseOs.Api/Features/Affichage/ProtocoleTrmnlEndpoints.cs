using System.Text.Json;
using System.Text.Json.Serialization;
using HouseOs.Api.Domaine;
using HouseOs.Api.Infrastructure;
using HouseOs.Api.Infrastructure.Journalisation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HouseOs.Api.Features.Affichage;

/// <summary>Réponse à GET /api/setup — les noms sont ceux du firmware, en snake_case.</summary>
public record ReponseSetup(
    [property: JsonPropertyName("status")] int Status,
    [property: JsonPropertyName("api_key")] string? ApiKey,
    [property: JsonPropertyName("friendly_id")] string? FriendlyId,
    [property: JsonPropertyName("image_url")] string? ImageUrl,
    [property: JsonPropertyName("filename")] string? Filename);

/// <summary>Réponse à GET /api/display.</summary>
public record ReponseDisplay(
    [property: JsonPropertyName("status")] int Status,
    [property: JsonPropertyName("image_url")] string ImageUrl,
    [property: JsonPropertyName("filename")] string Filename,
    [property: JsonPropertyName("refresh_rate")] int RefreshRate,
    [property: JsonPropertyName("update_firmware")] bool UpdateFirmware,
    [property: JsonPropertyName("firmware_url")] string? FirmwareUrl,
    [property: JsonPropertyName("reset_firmware")] bool ResetFirmware,
    [property: JsonPropertyName("special_function")] string SpecialFunction);

/// <summary>
/// House OS comme serveur du firmware TRMNL (D-2026-09-03 Protocole TRMNL BYOS Comme
/// API D'affichage). Trois routes que l'appareil appelle, une route d'image qu'il
/// télécharge. Toutes hors cookie : l'appareil s'identifie par sa MAC et sa clé.
/// </summary>
public static class ProtocoleTrmnlEndpoints
{
    /// <summary>Nom de fichier réservé à l'écran d'accueil (après l'enrôlement).</summary>
    public const string FichierAccueil = "accueil";

    public static IEndpointRouteBuilder MapProtocoleTrmnl(this IEndpointRouteBuilder app)
    {
        var journal = app.JournalPour("Affichage");

        // Enrôlement : un appareil inconnu est créé sur-le-champ (réseau privé), un
        // appareil connu reçoit sa clé existante — c'est ainsi qu'il la retrouve après
        // une réinitialisation du Wi-Fi.
        app.MapGet("/api/setup", async (HttpContext contexte, HouseOsDbContext db) =>
        {
            var mac = contexte.Request.Headers["ID"].ToString();
            if (string.IsNullOrWhiteSpace(mac))
            {
                journal.LogWarning("Setup sans en-tête ID depuis {Ip}.", contexte.Connection.RemoteIpAddress);
                return Results.Json(new ReponseSetup(404, null, null, null, null),
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var appareil = await OperationsAppareils.EnrolerOuRetrouverAsync(db, mac, DateTimeOffset.UtcNow);
            journal.LogInformation("Appareil {Identifiant} ({Mac}) enrôlé ou retrouvé au setup.",
                appareil.Identifiant, appareil.AdresseMac);
            return Results.Ok(new ReponseSetup(200, appareil.Cle, appareil.Identifiant,
                UrlImage(contexte, appareil, FichierAccueil), FichierAccueil));
        }).AllowAnonymous();

        // Le réveil : télémétrie, rendu à la taille annoncée, URL signée du bitmap,
        // délai avant le prochain réveil.
        app.MapGet("/api/display", async (
            HttpContext contexte, HouseOsDbContext db, IRenduEcran rendu, CacheImages cache,
            IOptions<AffichageOptions> options, CancellationToken ct) =>
        {
            var appareil = await OperationsAppareils.AuthentifierAsync(db,
                contexte.Request.Headers["ID"], contexte.Request.Headers["Access-Token"]);
            if (appareil is null)
            {
                journal.LogWarning("Display refusé — appareil inconnu ou clé invalide (ID {Id}).",
                    contexte.Request.Headers["ID"].ToString());
                return Results.Unauthorized();
            }

            var maintenant = DateTime.Now;
            // Instants en UTC pour Npgsql (timestamptz refuse tout autre offset).
            Telemetrie.Appliquer(appareil, contexte.Request.Headers, DateTimeOffset.UtcNow);

            var demande = new DemandeCapture(
                appareil.Largeur ?? AffichageOptions.LargeurParDefaut,
                appareil.Hauteur ?? AffichageOptions.HauteurParDefaut,
                Telemetrie.PileEnPourcent(appareil.TensionPile));
            string fichier;
            try
            {
                var bitmap = Seuillage.EnUnBit(await rendu.CapturerAsync(demande, ct));
                fichier = Seuillage.Signature(bitmap);
                cache.Deposer(appareil.Id, fichier, bitmap);
            }
            catch (RenduEcranException ex)
            {
                // L'appareil garde son image et réessaie à la cadence normale : mieux
                // qu'un écran vide.
                journal.LogError(ex, "Rendu impossible pour l'appareil {Identifiant}.", appareil.Identifiant);
                fichier = appareil.DernierFichier ?? FichierAccueil;
            }

            var inchange = fichier == appareil.DernierFichier;
            appareil.DernierFichier = fichier;
            await db.SaveChangesAsync(ct);

            var delai = DelaiReveil.Calculer(maintenant, options.Value);
            journal.LogInformation(
                "Display {Identifiant} — {Largeur}×{Hauteur}, pile {Tension} V, RSSI {Rssi}, image {Fichier}{Inchange}, prochain réveil dans {Delai} s.",
                appareil.Identifiant, demande.Largeur, demande.Hauteur, appareil.TensionPile, appareil.Rssi,
                fichier, inchange ? " (inchangée)" : "", delai);

            return Results.Ok(new ReponseDisplay(0, UrlImage(contexte, appareil, fichier), fichier, delai,
                UpdateFirmware: false, FirmwareUrl: null, ResetFirmware: false, SpecialFunction: "none"));
        }).AllowAnonymous();

        // Le bitmap lui-même : anonyme mais signé (HMAC du nom avec la clé de l'appareil).
        app.MapGet("/api/affichage/{identifiant:length(6)}/{jeton:length(32)}/{fichier:regex(^[a-z0-9]{{1,32}}$)}.png",
            async (string identifiant, string jeton, string fichier, HouseOsDbContext db,
                IRenduEcran rendu, CacheImages cache, CancellationToken ct) =>
        {
            var appareil = await db.AppareilsAffichage
                .SingleOrDefaultAsync(a => a.Identifiant == identifiant.ToUpperInvariant(), ct);
            if (appareil is null || OperationsAppareils.JetonImageValide(appareil, fichier, jeton) == false)
            {
                return Results.NotFound();
            }

            var octets = cache.Lire(appareil.Id, fichier);
            if (octets is null)
            {
                // Redémarrage entre le display et le téléchargement : on re-rend. Le
                // nom demandé ne correspond peut-être plus au contenu ; l'appareil
                // affichera l'écran courant, ce qui est ce qu'on veut.
                var demande = new DemandeCapture(
                    appareil.Largeur ?? AffichageOptions.LargeurParDefaut,
                    appareil.Hauteur ?? AffichageOptions.HauteurParDefaut,
                    Telemetrie.PileEnPourcent(appareil.TensionPile),
                    Accueil: fichier == FichierAccueil ? appareil.Identifiant : null);
                try
                {
                    octets = Seuillage.EnUnBit(await rendu.CapturerAsync(demande, ct));
                }
                catch (RenduEcranException ex)
                {
                    journal.LogError(ex, "Image {Fichier} introuvable et rendu impossible ({Identifiant}).",
                        fichier, appareil.Identifiant);
                    return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable);
                }
                cache.Deposer(appareil.Id, fichier, octets);
            }
            return Results.File(octets, "image/png");
        }).AllowAnonymous();

        // Le journal du firmware, reversé dans le nôtre : Seq voit ce que l'appareil a vu.
        app.MapPost("/api/log", async (HttpContext contexte, HouseOsDbContext db) =>
        {
            var appareil = await OperationsAppareils.AuthentifierAsync(db,
                contexte.Request.Headers["ID"], contexte.Request.Headers["Access-Token"]);
            if (appareil is null)
            {
                return Results.Unauthorized();
            }

            using var document = await LireJsonAsync(contexte.Request);
            if (document is not null && document.RootElement.TryGetProperty("logs", out var logs)
                && logs.ValueKind == JsonValueKind.Array)
            {
                foreach (var entree in logs.EnumerateArray())
                {
                    var niveau = entree.TryGetProperty("level", out var n) ? n.ToString() : "info";
                    var message = entree.TryGetProperty("message", out var m) ? m.ToString() : entree.ToString();
                    journal.Log(
                        niveau is "error" or "fatal" ? LogLevel.Error : niveau is "warning" ? LogLevel.Warning : LogLevel.Information,
                        "Firmware {Identifiant} [{Niveau}] : {Message} {Entree}",
                        appareil.Identifiant, niveau, message, entree.ToString());
                }
            }
            return Results.NoContent();
        }).AllowAnonymous();

        return app;
    }

    /// <summary>URL absolue que l'appareil téléchargera — configurée, sinon déduite de la requête.</summary>
    private static string UrlImage(HttpContext contexte, AppareilAffichage appareil, string fichier)
    {
        var options = contexte.RequestServices.GetRequiredService<IOptions<AffichageOptions>>().Value;
        var basePublique = string.IsNullOrWhiteSpace(options.UrlBase)
            ? $"{contexte.Request.Scheme}://{contexte.Request.Host}"
            : options.UrlBase.TrimEnd('/');
        return $"{basePublique}/api/affichage/{appareil.Identifiant}/{OperationsAppareils.JetonImage(appareil, fichier)}/{fichier}.png";
    }

    private static async Task<JsonDocument?> LireJsonAsync(HttpRequest requete)
    {
        try
        {
            return await JsonDocument.ParseAsync(requete.Body);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
