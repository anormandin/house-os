using HouseOs.Api.Domaine;
using HouseOs.Api.Infrastructure;
using HouseOs.Api.Infrastructure.Journalisation;
using Microsoft.EntityFrameworkCore;

namespace HouseOs.Api.Features.ComptesARebours;

public record CompteARebourRequete(string Titre, DateOnly? DateCible, string? Icone);
public record CompteARebourDto(Guid Id, string Titre, DateOnly DateCible, string Icone);

public static class ComptesAReboursEndpoints
{
    public static IEndpointRouteBuilder MapComptesARebours(this IEndpointRouteBuilder app)
    {
        var journal = app.JournalPour("ComptesARebours");

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
            var erreur = Convertir(journal, requete, compte);
            if (erreur is not null)
            {
                return erreur;
            }

            db.ComptesARebours.Add(compte);
            await db.SaveChangesAsync();
            journal.LogInformation(
                "Compte à rebours {CompteId} créé — « {Titre} » au {DateCible}.",
                compte.Id, compte.Titre, compte.DateCible);
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

            var erreur = Convertir(journal, requete, compte);
            if (erreur is not null)
            {
                return erreur;
            }

            await db.SaveChangesAsync();
            journal.LogInformation(
                "Compte à rebours {CompteId} modifié — « {Titre} » au {DateCible}.",
                compte.Id, compte.Titre, compte.DateCible);
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
            journal.LogInformation(
                "Compte à rebours {CompteId} supprimé — « {Titre} ».", compte.Id, compte.Titre);
            return Results.NoContent();
        });

        return app;
    }

    private static IResult? Convertir(ILogger journal, CompteARebourRequete requete, CompteARebours compte)
    {
        if (string.IsNullOrWhiteSpace(requete.Titre))
        {
            return ResultatsApi.Erreur(journal, "titre", "Le titre est requis.");
        }
        if (requete.Titre.Trim().Length > 200)
        {
            return ResultatsApi.Erreur(journal, "titre", "Le titre ne peut pas dépasser 200 caractères.");
        }
        if (requete.DateCible is null)
        {
            return ResultatsApi.Erreur(journal, "dateCible", "La date cible est requise.");
        }
        // Icône omise = conserver l'existante, comme le MCP ; à la création, le
        // défaut de l'entité (Soleil) s'applique.
        var icone = compte.Icone;
        if (requete.Icone is not null && Mcp.Conversions.ParserEnum(requete.Icone, out icone) == false)
        {
            return ResultatsApi.Erreur(journal, "icone", "Icône inconnue.");
        }

        compte.Titre = requete.Titre.Trim();
        compte.DateCible = requete.DateCible.Value;
        compte.Icone = icone;
        return null;
    }
}
