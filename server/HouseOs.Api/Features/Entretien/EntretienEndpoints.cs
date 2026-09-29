using System.Security.Claims;
using HouseOs.Api.Features.Auth;
using HouseOs.Api.Features.Synchro;
using HouseOs.Api.Infrastructure;
using HouseOs.Api.Infrastructure.Journalisation;
using Microsoft.EntityFrameworkCore;

namespace HouseOs.Api.Features.Entretien;

/// <summary>
/// Les packs d'entretien vus par le web : proposer (pour un équipement, ou le programme
/// de la maison) et adopter (créer les tâches cochées, en une transaction). Parité MCP :
/// <c>OutilsEntretien</c>.
/// </summary>
public static class EntretienEndpoints
{
    public static IEndpointRouteBuilder MapEntretien(this IEndpointRouteBuilder app)
    {
        var journal = app.JournalPour("Entretien");

        app.MapGet("/api/entretien/propositions", async (
            Guid? equipementId, HouseOsDbContext db, PacksEntretien packs) =>
        {
            var propositions = await OperationsEntretien.ProposerAsync(db, packs, equipementId);
            return propositions is null ? Results.NotFound() : Results.Ok(propositions);
        });

        app.MapPost("/api/entretien/adopter", async (
            AdopterRequete requete,
            ClaimsPrincipal principal,
            HouseOsDbContext db,
            PacksEntretien packs,
            IDiffuseurSynchro diffuseur) =>
        {
            var resultat = await OperationsEntretien.AdopterAsync(
                db, packs, requete, principal.IdUtilisateur(),
                DateTimeOffset.UtcNow, DateOnly.FromDateTime(DateTime.Now));
            if (resultat.Erreur is { } erreur)
            {
                return resultat.Statut switch
                {
                    404 => Results.NotFound(),
                    409 => Results.Conflict(new { message = erreur.Message }),
                    _ => ResultatsApi.Erreur(journal, erreur),
                };
            }

            var creees = resultat.Creees!;
            var acteur = await db.Utilisateurs.AsNoTracking()
                .Where(u => u.Id == principal.IdUtilisateur())
                .Select(u => u.NomAffichage)
                .FirstOrDefaultAsync();
            // Un lot = un événement, comme creer_taches côté MCP : le décompte voyage
            // dans l'événement et le client fusionne.
            await diffuseur.DiffuserAsync(new EvenementSynchro(
                ModulesSynchro.Taches,
                EvenementSynchro.GenreTachesCreees,
                EvenementSynchro.SourceWeb,
                principal.IdUtilisateur(),
                acteur,
                creees.Count == 1 ? creees[0].Titre : null,
                creees.Count));
            journal.LogInformation(
                "{Nombre} tâche(s) d'entretien adoptée(s) par {ActeurId} (équipement {EquipementId}) : {Cles}.",
                creees.Count, principal.IdUtilisateur(), requete.EquipementId, string.Join(", ", requete.Cles));
            return Results.Created("/api/taches", new { creees });
        });

        return app;
    }
}
