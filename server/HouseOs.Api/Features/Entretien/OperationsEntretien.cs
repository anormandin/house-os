using HouseOs.Api.Domaine;
using HouseOs.Api.Features.Taches;
using HouseOs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HouseOs.Api.Features.Entretien;

/// <summary>Un item de pack tel que l'UI et l'agent le voient, avec son état chez nous.</summary>
public record PropositionDto(
    string Cle,
    string Titre,
    string? Description,
    RecurrenceDto Recurrence,
    string? Strategie,
    // Une tâche de même titre existe déjà (liée à cet équipement, ou n'importe où pour
    // le programme de la maison) : proposée cochée-grisée, refusée à l'adoption.
    bool DejaPresente,
    Guid? TacheExistanteId);

/// <param name="Pack">« maison », ou le nom de la catégorie de l'équipement.</param>
/// <param name="Raison">Pourquoi la liste est vide quand elle l'est (équipement pas classé,
/// catégorie sans pack) — null quand il y a des propositions.</param>
public record PropositionsDto(
    string Pack,
    Guid? EquipementId,
    string? Equipement,
    string? Raison,
    List<PropositionDto> Propositions);

public record AdopterRequete(string[] Cles, Guid? EquipementId = null);

public record TacheAdopteeDto(Guid Id, string Titre, DateOnly? Echeance);

/// <summary>Statut = 201 (créées), 400, 404 ou 409, avec l'erreur qui va avec.</summary>
public record ResultatAdoption(int Statut, List<TacheAdopteeDto>? Creees, ErreurValidation? Erreur);

/// <summary>
/// Proposer et adopter les packs d'entretien (vault : D-2026-09-28 Packs D'entretien En
/// Fichier De Données). Partagé REST + MCP ; l'adoption passe par
/// <see cref="OperationsTaches.PreparerTacheAsync"/> pour que la matérialisation de la
/// première occurrence et les invariants restent ceux du moteur.
/// </summary>
public static class OperationsEntretien
{
    public const string PackMaison = "maison";

    /// <summary>Null = équipement introuvable.</summary>
    public static async Task<PropositionsDto?> ProposerAsync(
        HouseOsDbContext db, PacksEntretien packs, Guid? equipementId)
    {
        if (equipementId is null)
        {
            var existantes = await TitresExistantsAsync(db, null);
            return new PropositionsDto(PackMaison, null, null,
                packs.Maison.Count == 0 ? "Aucun programme de la maison dans les packs livrés." : null,
                Marquer(packs.Maison, existantes));
        }

        var equipement = await db.Equipements.AsNoTracking()
            .SingleOrDefaultAsync(e => e.Id == equipementId);
        if (equipement is null)
        {
            return null;
        }
        var (items, raison) = ItemsPour(packs, equipement);
        var titres = items.Count == 0 ? [] : await TitresExistantsAsync(db, equipement.Id);
        return new PropositionsDto(
            equipement.Categorie?.ToString() ?? PackMaison, equipement.Id, equipement.Nom, raison,
            Marquer(items, titres));
    }

    public static async Task<ResultatAdoption> AdopterAsync(
        HouseOsDbContext db,
        PacksEntretien packs,
        AdopterRequete requete,
        Guid createurId,
        DateTimeOffset maintenant,
        DateOnly aujourdhui)
    {
        var cles = (requete.Cles ?? [])
            .Where(c => string.IsNullOrWhiteSpace(c) == false)
            .Select(c => c.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (cles.Count == 0)
        {
            return Refus(400, "cles", "Au moins une clé de pack est requise.");
        }

        Equipement? equipement = null;
        IReadOnlyList<ItemDePack> disponibles = packs.Maison;
        if (requete.EquipementId is { } equipementId)
        {
            equipement = await db.Equipements.SingleOrDefaultAsync(e => e.Id == equipementId);
            if (equipement is null)
            {
                return Refus(404, "equipementId", "Cet équipement n'existe pas (ou plus).");
            }
            var (items, raison) = ItemsPour(packs, equipement);
            if (raison is not null)
            {
                return Refus(400, "equipementId", raison);
            }
            disponibles = items;
        }

        var retenus = new List<ItemDePack>();
        foreach (var cle in cles)
        {
            var item = disponibles.FirstOrDefault(i => i.Cle == cle);
            if (item is null)
            {
                var ailleurs = packs.Trouver(cle) is not null;
                return Refus(400, "cles", ailleurs
                    ? $"La clé « {cle} » n'est pas du pack {(equipement is null ? "de la maison" : "de cet équipement")}."
                    : $"Clé de pack inconnue : « {cle} ».");
            }
            retenus.Add(item);
        }

        var existantes = await TitresExistantsAsync(db, equipement?.Id);
        foreach (var item in retenus)
        {
            if (existantes.ContainsKey(Texte.Normaliser(item.Titre)))
            {
                return Refus(409, "cles", $"« {item.Titre} » est déjà dans vos tâches ({item.Cle}).");
            }
        }

        var creees = new List<Tache>();
        foreach (var item in retenus)
        {
            var demande = new CreerTacheRequete(
                item.Titre, item.Description, Echeance: null, AssigneAId: null,
                ZoneId: equipement?.ZoneId, EquipementId: equipement?.Id,
                Strategie: item.Strategie, Recurrence: item.Recurrence);
            var (tache, erreur) = await OperationsTaches.PreparerTacheAsync(db, demande, createurId, maintenant, aujourdhui);
            if (erreur is not null)
            {
                // Tout-ou-rien : les tâches déjà attachées au contexte partiraient au
                // prochain SaveChanges du même scope.
                db.ChangeTracker.Clear();
                return Refus(400, erreur.Champ, $"{item.Cle} : {erreur.Message}");
            }
            creees.Add(tache!);
        }
        await db.SaveChangesAsync();

        return new ResultatAdoption(201,
            creees.Select(t => new TacheAdopteeDto(t.Id, t.Titre, t.Occurrences.FirstOrDefault()?.Echeance)).ToList(),
            null);
    }

    private static (IReadOnlyList<ItemDePack> Items, string? Raison) ItemsPour(PacksEntretien packs, Equipement equipement)
    {
        if (equipement.Categorie is null)
        {
            return ([], "Classe d'abord cet équipement : les propositions dépendent de sa catégorie.");
        }
        if (equipement.Categorie == CategorieEquipement.Autre)
        {
            return ([], "La catégorie « Autre » n'a pas de pack d'entretien.");
        }
        var items = packs.Pour(equipement.Categorie.Value);
        return items.Count == 0
            ? ([], $"Aucun pack d'entretien pour la catégorie {equipement.Categorie}.")
            : (items, null);
    }

    /// <summary>Titre normalisé → id de la première tâche qui le porte.</summary>
    private static async Task<Dictionary<string, Guid>> TitresExistantsAsync(HouseOsDbContext db, Guid? equipementId)
    {
        var taches = db.Taches.AsNoTracking();
        if (equipementId is { } id)
        {
            taches = taches.Where(t => t.EquipementId == id);
        }
        var existantes = new Dictionary<string, Guid>(StringComparer.Ordinal);
        foreach (var tache in await taches.Select(t => new { t.Id, t.Titre }).ToListAsync())
        {
            existantes.TryAdd(Texte.Normaliser(tache.Titre), tache.Id);
        }
        return existantes;
    }

    private static List<PropositionDto> Marquer(IReadOnlyList<ItemDePack> items, Dictionary<string, Guid> existantes) =>
        items.Select(item =>
        {
            var presente = existantes.TryGetValue(Texte.Normaliser(item.Titre), out var tacheId);
            return new PropositionDto(item.Cle, item.Titre, item.Description, item.Recurrence!,
                item.Strategie, presente, presente ? tacheId : null);
        }).ToList();

    private static ResultatAdoption Refus(int statut, string champ, string message) =>
        new(statut, null, new ErreurValidation(champ, message));
}
