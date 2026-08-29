using HouseOs.Api.Domaine;
using HouseOs.Api.Infrastructure;
using HouseOs.Api.Infrastructure.Journalisation;
using Microsoft.EntityFrameworkCore;

namespace HouseOs.Api.Features.Zones;

public record ZoneRequete(string Nom, string? Type, int? Ordre);
public record ZoneDto(Guid Id, string Nom, string Type, int Ordre);

public static class ZonesEndpoints
{
    public static IEndpointRouteBuilder MapZones(this IEndpointRouteBuilder app)
    {
        var journal = app.JournalPour("Zones");

        app.MapGet("/api/zones", async (HouseOsDbContext db) =>
            await db.Zones
                .OrderBy(z => z.Ordre).ThenBy(z => z.Nom)
                .Select(z => new ZoneDto(z.Id, z.Nom, z.Type.ToString(), z.Ordre))
                .ToListAsync());

        app.MapPost("/api/zones", async (ZoneRequete requete, HouseOsDbContext db) =>
        {
            var (zone, erreur) = Convertir(journal, requete, new Zone { Id = Guid.NewGuid(), Nom = string.Empty });
            if (erreur is not null)
            {
                return erreur;
            }

            db.Zones.Add(zone);
            await db.SaveChangesAsync();
            journal.LogInformation("Zone {ZoneId} créée — « {Nom} » ({Type}).", zone.Id, zone.Nom, zone.Type);
            return Results.Created($"/api/zones/{zone.Id}",
                new ZoneDto(zone.Id, zone.Nom, zone.Type.ToString(), zone.Ordre));
        });

        app.MapPut("/api/zones/{id:guid}", async (Guid id, ZoneRequete requete, HouseOsDbContext db) =>
        {
            var zone = await db.Zones.FindAsync(id);
            if (zone is null)
            {
                return Results.NotFound();
            }

            var (_, erreur) = Convertir(journal, requete, zone);
            if (erreur is not null)
            {
                return erreur;
            }

            await db.SaveChangesAsync();
            journal.LogInformation("Zone {ZoneId} modifiée — « {Nom} ».", zone.Id, zone.Nom);
            return Results.NoContent();
        });

        app.MapDelete("/api/zones/{id:guid}", async (Guid id, HouseOsDbContext db) =>
        {
            var zone = await db.Zones.FindAsync(id);
            if (zone is null)
            {
                return Results.NotFound();
            }

            // Les tâches et équipements de la zone survivent (FK SetNull).
            db.Zones.Remove(zone);
            await db.SaveChangesAsync();
            journal.LogInformation("Zone {ZoneId} supprimée — « {Nom} ».", zone.Id, zone.Nom);
            return Results.NoContent();
        });

        return app;
    }

    private static (Zone Zone, IResult? Erreur) Convertir(ILogger journal, ZoneRequete requete, Zone zone)
    {
        if (string.IsNullOrWhiteSpace(requete.Nom))
        {
            return (zone, ResultatsApi.Erreur(journal, "nom", "Le nom est requis."));
        }
        if (requete.Nom.Trim().Length > 100)
        {
            return (zone, ResultatsApi.Erreur(journal, "nom", "Le nom ne peut pas dépasser 100 caractères."));
        }
        // Type omis = conserver l'existant, comme Ordre et comme le MCP ; à la
        // création, le défaut de l'entité (Interieur) s'applique.
        var type = zone.Type;
        if (requete.Type is not null && Mcp.Conversions.ParserEnum(requete.Type, out type) == false)
        {
            return (zone, ResultatsApi.Erreur(journal, "type", "Type inconnu (Interieur ou Exterieur)."));
        }

        zone.Nom = requete.Nom.Trim();
        zone.Type = type;
        zone.Ordre = requete.Ordre ?? zone.Ordre;
        return (zone, null);
    }
}
