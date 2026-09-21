using HouseOs.Api.Infrastructure;
using HouseOs.Api.Infrastructure.Journalisation;
using Microsoft.EntityFrameworkCore;

namespace HouseOs.Api.Features.FluxExternes;

/// <summary>Le corps d'une poussée : la liste complète des événements du flux, telle
/// qu'elle doit être après l'appel.</summary>
public record PousseeRequete(List<EvenementPousseRequete>? Evenements);

/// <summary>Un événement poussé. <paramref name="Date"/> en ISO (2026-10-08),
/// <paramref name="Heure"/> en HH:mm ou absente pour « toute la journée ».</summary>
public record EvenementPousseRequete(string? Titre, DateOnly Date, TimeOnly? Heure, string? Uid);

/// <summary>
/// La réception d'un flux poussé : un programme extérieur — un gratteur de site
/// municipal, par exemple — remplace d'un coup les événements d'un flux
/// (vault : D-2026-09-20 Flux Externe Poussé).
///
/// <para><b>Hors du cookie de session</b>, et sur <b>sa propre clé</b> : le programme
/// qui pousse vit ailleurs que la maison, et la clé du serveur MCP ouvrirait tout le
/// reste. Clé absente de la configuration = tout est refusé, comme pour le MCP.</para>
/// </summary>
public static class PousseeEndpoints
{
    /// <summary>Policy d'autorisation de la poussée (remplace la FallbackPolicy cookie).</summary>
    public const string PolicyClePoussee = "FluxPoussee";

    /// <summary>Chemin de la clé dans la configuration (.env : HOUSEOS_POUSSEE_CLE).</summary>
    public const string CheminDeLaCle = "FluxExternes:ClePoussee";

    public static IEndpointRouteBuilder MapPousseeFluxExternes(this IEndpointRouteBuilder app)
    {
        var journal = app.JournalPour("FluxExternes");
        if (string.IsNullOrWhiteSpace(app.ServiceProvider.GetRequiredService<IConfiguration>()[CheminDeLaCle]))
        {
            journal.LogInformation(
                "FluxExternes:ClePoussee non configurée — la poussée de flux externe refusera toutes les requêtes.");
        }

        app.MapPost("/api/flux-externes/{id:guid}/evenements", async (
            Guid id,
            PousseeRequete requete,
            HouseOsDbContext db,
            CancellationToken ct) =>
        {
            var flux = await db.FluxExternes.SingleOrDefaultAsync(f => f.Id == id, ct);
            if (flux is null)
            {
                return Results.NotFound();
            }

            var recus = (requete.Evenements ?? [])
                .Select(e => new EvenementPousse(e.Titre, e.Date, e.Heure, e.Uid))
                .ToList();
            if (OperationsPoussee.PourquoiRefuser(flux, recus) is { } refus)
            {
                journal.LogWarning(
                    "Poussée refusée pour le flux {FluxId} « {Nom} » — {Raison}", flux.Id, flux.Nom, refus);
                return Results.UnprocessableEntity(new { message = refus });
            }

            var resultat = await OperationsPoussee.RemplacerAsync(
                db, flux, recus, DateOnly.FromDateTime(DateTime.Now), DateTimeOffset.UtcNow, ct);
            journal.LogInformation(
                "Flux poussé {FluxId} « {Nom} » reçu — {Recus} événement(s), {Retenus} retenu(s), "
                + "{Ignores} hors fenêtre.",
                flux.Id, flux.Nom, resultat.Recus, resultat.Retenus, resultat.Ignores);
            return Results.Ok(resultat);
        })
        .RequireAuthorization(PolicyClePoussee);

        return app;
    }
}
