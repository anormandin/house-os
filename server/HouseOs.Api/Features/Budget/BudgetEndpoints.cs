using HouseOs.Api.Domaine;
using HouseOs.Api.Infrastructure;
using HouseOs.Api.Infrastructure.Journalisation;
using Microsoft.EntityFrameworkCore;

namespace HouseOs.Api.Features.Budget;

public record CompteBudgetDto(
    Guid Id,
    string Nom,
    string? Institution,
    decimal SoldeInitial,
    DateOnly DateAncrage,
    Guid? TacheVirementId,
    string? TitreTacheVirement);

public record CompteBudgetRequete(
    string Nom,
    string? Institution,
    decimal SoldeInitial,
    DateOnly? DateAncrage,
    Guid? TacheVirementId);

public record EnveloppeDto(
    Guid Id,
    string Nom,
    string Type,
    decimal? MontantCible,
    DateOnly? DateCible,
    DateOnly? DateEffective,
    Guid? TacheId,
    string? TitreTache,
    Guid? EquipementId,
    string? NomEquipement,
    List<Versement>? Echeancier,
    string Statut,
    decimal Solde,
    decimal Provision,
    bool EnRetard,
    bool EcheancierARenouveler);

public record EnveloppeRequete(
    string Nom,
    string Type,
    decimal? MontantCible,
    DateOnly? DateCible,
    Guid? TacheId,
    Guid? EquipementId,
    List<Versement>? Echeancier);

public record MouvementDto(
    Guid Id,
    DateOnly Date,
    decimal Montant,
    string Type,
    string? Note,
    Guid? TransactionBancaireId,
    string? DescriptionTransaction,
    Guid? EntreeJournalId,
    decimal SoldeApres);

public record EnveloppeDetailDto(EnveloppeDto Enveloppe, List<MouvementDto> Mouvements);

public record MouvementRequete(string Type, decimal Montant, DateOnly? Date, string? Note);

public record TransfertRequete(Guid DeEnveloppeId, Guid VersEnveloppeId, decimal Montant, string? Note);

public record SortiePrevueDto(DateOnly Date, string Nom, decimal Montant, Guid EnveloppeId);

public record ResumeBudgetDto(
    CompteBudgetDto? Compte,
    decimal SoldeCourant,
    decimal TotalEnveloppes,
    decimal NonAffecte,
    decimal VirementSuggere,
    Guid? OccurrenceVirementId,
    List<EnveloppeDto> Enveloppes,
    List<SortiePrevueDto> Sorties,
    int NbTransactionsNouvelles);

public record TransactionDto(
    Guid Id,
    DateOnly Date,
    decimal Montant,
    string Description,
    string Statut,
    Guid? SuggestionEnveloppeId,
    string? SuggestionNom,
    bool SuggererVentilation);

public record VentilationLigne(Guid EnveloppeId, decimal Montant);

public record LierRequete(List<VentilationLigne> Ventilation, Guid? EntreeJournalId, string? Note);

/// <summary>Issue d'une restauration de transaction ignorée.</summary>
public enum StatutRestauration
{
    Restauree,
    Introuvable,
    PasIgnoree,
}

public static class BudgetEndpoints
{
    public static IEndpointRouteBuilder MapBudget(this IEndpointRouteBuilder app)
    {
        var journal = app.JournalPour("Budget");

        app.MapGet("/api/budget", async (DateOnly? date, HouseOsDbContext db) =>
            Results.Ok(await ChargerResumeAsync(db, date ?? Aujourdhui())));

        app.MapPost("/api/budget/compte", async (CompteBudgetRequete requete, HouseOsDbContext db) =>
        {
            if (await db.ComptesBudget.AnyAsync())
            {
                return Results.Conflict(new { message = "Le compte est déjà ancré (le modifier plutôt)." });
            }
            var compte = new CompteBudget
            {
                Id = Guid.NewGuid(),
                Nom = string.Empty,
                CreeLe = DateTimeOffset.UtcNow,
            };
            if (await AppliquerCompte(requete, compte, db) is { } erreur)
            {
                return ResultatsApi.Erreur(journal, erreur.Champ, erreur.Message);
            }
            db.ComptesBudget.Add(compte);
            await db.SaveChangesAsync();
            journal.LogInformation("Compte budget {CompteId} ancré — « {Nom} ».", compte.Id, compte.Nom);
            return Results.Created("/api/budget", new { compte.Id });
        });

        app.MapPut("/api/budget/compte", async (CompteBudgetRequete requete, HouseOsDbContext db) =>
        {
            var compte = await db.ComptesBudget.OrderBy(c => c.CreeLe).FirstOrDefaultAsync();
            if (compte is null)
            {
                return Results.NotFound();
            }
            if (await AppliquerCompte(requete, compte, db) is { } erreur)
            {
                return ResultatsApi.Erreur(journal, erreur.Champ, erreur.Message);
            }
            await db.SaveChangesAsync();
            journal.LogInformation("Compte budget {CompteId} modifié — « {Nom} ».", compte.Id, compte.Nom);
            return Results.NoContent();
        });

        app.MapPost("/api/budget/enveloppes", async (EnveloppeRequete requete, HouseOsDbContext db) =>
        {
            var enveloppe = new Enveloppe
            {
                Id = Guid.NewGuid(),
                Nom = string.Empty,
                CreeLe = DateTimeOffset.UtcNow,
            };
            if (await AppliquerEnveloppe(requete, enveloppe, db) is { } erreur)
            {
                return ResultatsApi.Erreur(journal, erreur.Champ, erreur.Message);
            }
            db.Enveloppes.Add(enveloppe);
            await db.SaveChangesAsync();
            journal.LogInformation(
                "Enveloppe {EnveloppeId} créée — « {Nom} ».", enveloppe.Id, enveloppe.Nom);
            return Results.Created("/api/budget", new { enveloppe.Id });
        });

        app.MapPut("/api/budget/enveloppes/{id:guid}", async (
            Guid id, EnveloppeRequete requete, HouseOsDbContext db) =>
        {
            var enveloppe = await db.Enveloppes.FindAsync(id);
            if (enveloppe is null)
            {
                return Results.NotFound();
            }
            if (await AppliquerEnveloppe(requete, enveloppe, db) is { } erreur)
            {
                return ResultatsApi.Erreur(journal, erreur.Champ, erreur.Message);
            }
            await db.SaveChangesAsync();
            journal.LogInformation(
                "Enveloppe {EnveloppeId} modifiée — « {Nom} ».", enveloppe.Id, enveloppe.Nom);
            return Results.NoContent();
        });

        app.MapGet("/api/budget/enveloppes/{id:guid}", async (
            Guid id, DateOnly? date, HouseOsDbContext db) =>
        {
            var detail = await ChargerEnveloppeDetailAsync(db, id, date ?? Aujourdhui());
            return detail is null ? Results.NotFound() : Results.Ok(detail);
        });

        app.MapPost("/api/budget/enveloppes/{id:guid}/fermer", async (Guid id, HouseOsDbContext db) =>
        {
            var enveloppe = await db.Enveloppes.FindAsync(id);
            if (enveloppe is null)
            {
                return Results.NotFound();
            }
            var solde = await SoldeEnveloppeAsync(db, id);
            try
            {
                enveloppe.Fermer(solde);
            }
            catch (InvalidOperationException e)
            {
                journal.LogWarning(
                    "Fermeture refusée de l'enveloppe {EnveloppeId} — {Raison}.", id, e.Message);
                return Results.Conflict(new { message = e.Message });
            }
            await db.SaveChangesAsync();
            journal.LogInformation(
                "Enveloppe {EnveloppeId} fermée — « {Nom} », solde {Solde}.", id, enveloppe.Nom, solde);
            return Results.NoContent();
        });

        app.MapPost("/api/budget/enveloppes/{id:guid}/mouvements", async (
            Guid id, MouvementRequete requete, HouseOsDbContext db) =>
        {
            var enveloppe = await db.Enveloppes.FindAsync(id);
            if (enveloppe is null)
            {
                return Results.NotFound();
            }
            if (ValiderMouvement(requete, enveloppe, out var type) is { } erreur)
            {
                return ResultatsApi.Erreur(journal, erreur.Champ, erreur.Message);
            }
            db.MouvementsEnveloppe.Add(new MouvementEnveloppe
            {
                Id = Guid.NewGuid(),
                EnveloppeId = id,
                Date = requete.Date ?? Aujourdhui(),
                Montant = requete.Montant,
                Type = type,
                Note = Nettoyer(requete.Note),
                CreeLe = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync();
            journal.LogInformation(
                "Mouvement {Type} de {Montant} sur l'enveloppe {EnveloppeId} — « {Nom} ».",
                type, requete.Montant, id, enveloppe.Nom);
            return Results.NoContent();
        });

        app.MapPost("/api/budget/transferts", async (TransfertRequete requete, HouseOsDbContext db) =>
        {
            var erreur = await TransfererAsync(db, requete, Aujourdhui());
            if (erreur is not null)
            {
                return ResultatsApi.Erreur(journal, "transfert", erreur);
            }
            await db.SaveChangesAsync();
            journal.LogInformation(
                "Transfert de {Montant} de l'enveloppe {DeEnveloppeId} vers {VersEnveloppeId}.",
                requete.Montant, requete.DeEnveloppeId, requete.VersEnveloppeId);
            return Results.NoContent();
        });

        app.MapGet("/api/budget/transactions", async (
            string? statut, DateOnly? date, HouseOsDbContext db) =>
        {
            var statutFiltre = StatutTransaction.Nouvelle;
            if (string.IsNullOrWhiteSpace(statut) == false
                && Mcp.Conversions.ParserEnum(statut, out statutFiltre) == false)
            {
                return ResultatsApi.Erreur(journal, "statut", "Statut inconnu (Nouvelle, Liee ou Ignoree).");
            }
            return Results.Ok(await ChargerTransactionsAsync(db, statutFiltre, date ?? Aujourdhui()));
        });

        app.MapPost("/api/budget/transactions/{id:guid}/lier", async (
            Guid id, LierRequete requete, HouseOsDbContext db) =>
        {
            var erreur = await LierAsync(db, id, requete, journal);
            journal.LogInformation(
                "Transaction {TransactionId} liée — {Issue}.", id, erreur is null ? "ok" : "refusée");
            return erreur ?? Results.NoContent();
        });

        app.MapPost("/api/budget/transactions/{id:guid}/ignorer", async (Guid id, HouseOsDbContext db) =>
        {
            var transaction = await db.TransactionsBancaires.FindAsync(id);
            if (transaction is null)
            {
                return Results.NotFound();
            }
            if (transaction.Statut != StatutTransaction.Nouvelle)
            {
                return Results.Conflict(new { message = "Transaction déjà traitée." });
            }
            transaction.Statut = StatutTransaction.Ignoree;
            await db.SaveChangesAsync();
            journal.LogInformation("Transaction {TransactionId} ignorée.", id);
            return Results.NoContent();
        });

        app.MapPost("/api/budget/transactions/{id:guid}/restaurer", async (Guid id, HouseOsDbContext db) =>
        {
            var statut = await RestaurerAsync(db, id);
            journal.LogInformation("Restauration de la transaction {TransactionId} → {Statut}.", id, statut);
            return statut switch
            {
                StatutRestauration.Introuvable => Results.NotFound(),
                StatutRestauration.PasIgnoree =>
                    Results.Conflict(new { message = "Seule une transaction ignorée se restaure." }),
                _ => Results.NoContent(),
            };
        });

        return app;
    }

    public static DateOnly Aujourdhui() => DateOnly.FromDateTime(DateTime.Now);

    /// <summary>Restaure une transaction ignorée vers Nouvelle (web et MCP) —
    /// Ignoree est le seul statut restaurable. Fait SaveChanges.</summary>
    public static async Task<StatutRestauration> RestaurerAsync(HouseOsDbContext db, Guid id)
    {
        var transaction = await db.TransactionsBancaires.FindAsync(id);
        if (transaction is null)
        {
            return StatutRestauration.Introuvable;
        }
        if (transaction.Statut != StatutTransaction.Ignoree)
        {
            return StatutRestauration.PasIgnoree;
        }
        transaction.Statut = StatutTransaction.Nouvelle;
        await db.SaveChangesAsync();
        return StatutRestauration.Restauree;
    }

    /// <summary>Le résumé complet de la page Budget — partagé avec l'outil MCP bilan_budget.</summary>
    public static async Task<ResumeBudgetDto> ChargerResumeAsync(HouseOsDbContext db, DateOnly aujourdhui)
    {
        var compte = await db.ComptesBudget
            .Include(c => c.TacheVirement)
            .AsNoTracking()
            .OrderBy(c => c.CreeLe)
            .FirstOrDefaultAsync();

        // Filtre sur l'ancrage : l'import écarte déjà les antérieures, mais un
        // ré-ancrage à une date plus tardive doit aussi les sortir du solde.
        var soldeCourant = compte is null
            ? 0
            : MoteurProvision.SoldeCompte(
                compte.SoldeInitial,
                await db.TransactionsBancaires
                    .Where(t => t.CompteBudgetId == compte.Id && t.Date >= compte.DateAncrage)
                    .Select(t => t.Montant)
                    .ToListAsync());

        var enveloppes = await ChargerEnveloppesAsync(db, aujourdhui);
        var actives = enveloppes.Where(e => e.Statut == nameof(StatutEnveloppe.Active)).ToList();
        var totalEnveloppes = enveloppes.Sum(e => e.Solde);
        var virementSuggere = actives.Sum(e => e.Provision);

        var sorties = new List<SortiePrevueDto>();
        foreach (var enveloppe in actives)
        {
            if (enveloppe.Echeancier is { Count: > 0 })
            {
                sorties.AddRange(enveloppe.Echeancier
                    .Where(v => v.Date >= aujourdhui)
                    .Select(v => new SortiePrevueDto(v.Date, enveloppe.Nom, v.Montant, enveloppe.Id)));
            }
            else if (enveloppe is { MontantCible: { } cible, DateEffective: { } date })
            {
                sorties.Add(new SortiePrevueDto(date, enveloppe.Nom, cible, enveloppe.Id));
            }
        }

        Guid? occurrenceVirementId = compte?.TacheVirementId is { } tacheVirementId
            ? await db.Occurrences
                .Where(o => o.TacheId == tacheVirementId && o.Statut == StatutOccurrence.EnAttente)
                .Select(o => (Guid?)o.Id)
                .FirstOrDefaultAsync()
            : null;

        return new ResumeBudgetDto(
            compte is null
                ? null
                : new CompteBudgetDto(
                    compte.Id, compte.Nom, compte.Institution, compte.SoldeInitial,
                    compte.DateAncrage, compte.TacheVirementId, compte.TacheVirement?.Titre),
            soldeCourant,
            totalEnveloppes,
            MoteurProvision.NonAffecte(soldeCourant, enveloppes.Select(e => e.Solde)),
            virementSuggere,
            occurrenceVirementId,
            enveloppes,
            sorties.OrderBy(s => s.Date).ToList(),
            compte is null
                ? 0
                : await db.TransactionsBancaires.CountAsync(t =>
                    t.CompteBudgetId == compte.Id && t.Statut == StatutTransaction.Nouvelle));
    }

    /// <summary>Inbox de rapprochement avec suggestions — partagé avec bilan_budget.</summary>
    public static async Task<List<TransactionDto>> ChargerTransactionsAsync(
        HouseOsDbContext db, StatutTransaction statut, DateOnly aujourdhui)
    {
        var transactions = await db.TransactionsBancaires
            .Where(t => t.Statut == statut)
            .OrderByDescending(t => t.Date).ThenByDescending(t => t.ImporteeLe)
            .AsNoTracking()
            .ToListAsync();
        if (transactions.Count == 0)
        {
            return [];
        }

        // Historique des liaisons (marchand → enveloppe) pour l'heuristique de suggestion.
        var liaisonsPassees = await db.MouvementsEnveloppe
            .Where(m => m.TransactionBancaireId != null && m.Montant < 0)
            .Join(db.TransactionsBancaires,
                m => m.TransactionBancaireId, t => t.Id,
                (m, t) => new { t.Description, m.EnveloppeId })
            .ToListAsync();
        var nomsEnveloppes = await db.Enveloppes.ToDictionaryAsync(e => e.Id, e => e.Nom);

        var virementSuggere = statut == StatutTransaction.Nouvelle
            ? (await ChargerResumeAsync(db, aujourdhui)).VirementSuggere
            : 0;

        return transactions.Select(t =>
        {
            Guid? suggestion = t.Montant < 0
                ? SuggestionRapprochement.Suggerer(
                    t.Description, liaisonsPassees.Select(l => (l.Description, l.EnveloppeId)))
                : null;
            return new TransactionDto(
                t.Id, t.Date, t.Montant, t.Description, t.Statut.ToString(),
                suggestion,
                suggestion is { } s ? nomsEnveloppes.GetValueOrDefault(s) : null,
                t.Montant > 0 && SuggestionRapprochement.EstProcheDuVirement(t.Montant, virementSuggere));
        }).ToList();
    }

    /// <summary>Détail d'une enveloppe : fiche + mouvements avec solde après chacun.</summary>
    public static async Task<EnveloppeDetailDto?> ChargerEnveloppeDetailAsync(
        HouseOsDbContext db, Guid id, DateOnly aujourdhui)
    {
        var enveloppes = await ChargerEnveloppesAsync(db, aujourdhui);
        var enveloppe = enveloppes.FirstOrDefault(e => e.Id == id);
        if (enveloppe is null)
        {
            return null;
        }

        var mouvements = await db.MouvementsEnveloppe
            .Where(m => m.EnveloppeId == id)
            .OrderBy(m => m.Date).ThenBy(m => m.CreeLe)
            .Select(m => new
            {
                m.Id, m.Date, m.Montant, m.Type, m.Note,
                m.TransactionBancaireId,
                DescriptionTransaction = m.TransactionBancaire == null ? null : m.TransactionBancaire.Description,
                m.EntreeJournalId,
            })
            .ToListAsync();

        decimal cumul = 0;
        var dtos = mouvements.Select(m =>
        {
            cumul += m.Montant;
            return new MouvementDto(
                m.Id, m.Date, m.Montant, m.Type.ToString(), m.Note,
                m.TransactionBancaireId, m.DescriptionTransaction, m.EntreeJournalId, cumul);
        }).ToList();
        dtos.Reverse(); // du plus récent au plus ancien, solde après déjà calculé
        return new EnveloppeDetailDto(enveloppe, dtos);
    }

    /// <summary>Lie une transaction (retrait mono-enveloppe, dépôt ventilé) et sauvegarde.
    /// La réclamation du statut est un UPDATE conditionnel sous transaction : un rejeu
    /// concurrent (timeout MCP, deux navigateurs) obtient un conflit au lieu de doubler
    /// les mouvements. Null = succès.</summary>
    public static async Task<IResult?> LierAsync(
        HouseOsDbContext db, Guid id, LierRequete requete, ILogger journal)
    {
        var transaction = await db.TransactionsBancaires.FindAsync(id);
        if (transaction is null)
        {
            return Results.NotFound();
        }
        if (transaction.Statut != StatutTransaction.Nouvelle)
        {
            return Results.Conflict(new { message = "Transaction déjà traitée." });
        }
        await using var portee = await db.Database.BeginTransactionAsync();
        var reclamees = await db.TransactionsBancaires
            .Where(t => t.Id == id && t.Statut == StatutTransaction.Nouvelle)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.Statut, StatutTransaction.Liee));
        if (reclamees == 0)
        {
            return Results.Conflict(new { message = "Transaction déjà traitée." });
        }
        var erreur = await CreerLiaisonAsync(db, transaction, requete);
        if (erreur is { } e)
        {
            return ResultatsApi.Erreur(journal, e.Champ, e.Message); // rollback à la sortie de la portée
        }
        await db.SaveChangesAsync();
        await portee.CommitAsync();
        return null;
    }

    /// <summary>Cœur de la liaison, partagé avec le MCP. Null = valide (mouvements ajoutés).</summary>
    public static async Task<(string Champ, string Message)?> CreerLiaisonAsync(
        HouseOsDbContext db, TransactionBancaire transaction, LierRequete requete)
    {
        if (requete.Ventilation is not { Count: > 0 } ventilation)
        {
            return ("ventilation", "Au moins une enveloppe est requise.");
        }
        if (ventilation.Select(v => v.EnveloppeId).Distinct().Count() != ventilation.Count)
        {
            return ("ventilation", "Une même enveloppe apparaît deux fois.");
        }
        var ids = ventilation.Select(v => v.EnveloppeId).ToList();
        var enveloppes = await db.Enveloppes.Where(e => ids.Contains(e.Id)).ToListAsync();
        if (enveloppes.Count != ids.Count)
        {
            return ("ventilation", "Une enveloppe n'existe pas.");
        }
        if (enveloppes.Any(e => e.Statut == StatutEnveloppe.Fermee))
        {
            return ("ventilation", "Une enveloppe fermée ne reçoit plus de mouvements.");
        }

        if (transaction.Montant < 0)
        {
            // Retrait : mono-enveloppe, montant entier de la transaction
            // (le solde d'enveloppe peut passer sous zéro — permis et affiché).
            if (ventilation.Count != 1)
            {
                return ("ventilation", "Un retrait se lie à une seule enveloppe.");
            }
            if (ventilation[0].Montant != -transaction.Montant)
            {
                return ("ventilation",
                    "Le montant de la ventilation doit égaler celui du retrait (valeur absolue).");
            }
            if (requete.EntreeJournalId is { } journalId
                && await db.Journal.AnyAsync(j => j.Id == journalId) == false)
            {
                return ("entreeJournalId", "Cette entrée de journal n'existe pas.");
            }
            db.MouvementsEnveloppe.Add(new MouvementEnveloppe
            {
                Id = Guid.NewGuid(),
                EnveloppeId = ventilation[0].EnveloppeId,
                Date = transaction.Date,
                Montant = transaction.Montant,
                Type = TypeMouvement.Retrait,
                TransactionBancaireId = transaction.Id,
                EntreeJournalId = requete.EntreeJournalId,
                Note = Nettoyer(requete.Note),
                CreeLe = DateTimeOffset.UtcNow,
            });
        }
        else
        {
            // Dépôt ventilé (D-2026-08-26 Ventilation Du Dépôt Multi-Enveloppes) :
            // N mouvements Provision liés à la même transaction, le reste demeure
            // en Non affecté (aucun mouvement).
            if (ventilation.Any(v => v.Montant <= 0))
            {
                return ("ventilation", "Chaque part doit être positive.");
            }
            if (ventilation.Sum(v => v.Montant) > transaction.Montant)
            {
                return ("ventilation", "La ventilation dépasse le montant du dépôt.");
            }
            if (requete.EntreeJournalId is not null)
            {
                return ("entreeJournalId", "Le lien au journal ne s'applique qu'à un retrait.");
            }
            foreach (var ligne in ventilation)
            {
                db.MouvementsEnveloppe.Add(new MouvementEnveloppe
                {
                    Id = Guid.NewGuid(),
                    EnveloppeId = ligne.EnveloppeId,
                    Date = transaction.Date,
                    Montant = ligne.Montant,
                    Type = TypeMouvement.Provision,
                    TransactionBancaireId = transaction.Id,
                    Note = Nettoyer(requete.Note),
                    CreeLe = DateTimeOffset.UtcNow,
                });
            }
        }

        transaction.Statut = StatutTransaction.Liee;
        return null;
    }

    /// <summary>Transfert = deux mouvements opposés, même date. Null = succès, sinon message.</summary>
    public static async Task<string?> TransfererAsync(
        HouseOsDbContext db, TransfertRequete requete, DateOnly date)
    {
        if (requete.Montant <= 0)
        {
            return "Le montant doit être positif.";
        }
        if (requete.DeEnveloppeId == requete.VersEnveloppeId)
        {
            return "Choisir deux enveloppes différentes.";
        }
        var enveloppes = await db.Enveloppes
            .Where(e => e.Id == requete.DeEnveloppeId || e.Id == requete.VersEnveloppeId)
            .ToListAsync();
        if (enveloppes.Count != 2)
        {
            return "Une des enveloppes n'existe pas.";
        }
        if (enveloppes.Any(e => e.Statut == StatutEnveloppe.Fermee))
        {
            return "Une enveloppe fermée ne reçoit plus de mouvements.";
        }
        var note = Nettoyer(requete.Note);
        var maintenant = DateTimeOffset.UtcNow;
        db.MouvementsEnveloppe.AddRange(
            new MouvementEnveloppe
            {
                Id = Guid.NewGuid(),
                EnveloppeId = requete.DeEnveloppeId,
                Date = date,
                Montant = -requete.Montant,
                Type = TypeMouvement.Transfert,
                Note = note,
                CreeLe = maintenant,
            },
            new MouvementEnveloppe
            {
                Id = Guid.NewGuid(),
                EnveloppeId = requete.VersEnveloppeId,
                Date = date,
                Montant = requete.Montant,
                Type = TypeMouvement.Transfert,
                Note = note,
                CreeLe = maintenant,
            });
        return null;
    }

    public static async Task<decimal> SoldeEnveloppeAsync(HouseOsDbContext db, Guid enveloppeId) =>
        await db.MouvementsEnveloppe
            .Where(m => m.EnveloppeId == enveloppeId)
            .SumAsync(m => (decimal?)m.Montant) ?? 0;

    /// <summary>Applique une requête d'enveloppe (création et modification, web et MCP).
    /// Null = valide.</summary>
    public static async Task<(string Champ, string Message)?> AppliquerEnveloppe(
        EnveloppeRequete requete, Enveloppe enveloppe, HouseOsDbContext db)
    {
        if (enveloppe.Statut == StatutEnveloppe.Fermee)
        {
            return ("enveloppe",
                "Une enveloppe fermée ne se modifie plus — son historique est figé.");
        }
        if (string.IsNullOrWhiteSpace(requete.Nom))
        {
            return ("nom", "Le nom est requis.");
        }
        if (requete.Nom.Trim().Length > 200)
        {
            return ("nom", "Le nom ne peut pas dépasser 200 caractères.");
        }
        if (Mcp.Conversions.ParserEnum(requete.Type, out TypeEnveloppe type) == false)
        {
            return ("type", "Type inconnu (Equipement, Taxes, Projet ou Reserve).");
        }
        if (requete.MontantCible is < 0)
        {
            return ("montantCible", "La cible ne peut pas être négative.");
        }
        if (requete.TacheId is not null && requete.EquipementId is not null)
        {
            return ("tacheId", "Lier une tâche OU un équipement, pas les deux.");
        }
        if (requete.TacheId is { } tacheId && await db.Taches.AnyAsync(t => t.Id == tacheId) == false)
        {
            return ("tacheId", "Cette tâche n'existe pas (ou plus).");
        }
        if (requete.EquipementId is { } equipementId
            && await db.Equipements.AnyAsync(e => e.Id == equipementId) == false)
        {
            return ("equipementId", "Cet équipement n'existe pas (ou plus).");
        }
        if (requete.Echeancier is { Count: > 0 } && type != TypeEnveloppe.Taxes)
        {
            return ("echeancier", "L'échéancier de versements est réservé au type Taxes.");
        }
        if (requete.Echeancier?.Any(v => v.Montant <= 0) == true)
        {
            return ("echeancier", "Chaque versement doit être positif.");
        }

        enveloppe.Nom = requete.Nom.Trim();
        enveloppe.Type = type;
        enveloppe.MontantCible = requete.MontantCible;
        enveloppe.DateCible = requete.DateCible;
        enveloppe.TacheId = requete.TacheId;
        enveloppe.EquipementId = requete.EquipementId;
        enveloppe.Echeancier = requete.Echeancier is { Count: > 0 }
            ? requete.Echeancier.OrderBy(v => v.Date).ToList()
            : null;
        return null;
    }

    /// <summary>Applique une requête de compte (ancrage et modification, web et MCP).
    /// Null = valide.</summary>
    public static async Task<(string Champ, string Message)?> AppliquerCompte(
        CompteBudgetRequete requete, CompteBudget compte, HouseOsDbContext db)
    {
        if (string.IsNullOrWhiteSpace(requete.Nom))
        {
            return ("nom", "Le nom du compte est requis.");
        }
        if (requete.DateAncrage is null)
        {
            return ("dateAncrage", "La date d'ancrage est requise.");
        }
        if (requete.TacheVirementId is { } tacheId
            && await db.Taches.AnyAsync(t => t.Id == tacheId) == false)
        {
            return ("tacheVirementId", "Cette tâche n'existe pas (ou plus).");
        }
        // Avancer l'ancrage au-delà de transactions déjà liées laisserait leurs
        // mouvements d'enveloppe orphelins du solde — « non affecté » fantôme.
        var lieesAnterieures = await db.TransactionsBancaires.CountAsync(t =>
            t.CompteBudgetId == compte.Id
            && t.Statut == StatutTransaction.Liee
            && t.Date < requete.DateAncrage.Value);
        if (lieesAnterieures > 0)
        {
            return ("dateAncrage",
                $"{lieesAnterieures} transaction(s) liée(s) deviendraient antérieures au nouvel " +
                "ancrage — les délier d'abord ou choisir une date plus ancienne.");
        }
        compte.Nom = requete.Nom.Trim();
        compte.Institution = Nettoyer(requete.Institution);
        compte.SoldeInitial = requete.SoldeInitial;
        compte.DateAncrage = requete.DateAncrage.Value;
        compte.TacheVirementId = requete.TacheVirementId;
        return null;
    }

    /// <summary>Valide un mouvement manuel (web et MCP). Null = valide.</summary>
    public static (string Champ, string Message)? ValiderMouvement(
        MouvementRequete requete, Enveloppe enveloppe, out TypeMouvement type)
    {
        type = default;
        if (enveloppe.Statut == StatutEnveloppe.Fermee)
        {
            return ("enveloppe", "Une enveloppe fermée ne reçoit plus de mouvements.");
        }
        if (Mcp.Conversions.ParserEnum(requete.Type, out type) == false || type == TypeMouvement.Transfert)
        {
            return ("type", "Type inconnu (Provision, Retrait ou Ajustement — " +
                "un transfert passe par le transfert dédié).");
        }
        if (requete.Montant == 0)
        {
            return ("montant", "Le montant ne peut pas être zéro.");
        }
        if (type == TypeMouvement.Retrait && requete.Montant > 0)
        {
            return ("montant", "Un retrait est négatif.");
        }
        if (type == TypeMouvement.Provision && requete.Montant < 0)
        {
            return ("montant", "Une provision est positive.");
        }
        return null;
    }

    private static async Task<List<EnveloppeDto>> ChargerEnveloppesAsync(
        HouseOsDbContext db, DateOnly aujourdhui)
    {
        var enveloppes = await db.Enveloppes
            .Include(e => e.Tache)
            .Include(e => e.Equipement)
            .AsNoTracking()
            .OrderBy(e => e.Nom)
            .ToListAsync();

        var soldes = await db.MouvementsEnveloppe
            .GroupBy(m => m.EnveloppeId)
            .Select(g => new { EnveloppeId = g.Key, Solde = g.Sum(m => m.Montant) })
            .ToDictionaryAsync(s => s.EnveloppeId, s => s.Solde);

        // Échéances dérivées : prochaine occurrence en attente de chaque tâche liée.
        var tachesLiees = enveloppes
            .Where(e => e.TacheId is not null)
            .Select(e => e.TacheId!.Value)
            .Distinct()
            .ToList();
        var echeancesTaches = await db.Occurrences
            .Where(o => tachesLiees.Contains(o.TacheId) && o.Statut == StatutOccurrence.EnAttente)
            .ToDictionaryAsync(o => o.TacheId, o => o.Echeance);

        return enveloppes.Select(e =>
        {
            var solde = soldes.GetValueOrDefault(e.Id);
            DateOnly? echeanceTache = e.TacheId is { } tacheId
                ? echeancesTaches.GetValueOrDefault(tacheId)
                : null;
            var provision = MoteurProvision.Calculer(e, solde, aujourdhui, echeanceTache);
            return new EnveloppeDto(
                e.Id, e.Nom, e.Type.ToString(), e.MontantCible, e.DateCible,
                provision.DateEffective,
                e.TacheId, e.Tache?.Titre,
                e.EquipementId, e.Equipement?.Nom,
                e.Echeancier, e.Statut.ToString(),
                solde, provision.Montant, provision.EnRetard, provision.EcheancierARenouveler);
        }).ToList();
    }

    private static string? Nettoyer(string? valeur) =>
        string.IsNullOrWhiteSpace(valeur) ? null : valeur.Trim();
}
