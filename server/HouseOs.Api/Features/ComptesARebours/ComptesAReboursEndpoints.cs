using HouseOs.Api.Domaine;
using HouseOs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HouseOs.Api.Features.ComptesARebours;

public record CompteARebourRequete(string Titre, DateOnly? DateCible, string? Icone);
public record CompteARebourDto(Guid Id, string Titre, DateOnly DateCible, string Icone);

public static class ComptesAReboursEndpoints
{
    public static IEndpointRouteBuilder MapComptesARebours(this IEndpointRouteBuilder app)
    {
        // Retourne aussi les comptes passés : la gestion les affiche (marqués
        // « passé ») pour suppression, la carte filtre côté client.
        app.MapGet("/api/comptes-a-rebours", async (HouseOsDbContext db) =>
            await db.ComptesARebours
                .OrderBy(c => c.DateCible)
                .Select(c => new CompteARebourDto(c.Id, c.Titre, c.DateCible, c.Icone.ToString()))
                .ToListAsync());

        app.MapPost("/api/comptes-a-rebours", async (CompteARebourRequete requete, HouseOsDbContext db) =>
        {
            var compte = new CompteARebours { Id = Guid.NewGuid(), Titre = string.Empty };
            var erreur = Convertir(requete, compte);
            if (erreur is not null)
            {
                return erreur;
            }

            db.ComptesARebours.Add(compte);
            await db.SaveChangesAsync();
            return Results.Created($"/api/comptes-a-rebours/{compte.Id}",
                new CompteARebourDto(compte.Id, compte.Titre, compte.DateCible, compte.Icone.ToString()));
        });

        app.MapPut("/api/comptes-a-rebours/{id:guid}", async (Guid id, CompteARebourRequete requete, HouseOsDbContext db) =>
        {
            var compte = await db.ComptesARebours.FindAsync(id);
            if (compte is null)
            {
                return Results.NotFound();
            }

            var erreur = Convertir(requete, compte);
            if (erreur is not null)
            {
                return erreur;
            }

            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        app.MapDelete("/api/comptes-a-rebours/{id:guid}", async (Guid id, HouseOsDbContext db) =>
        {
            var compte = await db.ComptesARebours.FindAsync(id);
            if (compte is null)
            {
                return Results.NotFound();
            }

            db.ComptesARebours.Remove(compte);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        return app;
    }

    private static IResult? Convertir(CompteARebourRequete requete, CompteARebours compte)
    {
        if (string.IsNullOrWhiteSpace(requete.Titre))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["titre"] = ["Le titre est requis."],
            });
        }
        if (requete.DateCible is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["dateCible"] = ["La date cible est requise."],
            });
        }
        var icone = IconeCompteARebours.Soleil;
        if (requete.Icone is not null && Enum.TryParse(requete.Icone, out icone) == false)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["icone"] = ["Icône inconnue."],
            });
        }

        compte.Titre = requete.Titre.Trim();
        compte.DateCible = requete.DateCible.Value;
        compte.Icone = icone;
        return null;
    }
}
