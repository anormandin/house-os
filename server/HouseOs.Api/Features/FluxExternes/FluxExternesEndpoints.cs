using HouseOs.Api.Domaine;
using HouseOs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HouseOs.Api.Features.FluxExternes;

public record FluxExterneRequete(string Nom, string Url, string? Type);
public record FluxExterneDto(
    Guid Id,
    string Nom,
    string Url,
    string Type,
    bool Actif,
    DateTimeOffset? DernierRafraichissementLe,
    string? DerniereErreur,
    int NbEvenements);
public record EvenementExterneDto(string Titre, string Type, DateOnly Date, TimeOnly? Heure);

public static class FluxExternesEndpoints
{
    public static IEndpointRouteBuilder MapFluxExternes(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/flux-externes", async (HouseOsDbContext db) =>
            await db.FluxExternes
                .OrderBy(f => f.Nom)
                .Select(f => new FluxExterneDto(
                    f.Id, f.Nom, f.Url, f.Type.ToString(), f.Actif,
                    f.DernierRafraichissementLe, f.DerniereErreur, f.Evenements.Count))
                .ToListAsync());

        // La création télécharge le flux une fois, inline : c'est la validation de
        // l'URL et l'amorçage des événements (D-2026-08-24 Tables Flux Externes —
        // exception assumée au « jamais d'appel externe dans une requête »).
        app.MapPost("/api/flux-externes", async (
            FluxExterneRequete requete,
            HouseOsDbContext db,
            IHttpClientFactory httpFactory,
            CancellationToken ct) =>
        {
            var erreur = Valider(requete, out var type);
            if (erreur is not null)
            {
                return erreur;
            }

            var flux = new FluxExterne
            {
                Id = Guid.NewGuid(),
                Nom = requete.Nom.Trim(),
                Url = requete.Url.Trim(),
                Type = type,
            };
            db.FluxExternes.Add(flux);
            await db.SaveChangesAsync(ct);

            await FluxExternesRafraichissement.Rafraichir(
                db, flux, httpFactory.CreateClient(FluxExternesRafraichissement.NomClientHttp), ct);
            if (flux.DerniereErreur is not null)
            {
                // URL invalide : on ne garde pas l'abonnement mort.
                db.FluxExternes.Remove(flux);
                await db.SaveChangesAsync(ct);
                return Results.UnprocessableEntity(new
                {
                    message = $"Impossible de lire ce calendrier : {flux.DerniereErreur}",
                });
            }

            var nbEvenements = await db.EvenementsExternes.CountAsync(e => e.FluxExterneId == flux.Id, ct);
            return Results.Created($"/api/flux-externes/{flux.Id}", new FluxExterneDto(
                flux.Id, flux.Nom, flux.Url, flux.Type.ToString(), flux.Actif,
                flux.DernierRafraichissementLe, null, nbEvenements));
        });

        app.MapPut("/api/flux-externes/{id:guid}", async (
            Guid id,
            FluxExterneRequete requete,
            HouseOsDbContext db,
            IHttpClientFactory httpFactory,
            CancellationToken ct) =>
        {
            var flux = await db.FluxExternes.FindAsync(id);
            if (flux is null)
            {
                return Results.NotFound();
            }
            var erreur = Valider(requete, out var type);
            if (erreur is not null)
            {
                return erreur;
            }

            var urlChangee = flux.Url != requete.Url.Trim();
            flux.Nom = requete.Nom.Trim();
            flux.Url = requete.Url.Trim();
            flux.Type = type;
            await db.SaveChangesAsync(ct);

            if (urlChangee)
            {
                // L'ancien calendrier ne vaut plus rien sous ce flux : purger tout de
                // suite et recharger — sinon jusqu'à 6 h de faux événements sous le
                // nouveau nom. Un échec s'affiche dans la gestion (DerniereErreur).
                await db.EvenementsExternes.Where(e => e.FluxExterneId == flux.Id).ExecuteDeleteAsync(ct);
                await FluxExternesRafraichissement.Rafraichir(
                    db, flux, httpFactory.CreateClient(FluxExternesRafraichissement.NomClientHttp), ct);
            }
            return Results.NoContent();
        });

        app.MapDelete("/api/flux-externes/{id:guid}", async (Guid id, HouseOsDbContext db) =>
        {
            var flux = await db.FluxExternes.FindAsync(id);
            if (flux is null)
            {
                return Results.NotFound();
            }
            db.FluxExternes.Remove(flux);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // Les événements à venir (défaut : 7 jours), pour le bandeau du jour et
        // Cette semaine. Lecture des tables seulement.
        app.MapGet("/api/evenements-externes", async (int? jours, HouseOsDbContext db) =>
        {
            var aujourdhui = DateOnly.FromDateTime(DateTime.Now);
            var fin = aujourdhui.AddDays(Math.Clamp(jours ?? 7, 1, FluxExternesRafraichissement.FenetreJours));
            return await db.EvenementsExternes
                .Where(e => e.Date >= aujourdhui && e.Date < fin)
                .Join(db.FluxExternes, e => e.FluxExterneId, f => f.Id,
                    (e, f) => new { e, f })
                .OrderBy(x => x.e.Date).ThenBy(x => x.e.Heure)
                .Select(x => new EvenementExterneDto(x.e.Titre, x.f.Type.ToString(), x.e.Date, x.e.Heure))
                .ToListAsync();
        });

        return app;
    }

    private static IResult? Valider(FluxExterneRequete requete, out TypeFluxExterne type)
    {
        type = TypeFluxExterne.Autre;
        if (string.IsNullOrWhiteSpace(requete.Nom))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["nom"] = ["Le nom est requis."],
            });
        }
        if (Uri.TryCreate(requete.Url?.Trim(), UriKind.Absolute, out var uri) == false
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["url"] = ["L'URL doit être une adresse http(s) valide."],
            });
        }
        if (requete.Type is not null && Enum.TryParse(requete.Type, out type) == false)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["type"] = ["Type inconnu."],
            });
        }
        return null;
    }
}
