using System.Text;
using HouseOs.Api.Domaine;
using HouseOs.Api.Features.Taches;
using HouseOs.Api.Infrastructure;
using HouseOs.Api.Infrastructure.Journalisation;
using Microsoft.EntityFrameworkCore;

namespace HouseOs.Api.Features.Budget;

public record RapportImportDto(int Importees, int Doublons, int Anterieures);

/// <summary>Une ligne de relevé tapée à la main : montant signé (négatif = retrait).
/// <paramref name="Forcer"/> accepte un doublon exact (même date, montant, description)
/// quand la banque a vraiment passé deux lignes identiques.</summary>
public record SaisieTransactionRequete(DateOnly Date, decimal Montant, string Description, bool Forcer = false);

/// <summary>Issue d'une saisie manuelle : la transaction créée, ou le refus (champ +
/// message), <paramref name="Doublon"/> distinguant « déjà saisie » d'une validation.</summary>
public sealed record IssueSaisie(TransactionBancaire? Transaction, ErreurValidation? Erreur, bool Doublon = false)
{
    public static IssueSaisie Refus(string champ, string message, bool doublon = false) =>
        new(null, new ErreurValidation(champ, message), doublon);
}

/// <summary>
/// Entrée des transactions bancaires : téléversement d'un export CSV/OFX — web seulement,
/// comme les documents (l'upload de fichiers est exclu du MCP) — et saisie manuelle d'une
/// ligne (web et MCP), pour les comptes dont la banque n'offre aucun export (D-2026-10-19
/// Saisie Manuelle De Transaction). Dédup par FITID/hash : réimporter le même fichier
/// est sans effet (D-2026-08-26 Import Manuel D'abord Sync Ensuite).
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

        app.MapPost("/api/budget/transactions", async (SaisieTransactionRequete requete, HouseOsDbContext db) =>
        {
            var compte = await db.ComptesBudget.OrderBy(c => c.CreeLe).FirstOrDefaultAsync();
            if (compte is null)
            {
                return Results.Conflict(new { message = "Ancrer le compte avant de saisir une ligne." });
            }
            var issue = await SaisirAsync(db, compte, requete, BudgetEndpoints.Aujourdhui());
            if (issue.Erreur is { } erreur)
            {
                return issue.Doublon
                    ? Results.Conflict(new { message = erreur.Message })
                    : ResultatsApi.Erreur(journal, erreur);
            }
            await db.SaveChangesAsync();
            var transaction = issue.Transaction!;
            journal.LogInformation(
                "Saisie manuelle — {Date} {Montant} « {Description} » → {TransactionId}.",
                transaction.Date, transaction.Montant, transaction.Description, transaction.Id);
            return Results.Created(
                $"/api/budget/transactions/{transaction.Id}",
                new TransactionDto(transaction.Id, transaction.Date, transaction.Montant,
                    transaction.Description, nameof(StatutTransaction.Nouvelle), null, null, false));
        });

        return app;
    }

    /// <summary>
    /// Saisie manuelle d'une ligne de relevé (web et MCP) ; ajoute sans sauvegarder.
    /// Mêmes gardes que l'import : date après l'ancrage, jamais dans le futur (une ligne
    /// de banque est un fait, pas une prévision — les prévisions sont les enveloppes).
    /// La clé de dédup est celle d'un import sans numéro de séquence : une ligne identique
    /// déjà en base est refusée (rejeu, double clic) sauf <see cref="SaisieTransactionRequete.Forcer"/>,
    /// qui prend le rang d'occurrence libre suivant — comme deux doublons intra-fichier.
    /// </summary>
    public static async Task<IssueSaisie> SaisirAsync(
        HouseOsDbContext db, CompteBudget compte, SaisieTransactionRequete requete, DateOnly aujourdhui)
    {
        var description = (requete.Description ?? "").Trim();
        if (description.Length == 0)
        {
            return IssueSaisie.Refus("description", "La description est requise.");
        }
        if (description.Length > 300)
        {
            return IssueSaisie.Refus("description", "La description dépasse 300 caractères.");
        }
        if (requete.Montant == 0)
        {
            return IssueSaisie.Refus("montant", "Le montant ne peut pas être zéro (négatif = retrait).");
        }
        if (requete.Date < compte.DateAncrage)
        {
            return IssueSaisie.Refus("date",
                $"Antérieure à l'ancrage du compte ({compte.DateAncrage:yyyy-MM-dd}) : elle vit à la banque.");
        }
        if (requete.Date > aujourdhui)
        {
            return IssueSaisie.Refus("date", "Une ligne de relevé ne peut pas être dans le futur.");
        }

        var cle = TransactionBancaire.CalculerCleDedup(null, requete.Date, requete.Montant, description);
        var existantes = await db.TransactionsBancaires
            .Where(t => t.CompteBudgetId == compte.Id && t.Date == requete.Date && t.Montant == requete.Montant)
            .Select(t => t.CleDedup)
            .ToListAsync();
        if (existantes.Contains(cle))
        {
            if (requete.Forcer == false)
            {
                return IssueSaisie.Refus("description",
                    "Une ligne identique (même date, montant et description) existe déjà — " +
                    "forcer si la banque en a vraiment passé deux.", doublon: true);
            }
            var occurrence = 1;
            while (existantes.Contains(cle = TransactionBancaire.CalculerCleDedup(
                null, requete.Date, requete.Montant, description, occurrence: occurrence)))
            {
                occurrence++;
            }
        }

        var transaction = new TransactionBancaire
        {
            Id = Guid.NewGuid(),
            CompteBudgetId = compte.Id,
            Date = requete.Date,
            Montant = requete.Montant,
            Description = description,
            CleDedup = cle,
            ImporteeLe = DateTimeOffset.UtcNow,
        };
        db.TransactionsBancaires.Add(transaction);
        return new IssueSaisie(transaction, null);
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
