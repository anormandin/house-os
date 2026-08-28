using System.Runtime.CompilerServices;
using HouseOs.Api.Domaine;
using HouseOs.Api.Domaine.Humeur;
using HouseOs.Api.Domaine.Meteo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HouseOs.Api.Features.Synchro;

/// <summary>
/// Le tier grossier de la synchro : traduit automatiquement toute écriture en un
/// événement de module, sans que les slices aient à y penser. Couvre d'office les
/// écritures HTTP, MCP et les quatre BackgroundService.
///
/// Deux temps, parce que le ChangeTracker est remis à « Unchanged » par la sauvegarde :
/// on capture les modules AVANT (SavingChanges), on diffuse APRÈS (SavedChanges).
///
/// Transactions : MeteoIngestionService sauvegarde dans une transaction explicite.
/// Diffuser au SavedChanges ferait refetcher le client sur des données pas encore
/// visibles, et aucun second événement ne suivrait — périmé jusqu'au prochain focus.
/// Quand une transaction est ouverte, les modules attendent donc le commit.
/// </summary>
public sealed class IntercepteurSynchro(IDiffuseurSynchro diffuseur)
    : ISaveChangesInterceptor, IDbTransactionInterceptor
{
    /// <summary>
    /// Type d'entité → module. Une nouvelle table = une ligne ici, et tout le reste
    /// (invalidation web, MCP, arrière-plan) suit sans autre changement.
    ///
    /// <see cref="Utilisateur"/> est volontairement absent : l'amorçage au démarrage
    /// écrit des utilisateurs et n'a aucun client à prévenir.
    /// </summary>
    private static readonly Dictionary<Type, string> ModuleParType = new()
    {
        [typeof(Tache)] = ModulesSynchro.Taches,
        [typeof(Occurrence)] = ModulesSynchro.Taches,
        [typeof(EntreeJournal)] = ModulesSynchro.Taches,
        [typeof(Zone)] = ModulesSynchro.Zones,
        [typeof(Equipement)] = ModulesSynchro.Equipements,
        [typeof(Document)] = ModulesSynchro.Documents,
        [typeof(CompteARebours)] = ModulesSynchro.ComptesARebours,
        [typeof(PrevisionHoraire)] = ModulesSynchro.Meteo,
        [typeof(PrevisionQuotidienne)] = ModulesSynchro.Meteo,
        [typeof(ReleveMeteo)] = ModulesSynchro.Meteo,
        [typeof(PhraseDuJour)] = ModulesSynchro.PhraseDuJour,
        [typeof(FluxExterne)] = ModulesSynchro.FluxExternes,
        [typeof(EvenementExterne)] = ModulesSynchro.FluxExternes,
        [typeof(CompteBudget)] = ModulesSynchro.Budget,
        [typeof(Enveloppe)] = ModulesSynchro.Budget,
        [typeof(MouvementEnveloppe)] = ModulesSynchro.Budget,
        [typeof(TransactionBancaire)] = ModulesSynchro.Budget,
    };

    /// <summary>Table de jointure sans type CLR (many-to-many Tâche ⇄ Document).</summary>
    private static readonly Dictionary<string, string> ModuleParNomEntite = new()
    {
        ["TacheDocuments"] = ModulesSynchro.Taches,
    };

    /// <summary>
    /// Modules en attente, par instance de DbContext : l'intercepteur est un singleton
    /// partagé par toutes les requêtes, l'état ne peut pas vivre sur lui. La table
    /// faible libère l'entrée avec le contexte, sans fuite.
    /// </summary>
    private readonly ConditionalWeakTable<DbContext, HashSet<string>> _enAttente = new();

    /// <summary>Exposé pour les tests : le module associé à un type, ou null.</summary>
    public static string? ModulePour(Type type) =>
        ModuleParType.TryGetValue(type, out var module) ? module : null;

    // ---- Capture : avant la sauvegarde, tant que les états sont encore lisibles ----

    public InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        Capturer(eventData.Context);
        return result;
    }

    public ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Capturer(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void Capturer(DbContext? contexte)
    {
        if (contexte is null)
        {
            return;
        }

        var modules = _enAttente.GetOrCreateValue(contexte);
        foreach (var entree in contexte.ChangeTracker.Entries())
        {
            if (entree.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            var type = entree.Metadata.ClrType;
            // Un type possédé (la spec de récurrence d'une tâche) peut être seul modifié :
            // il compte pour son propriétaire, sinon l'écriture passerait inaperçue.
            if (ModuleParType.ContainsKey(type) == false && entree.Metadata.IsOwned())
            {
                type = entree.Metadata.FindOwnership()?.PrincipalEntityType.ClrType ?? type;
            }

            if (ModuleParType.TryGetValue(type, out var module))
            {
                modules.Add(module);
            }
            else if (ModuleParNomEntite.TryGetValue(entree.Metadata.Name, out var moduleJointure))
            {
                modules.Add(moduleJointure);
            }
        }
    }

    // ---- Diffusion : après la sauvegarde, ou après le commit si transaction ----

    public int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        if (TransactionOuverte(eventData.Context) == false)
        {
            _ = DiffuserAsync(Vider(eventData.Context), CancellationToken.None);
        }
        return result;
    }

    public async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        if (TransactionOuverte(eventData.Context) == false)
        {
            await DiffuserAsync(Vider(eventData.Context), cancellationToken);
        }
        return result;
    }

    public void SaveChangesFailed(DbContextErrorEventData eventData) => Vider(eventData.Context);

    public Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        Vider(eventData.Context);
        return Task.CompletedTask;
    }

    public void TransactionCommitted(
        System.Data.Common.DbTransaction transaction, TransactionEndEventData eventData) =>
        _ = DiffuserAsync(Vider(eventData.Context), CancellationToken.None);

    public Task TransactionCommittedAsync(
        System.Data.Common.DbTransaction transaction,
        TransactionEndEventData eventData,
        CancellationToken cancellationToken = default) =>
        DiffuserAsync(Vider(eventData.Context), cancellationToken);

    public void TransactionRolledBack(
        System.Data.Common.DbTransaction transaction, TransactionEndEventData eventData) =>
        Vider(eventData.Context);

    public Task TransactionRolledBackAsync(
        System.Data.Common.DbTransaction transaction,
        TransactionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        Vider(eventData.Context);
        return Task.CompletedTask;
    }

    private static bool TransactionOuverte(DbContext? contexte) =>
        contexte?.Database.CurrentTransaction is not null;

    private string[] Vider(DbContext? contexte)
    {
        if (contexte is null || _enAttente.TryGetValue(contexte, out var modules) == false)
        {
            return [];
        }
        var pris = modules.ToArray();
        modules.Clear();
        return pris;
    }

    private async Task DiffuserAsync(string[] modules, CancellationToken annulation)
    {
        foreach (var module in modules)
        {
            try
            {
                await diffuseur.DiffuserAsync(new EvenementSynchro(module), annulation);
            }
            catch
            {
                // On est dans le chemin de SaveChanges, après le commit : laisser
                // remonter une panne de diffusion ferait échouer une écriture déjà
                // durable. L'implémentation de production journalise déjà.
            }
        }
    }
}
