using HouseOs.Api.Domaine;
using HouseOs.Api.Infrastructure;
using HouseOs.Api.Infrastructure.Journalisation;
using Microsoft.EntityFrameworkCore;

namespace HouseOs.Api.Features.FluxExternes;

/// <param name="Url">Requise pour un abonnement iCal, interdite pour un flux poussé,
/// qui n'a rien à télécharger (vault : D-2026-09-20 Flux Externe Poussé).</param>
/// <param name="Source">Ics (défaut) ou Poussee. Elle ne se change pas après coup :
/// les deux chemins d'ingestion n'ont ni la même cadence ni le même maître.</param>
public record FluxExterneRequete(string Nom, string? Url, string? Type, string? Source = null);

/// <param name="DernierRafraichissementLe">Dernier téléchargement réussi, ou dernière
/// poussée reçue — c'est le même fait, et l'UI le nomme selon la source.</param>
public record FluxExterneDto(
    Guid Id,
    string Nom,
    string? Url,
    string Type,
    string Source,
    bool Actif,
    DateTimeOffset? DernierRafraichissementLe,
    string? DerniereErreur,
    int NbEvenements);
public record EvenementExterneDto(string Titre, string Type, DateOnly Date, TimeOnly? Heure);

public static class FluxExternesEndpoints
{
    public static IEndpointRouteBuilder MapFluxExternes(this IEndpointRouteBuilder app)
    {
        var journal = app.JournalPour("FluxExternes");

        app.MapGet("/api/flux-externes", async (HouseOsDbContext db) =>
            await db.FluxExternes
                .OrderBy(f => f.Nom)
                .Select(f => new FluxExterneDto(
                    f.Id, f.Nom, f.Url, f.Type.ToString(), f.Source.ToString(), f.Actif,
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
            var erreur = Valider(journal, requete, out var type, out var source);
            if (erreur is not null)
            {
                return erreur;
            }

            var flux = new FluxExterne
            {
                Id = Guid.NewGuid(),
                Nom = requete.Nom.Trim(),
                Url = source == SourceFluxExterne.Ics ? requete.Url?.Trim() : null,
                Type = type,
                Source = source,
            };
            db.FluxExternes.Add(flux);
            await db.SaveChangesAsync(ct);

            // Un flux poussé n'a rien à valider ni à amorcer : il attend sa première
            // poussée, et l'UI montre qu'il n'a encore rien reçu.
            if (source == SourceFluxExterne.Poussee)
            {
                journal.LogInformation(
                    "Flux poussé {FluxId} créé — « {Nom} » ({Type}), en attente de sa première poussée.",
                    flux.Id, flux.Nom, flux.Type);
                return Results.Created($"/api/flux-externes/{flux.Id}", new FluxExterneDto(
                    flux.Id, flux.Nom, null, flux.Type.ToString(), flux.Source.ToString(), flux.Actif,
                    null, null, 0));
            }

            await FluxExternesRafraichissement.Rafraichir(
                db, flux, httpFactory.CreateClient(FluxExternesRafraichissement.NomClientHttp), ct);
            if (flux.DerniereErreur is not null)
            {
                // URL invalide : on ne garde pas l'abonnement mort.
                db.FluxExternes.Remove(flux);
                await db.SaveChangesAsync(ct);
                journal.LogWarning(
                    "Flux externe « {Nom} » refusé à la création — {Erreur} ({Url}).",
                    flux.Nom, flux.DerniereErreur, flux.Url);
                return Results.UnprocessableEntity(new
                {
                    message = $"Impossible de lire ce calendrier : {flux.DerniereErreur}",
                });
            }

            var nbEvenements = await db.EvenementsExternes.CountAsync(e => e.FluxExterneId == flux.Id, ct);
            journal.LogInformation(
                "Flux externe {FluxId} créé — « {Nom} » ({Type}), {NbEvenements} évènement(s) amorcé(s).",
                flux.Id, flux.Nom, flux.Type, nbEvenements);
            return Results.Created($"/api/flux-externes/{flux.Id}", new FluxExterneDto(
                flux.Id, flux.Nom, flux.Url, flux.Type.ToString(), flux.Source.ToString(), flux.Actif,
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
            // La source se juge AVANT le reste : un client qui relit puis réécrit une
            // fiche envoie l'URL courante avec la nouvelle source, et se ferait sinon
            // répondre « un calendrier poussé n'a pas d'URL » — vrai, mais à côté de
            // ce qu'il a demandé. Changer de source, c'est changer de maître : un
            // abonnement devenu poussé se viderait à la prochaine passe si le filtre
            // bougeait, et un flux poussé devenu abonnement perdrait ce que le dehors
            // lui a donné. On en crée un autre — c'est plus clair que de deviner.
            // Source absente : celle qu'il a déjà (modifier un nom ne change rien).
            if (requete.Source is { } demandee
                && (ParseurEnum.Lire<SourceFluxExterne>(demandee, out var voulue) == false
                    || voulue != flux.Source))
            {
                return ResultatsApi.Erreur(
                    journal, "source",
                    "La source d'un calendrier ne se change pas : supprimez-le et recréez-le.");
            }
            var erreur = Valider(
                journal, requete with { Source = flux.Source.ToString() },
                out var type, out var source);
            if (erreur is not null)
            {
                return erreur;
            }

            var urlChangee = source == SourceFluxExterne.Ics && flux.Url != requete.Url?.Trim();
            flux.Nom = requete.Nom.Trim();
            flux.Url = source == SourceFluxExterne.Ics ? requete.Url?.Trim() : null;
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
            journal.LogInformation(
                "Flux externe {FluxId} modifié — « {Nom} », URL changée : {UrlChangee}.",
                flux.Id, flux.Nom, urlChangee);
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
            journal.LogInformation("Flux externe {FluxId} supprimé — « {Nom} ».", flux.Id, flux.Nom);
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

    private static IResult? Valider(
        ILogger journal, FluxExterneRequete requete, out TypeFluxExterne type, out SourceFluxExterne source)
    {
        type = TypeFluxExterne.Autre;
        source = SourceFluxExterne.Ics;
        if (string.IsNullOrWhiteSpace(requete.Nom))
        {
            return ResultatsApi.Erreur(journal, "nom", "Le nom est requis.");
        }
        // ParseurEnum et non Enum.TryParse : celui-ci accepte « 5 » et les valeurs
        // indéfinies, ce qui donnait un flux que plus rien ne rafraîchit ni ne reçoit.
        if (requete.Source is not null && ParseurEnum.Lire(requete.Source, out source) == false)
        {
            return ResultatsApi.Erreur(journal, "source", "Source inconnue.");
        }
        if (source == SourceFluxExterne.Poussee)
        {
            // Une URL sur un flux poussé ne serait jamais lue : la refuser plutôt que
            // de la garder en base à faire croire le contraire.
            if (string.IsNullOrWhiteSpace(requete.Url) == false)
            {
                return ResultatsApi.Erreur(
                    journal, "url", "Un calendrier poussé n'a pas d'URL : c'est le dehors qui l'alimente.");
            }
        }
        else if (Uri.TryCreate(requete.Url?.Trim(), UriKind.Absolute, out var uri) == false
                 || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return ResultatsApi.Erreur(journal, "url", "L'URL doit être une adresse http(s) valide.");
        }
        if (requete.Type is not null && ParseurEnum.Lire(requete.Type, out type) == false)
        {
            return ResultatsApi.Erreur(journal, "type", "Type inconnu.");
        }
        return null;
    }
}
