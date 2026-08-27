using System.Text;
using HouseOs.Api.Domaine;
using HouseOs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HouseOs.Api.Features.Budget;

public record RapportImportDto(int Importees, int Doublons, int Anterieures);

/// <summary>
/// Téléversement d'un export bancaire CSV/OFX — web seulement, comme les documents
/// (l'upload de fichiers est exclu du MCP). Dédup par FITID/hash : réimporter le
/// même fichier est sans effet (D-2026-08-26 Import Manuel D'abord Sync Ensuite).
/// </summary>
public static class ImportTransactionsEndpoints
{
    public const long TailleMax = 5 * 1024 * 1024;

    public static IEndpointRouteBuilder MapImportTransactions(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/budget/import", async (
            HttpRequest requete,
            HouseOsDbContext db,
            IFournisseurTransactions fournisseur) =>
        {
            var compte = await db.ComptesBudget.FirstOrDefaultAsync();
            if (compte is null)
            {
                return Results.Conflict(new { message = "Ancrer le compte avant d'importer." });
            }

            var formulaire = await requete.ReadFormAsync();
            var fichier = formulaire.Files.GetFile("fichier");
            if (fichier is null || fichier.Length == 0 || fichier.Length > TailleMax)
            {
                return Erreur("fichier", "Fichier manquant, vide ou trop gros (max 5 Mo).");
            }

            string contenu;
            using (var lecteur = new StreamReader(fichier.OpenReadStream(), Encoding.UTF8))
            {
                contenu = await lecteur.ReadToEndAsync();
            }

            IReadOnlyList<TransactionImportee> lues;
            try
            {
                lues = fournisseur.Lire(contenu, fichier.FileName ?? "");
            }
            catch (FormatFichierException e)
            {
                return Erreur("fichier", e.Message);
            }

            var rapport = await ImporterAsync(db, compte, lues);
            await db.SaveChangesAsync();
            return Results.Ok(rapport);
        }).DisableAntiforgery();

        return app;
    }

    /// <summary>Dédup + écart des antérieures à l'ancrage ; ne sauvegarde pas.</summary>
    public static async Task<RapportImportDto> ImporterAsync(
        HouseOsDbContext db, CompteBudget compte, IReadOnlyList<TransactionImportee> lues)
    {
        var clesExistantes = await db.TransactionsBancaires
            .Where(t => t.CompteBudgetId == compte.Id)
            .Select(t => t.CleDedup)
            .ToListAsync();
        var vues = new HashSet<string>(clesExistantes);

        int importees = 0, doublons = 0, anterieures = 0;
        foreach (var lue in lues)
        {
            if (lue.Date < compte.DateAncrage)
            {
                anterieures++;
                continue;
            }
            var cle = TransactionBancaire.CalculerCleDedup(lue.IdExterne, lue.Date, lue.Montant, lue.Description);
            if (vues.Add(cle) == false)
            {
                doublons++;
                continue;
            }
            db.TransactionsBancaires.Add(new TransactionBancaire
            {
                Id = Guid.NewGuid(),
                CompteBudgetId = compte.Id,
                Date = lue.Date,
                Montant = lue.Montant,
                Description = lue.Description.Length > 300 ? lue.Description[..300] : lue.Description,
                IdExterne = lue.IdExterne,
                CleDedup = cle,
                ImporteeLe = DateTimeOffset.UtcNow,
            });
            importees++;
        }
        return new RapportImportDto(importees, doublons, anterieures);
    }

    private static IResult Erreur(string champ, string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { [champ] = [message] });
}
