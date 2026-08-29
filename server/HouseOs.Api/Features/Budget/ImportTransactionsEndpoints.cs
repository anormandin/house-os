using System.Text;
using HouseOs.Api.Domaine;
using HouseOs.Api.Infrastructure;
using HouseOs.Api.Infrastructure.Journalisation;
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
        var journal = app.JournalPour("Budget");

        app.MapPost("/api/budget/import", async (
            HttpRequest requete,
            HouseOsDbContext db,
            IFournisseurTransactions fournisseur) =>
        {
            var compte = await db.ComptesBudget.OrderBy(c => c.CreeLe).FirstOrDefaultAsync();
            if (compte is null)
            {
                return Results.Conflict(new { message = "Ancrer le compte avant d'importer." });
            }

            var formulaire = await requete.ReadFormAsync();
            var fichier = formulaire.Files.GetFile("fichier");
            if (fichier is null || fichier.Length == 0 || fichier.Length > TailleMax)
            {
                return ResultatsApi.Erreur(journal, "fichier", "Fichier manquant, vide ou trop gros (max 5 Mo).");
            }

            string contenu;
            using (var memoire = new MemoryStream())
            {
                await fichier.CopyToAsync(memoire);
                contenu = DecoderContenu(memoire.ToArray());
            }

            IReadOnlyList<TransactionImportee> lues;
            try
            {
                lues = fournisseur.Lire(contenu, fichier.FileName ?? "");
            }
            catch (FormatFichierException e)
            {
                return ResultatsApi.Erreur(journal, "fichier", e.Message);
            }

            var rapport = await ImporterAsync(db, compte, lues);
            await db.SaveChangesAsync();
            journal.LogInformation(
                "Import bancaire — {NomFichier} ({Taille} octets), {NbLues} ligne(s) lue(s) → "
                + "{Importees} importée(s), {Doublons} doublon(s), {Anterieures} antérieure(s).",
                fichier.FileName, fichier.Length, lues.Count,
                rapport.Importees, rapport.Doublons, rapport.Anterieures);
            return Results.Ok(rapport);
        }).DisableAntiforgery();

        return app;
    }

    /// <summary>UTF-8 strict d'abord ; les exports bancaires québécois sont souvent en
    /// Windows-1252 — sans repli, « DÉPÔT » deviendrait « D�P�T » jusque dans la clé de dédup.</summary>
    internal static string DecoderContenu(byte[] octets)
    {
        try
        {
            return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true)
                .GetString(octets)
                .TrimStart('\uFEFF');
        }
        catch (DecoderFallbackException)
        {
            return Windows1252.GetString(octets);
        }
    }

    private static readonly Encoding Windows1252 = ChargerWindows1252();

    private static Encoding ChargerWindows1252()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(1252);
    }

    /// <summary>Dédup + écart des antérieures à l'ancrage ; ne sauvegarde pas.
    /// Deux transactions légitimes identiques le même jour survivent : le numéro de
    /// séquence CSV (ou, à défaut, le rang du doublon dans le fichier) entre dans la
    /// clé — réimporter le même fichier reste sans effet.</summary>
    public static async Task<RapportImportDto> ImporterAsync(
        HouseOsDbContext db, CompteBudget compte, IReadOnlyList<TransactionImportee> lues)
    {
        var clesExistantes = await db.TransactionsBancaires
            .Where(t => t.CompteBudgetId == compte.Id)
            .Select(t => t.CleDedup)
            .ToListAsync();
        var enBase = new HashSet<string>(clesExistantes);
        var vues = new HashSet<string>(clesExistantes);
        var occurrencesFichier = new Dictionary<string, int>();

        int importees = 0, doublons = 0, anterieures = 0;
        foreach (var lue in lues)
        {
            if (lue.Date < compte.DateAncrage)
            {
                anterieures++;
                continue;
            }
            var cleBase = TransactionBancaire.CalculerCleDedup(
                lue.IdExterne, lue.Date, lue.Montant, lue.Description, lue.NumeroSequence);
            var occurrence = occurrencesFichier.GetValueOrDefault(cleBase);
            occurrencesFichier[cleBase] = occurrence + 1;
            var cle = occurrence == 0
                ? cleBase
                : TransactionBancaire.CalculerCleDedup(
                    lue.IdExterne, lue.Date, lue.Montant, lue.Description, lue.NumeroSequence, occurrence);

            // Compat : les transactions importées avant la capture du numéro de séquence
            // portent la clé sans lui — un réimport d'un fichier déjà passé reste sans effet.
            if (lue.IdExterne is null && lue.NumeroSequence is not null
                && enBase.Contains(TransactionBancaire.CalculerCleDedup(null, lue.Date, lue.Montant, lue.Description)))
            {
                doublons++;
                continue;
            }

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
                IdExterne = Tronquer(lue.IdExterne ?? lue.NumeroSequence, 100),
                CleDedup = cle,
                ImporteeLe = DateTimeOffset.UtcNow,
            });
            importees++;
        }
        return new RapportImportDto(importees, doublons, anterieures);
    }

    private static string? Tronquer(string? valeur, int max) =>
        valeur is { Length: > 0 } && valeur.Length > max ? valeur[..max] : valeur;
}
