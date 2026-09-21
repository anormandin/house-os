using System.ComponentModel;
using System.Globalization;
using HouseOs.Api.Domaine;
using HouseOs.Api.Features.FluxExternes;
using HouseOs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace HouseOs.Api.Features.Mcp;

/// <summary>Un événement à pousser dans un flux (date ISO, heure facultative).</summary>
public record EvenementPousseDonnees(
    [property: Description("Titre affiché (requis).")] string Titre,
    [property: Description("Date YYYY-MM-DD (requise).")] string Date,
    [property: Description("Heure de début HH:mm, ou null pour toute la journée.")] string? Heure = null,
    [property: Description("Identifiant de la source, s'il y en a un ; sinon le titre en tient lieu.")]
    string? Uid = null);

/// <summary>
/// Les calendriers externes par MCP : la même tranche que REST, au même moment
/// (vault : Serveur MCP — parité MCP / API). Ce que l'UI sait faire, l'agent le sait
/// faire : abonner à un iCal, ouvrir un flux poussé, et remplacer les événements d'un
/// flux poussé (vault : D-2026-09-20 Flux Externe Poussé).
/// </summary>
[McpServerToolType]
public static class OutilsFlux
{
    [McpServerTool(Name = "lister_flux_externes")]
    [Description("Liste les calendriers externes : id, nom, type (Collecte|Ecole|Municipal|Autre), " +
        "source (Ics = téléchargé par l'app toutes les 6 h ; Poussee = rempli du dehors par " +
        "pousser_evenements_flux), URL, dernière réception, dernière erreur, nombre d'événements à venir.")]
    public static async Task<List<FluxExterneDto>> ListerFluxExternes(HouseOsDbContext db) =>
        await db.FluxExternes
            .OrderBy(f => f.Nom)
            .Select(f => new FluxExterneDto(
                f.Id, f.Nom, f.Url, f.Type.ToString(), f.Source.ToString(), f.Actif,
                f.DernierRafraichissementLe, f.DerniereErreur, f.Evenements.Count))
            .ToListAsync();

    [McpServerTool(Name = "gerer_flux_externe")]
    [Description("Créer, modifier ou supprimer un calendrier externe. Un abonnement iCal (source Ics) " +
        "est téléchargé une fois à la création pour valider l'URL — une URL illisible fait échouer " +
        "la création. Un flux poussé (source Poussee) n'a pas d'URL et reste vide jusqu'à sa première " +
        "poussée. La source ne se change pas après coup.")]
    public static async Task<object> GererFluxExterne(
        HouseOsDbContext db,
        IHttpClientFactory httpFactory,
        [Description("creer, modifier ou supprimer.")] string action,
        [Description("Id du flux (requis pour modifier et supprimer).")] Guid? id = null,
        [Description("Nom affiché (requis pour creer).")] string? nom = null,
        [Description("URL du .ics (requise pour creer un flux Ics, interdite pour un flux poussé).")]
        string? url = null,
        [Description("Collecte, Ecole, Municipal ou Autre (défaut : Autre).")] string? type = null,
        [Description("Ics (défaut) ou Poussee.")] string? source = null,
        CancellationToken ct = default)
    {
        switch (Conversions.NormaliserAction(action))
        {
            case "creer":
            {
                var flux = new FluxExterne
                {
                    Id = Guid.NewGuid(),
                    Nom = RequisNom(nom),
                    Type = ParserType(type) ?? TypeFluxExterne.Autre,
                    Source = ParserSource(source) ?? SourceFluxExterne.Ics,
                };
                flux.Url = UrlValidee(url, flux.Source);
                db.FluxExternes.Add(flux);
                await db.SaveChangesAsync(ct);

                if (flux.Source == SourceFluxExterne.Ics)
                {
                    // Même geste qu'à la création par REST : le téléchargement inline
                    // EST la validation, et un abonnement mort ne se garde pas.
                    await FluxExternesRafraichissement.Rafraichir(
                        db, flux, httpFactory.CreateClient(FluxExternesRafraichissement.NomClientHttp), ct);
                    if (flux.DerniereErreur is { } erreur)
                    {
                        db.FluxExternes.Remove(flux);
                        await db.SaveChangesAsync(ct);
                        throw new McpException($"Impossible de lire ce calendrier : {erreur}");
                    }
                }
                return await DecrireAsync(db, flux, ct);
            }
            case "modifier":
            {
                var flux = await TrouverFlux(db, id, ct);
                if (ParserSource(source) is { } demandee && demandee != flux.Source)
                {
                    throw new McpException(
                        "La source d'un calendrier ne se change pas : supprimez-le et recréez-le.");
                }
                // Fiche partielle d'un client LLM : un champ vide veut dire « ne pas
                // toucher », jamais un effacement — même convention que les autres
                // outils gerer_*. Une URL *fournie* sur un flux poussé, en revanche,
                // se refuse comme le fait REST : l'accepter en la jetant en silence
                // laisserait l'agent croire qu'elle sert à quelque chose.
                var urlDemandee = string.IsNullOrWhiteSpace(url)
                    ? flux.Url
                    : UrlValidee(url, flux.Source);
                var urlChangee = urlDemandee != flux.Url;

                flux.Nom = string.IsNullOrWhiteSpace(nom) ? flux.Nom : nom.Trim();
                flux.Url = urlDemandee;
                flux.Type = ParserType(type) ?? flux.Type;
                await db.SaveChangesAsync(ct);

                if (urlChangee)
                {
                    await db.EvenementsExternes.Where(e => e.FluxExterneId == flux.Id).ExecuteDeleteAsync(ct);
                    await FluxExternesRafraichissement.Rafraichir(
                        db, flux, httpFactory.CreateClient(FluxExternesRafraichissement.NomClientHttp), ct);
                }
                return await DecrireAsync(db, flux, ct);
            }
            case "supprimer":
            {
                var flux = await TrouverFlux(db, id, ct);
                db.FluxExternes.Remove(flux);
                await db.SaveChangesAsync(ct);
                return new { supprime = true, id = flux.Id };
            }
            default:
                throw new McpException($"Action inconnue : '{action}' (creer, modifier ou supprimer).");
        }
    }

    [McpServerTool(Name = "pousser_evenements_flux")]
    [Description("Remplace TOUS les événements d'un flux poussé par ceux fournis — ce qui n'est pas " +
        "dans la liste disparaît. Réservé aux flux de source Poussee. Les événements hors de la " +
        "fenêtre (d'hier à +60 jours) sont écartés sans faire échouer l'appel.")]
    public static async Task<object> PousserEvenementsFlux(
        HouseOsDbContext db,
        [Description("Id du flux poussé (via lister_flux_externes).")] Guid id,
        [Description("La liste complète des événements du flux. Vide = le flux n'annonce plus rien.")]
        List<EvenementPousseDonnees>? evenements = null)
    {
        var flux = await TrouverFlux(db, id);
        var recus = (evenements ?? [])
            .Select(e => new EvenementPousse(
                e.Titre,
                Conversions.ParserDate(e.Date, "date")
                    ?? throw new McpException("Chaque événement a besoin d'une date YYYY-MM-DD."),
                ParserHeure(e.Heure),
                e.Uid))
            .ToList();
        if (OperationsPoussee.PourquoiRefuser(flux, recus) is { } refus)
        {
            throw new McpException(refus);
        }
        return await OperationsPoussee.RemplacerAsync(
            db, flux, recus, DateOnly.FromDateTime(DateTime.Now), DateTimeOffset.UtcNow);
    }

    private static async Task<FluxExterne> TrouverFlux(HouseOsDbContext db, Guid? id, CancellationToken ct = default)
    {
        var flux = id is { } valeur
            ? await db.FluxExternes.SingleOrDefaultAsync(f => f.Id == valeur, ct)
            : null;
        return flux ?? throw new McpException($"Calendrier externe introuvable : {id}.");
    }

    private static async Task<FluxExterneDto> DecrireAsync(
        HouseOsDbContext db, FluxExterne flux, CancellationToken ct) =>
        new(flux.Id, flux.Nom, flux.Url, flux.Type.ToString(), flux.Source.ToString(), flux.Actif,
            flux.DernierRafraichissementLe, flux.DerniereErreur,
            await db.EvenementsExternes.CountAsync(e => e.FluxExterneId == flux.Id, ct));

    private static string RequisNom(string? nom) =>
        string.IsNullOrWhiteSpace(nom) ? throw new McpException("Le nom est requis.") : nom.Trim();

    private static TypeFluxExterne? ParserType(string? type) =>
        type is null
            ? null
            : Conversions.ParserEnum<TypeFluxExterne>(type, out var valeur)
                ? valeur
                : throw new McpException($"Type inconnu : '{type}' (Collecte, Ecole, Municipal ou Autre).");

    private static SourceFluxExterne? ParserSource(string? source) =>
        source is null
            ? null
            : Conversions.ParserEnum<SourceFluxExterne>(source, out var valeur)
                ? valeur
                : throw new McpException($"Source inconnue : '{source}' (Ics ou Poussee).");

    private static string? UrlValidee(string? url, SourceFluxExterne source)
    {
        if (source == SourceFluxExterne.Poussee)
        {
            return string.IsNullOrWhiteSpace(url)
                ? null
                : throw new McpException("Un calendrier poussé n'a pas d'URL : c'est le dehors qui l'alimente.");
        }
        if (Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var uri) == false
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new McpException("L'URL doit être une adresse http(s) valide.");
        }
        return url!.Trim();
    }

    private static TimeOnly? ParserHeure(string? heure)
    {
        if (string.IsNullOrWhiteSpace(heure))
        {
            return null;
        }
        return TimeOnly.TryParse(heure, CultureInfo.InvariantCulture, out var valeur)
            ? valeur
            : throw new McpException($"Heure invalide : '{heure}' — format attendu HH:mm (ex. 19:30).");
    }
}
