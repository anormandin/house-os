using HouseOs.Api.Domaine;
using HouseOs.Api.Features.Auth;
using HouseOs.Api.Infrastructure;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using Microsoft.EntityFrameworkCore;

namespace HouseOs.Api.Features.FluxIcal;

public static class IcalEndpoints
{
    public static IEndpointRouteBuilder MapIcal(this IEndpointRouteBuilder app)
    {
        // Flux personnel : occurrences en attente avec échéance, assignées à la
        // personne + non-assignées. Anonyme — le jeton secret est l'authentification
        // (les apps calendrier ne savent pas envoyer de cookie).
        app.MapGet("/ical/{jeton}.ics", async (string jeton, HouseOsDbContext db) =>
        {
            var utilisateur = await db.Utilisateurs.AsNoTracking()
                .SingleOrDefaultAsync(u => u.JetonIcal == jeton);
            if (utilisateur is null)
            {
                return Results.NotFound();
            }

            var occurrences = await db.Occurrences.AsNoTracking()
                .Include(o => o.Tache)
                .Where(o => o.Statut == StatutOccurrence.EnAttente
                    && o.Echeance != null
                    && (o.AssigneAId == null || o.AssigneAId == utilisateur.Id))
                .OrderBy(o => o.Echeance)
                .Take(500)
                .ToListAsync();

            var calendrier = new Ical.Net.Calendar();
            calendrier.AddProperty("X-WR-CALNAME", $"Maison — {utilisateur.NomAffichage}");
            foreach (var occurrence in occurrences)
            {
                var echeance = occurrence.Echeance!.Value;
                var debut = new CalDateTime(echeance.Year, echeance.Month, echeance.Day);
                calendrier.Events.Add(new CalendarEvent
                {
                    Uid = $"occurrence-{occurrence.Id}@houseos",
                    Summary = occurrence.Tache!.Titre,
                    Description = occurrence.Tache.Description,
                    Start = debut,
                    End = debut.AddDays(1),
                });
            }

            // Comptes à rebours du foyer : évènements toute-la-journée à leur date
            // cible, dans le flux des deux personnes. Les passés sont exclus.
            var aujourdhui = DateOnly.FromDateTime(DateTime.Now);
            var comptes = await db.ComptesARebours.AsNoTracking()
                .Where(c => c.DateCible >= aujourdhui)
                .OrderBy(c => c.DateCible)
                .ToListAsync();
            foreach (var compte in comptes)
            {
                var date = new CalDateTime(compte.DateCible.Year, compte.DateCible.Month, compte.DateCible.Day);
                calendrier.Events.Add(new CalendarEvent
                {
                    Uid = $"compte-{compte.Id}@houseos",
                    Summary = compte.Titre,
                    Start = date,
                    End = date.AddDays(1),
                });
            }

            var contenu = new CalendarSerializer().SerializeToString(calendrier);
            return Results.Text(contenu, "text/calendar; charset=utf-8");
        }).AllowAnonymous();

        // L'URL du flux de la personne connectée (affichée dans la section Calendrier).
        app.MapGet("/api/ical/mon-flux", async (
            System.Security.Claims.ClaimsPrincipal principal,
            HouseOsDbContext db,
            IConfiguration config) =>
        {
            var id = principal.IdUtilisateur();
            var jeton = await db.Utilisateurs.AsNoTracking()
                .Where(u => u.Id == id)
                .Select(u => u.JetonIcal)
                .SingleOrDefaultAsync();
            return jeton is null
                ? Results.NotFound()
                : Results.Ok(JetonIcal.ReponseFlux(jeton, config));
        });

        // Rotation du jeton — chacun régénère le sien seulement. L'ancien flux
        // meurt immédiatement : les abonnements existants sont à refaire.
        app.MapPost("/api/ical/rotation", async (
            System.Security.Claims.ClaimsPrincipal principal,
            HouseOsDbContext db,
            IConfiguration config) =>
        {
            var id = principal.IdUtilisateur();
            var utilisateur = await db.Utilisateurs.SingleAsync(u => u.Id == id);
            utilisateur.JetonIcal = JetonIcal.Generer();
            await db.SaveChangesAsync();
            return Results.Ok(JetonIcal.ReponseFlux(utilisateur.JetonIcal, config));
        });

        return app;
    }
}
