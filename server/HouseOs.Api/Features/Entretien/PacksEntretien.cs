using System.Text.Json;
using HouseOs.Api.Domaine;
using HouseOs.Api.Features.Taches;
using HouseOs.Api.Infrastructure;

namespace HouseOs.Api.Features.Entretien;

/// <summary>
/// Le chemin d'un fichier de packs qui remplace celui de l'app. Vide — le cas
/// ordinaire — les packs livrés servent (<see cref="LecturePacks.NomDuFichierParDefaut"/>).
/// `.env` : ENTRETIEN_FICHIER. Un chemin réglé mais illisible ne retombe <b>pas</b> sur
/// les packs livrés : proposer les gouttières d'une maison de zone 4 à un foyer qui a
/// justement écrit les siens serait pire que rien.
/// </summary>
public sealed class EntretienOptions
{
    public string? Fichier { get; set; }
}

/// <summary>Une tâche d'entretien telle que le fichier la décrit, avant validation.</summary>
public sealed class ItemDePack
{
    /// <summary>Clé stable, unique dans tout le fichier (« maison.gouttieres-automne »).</summary>
    public string Cle { get; set; } = string.Empty;
    public string Titre { get; set; } = string.Empty;
    public string? Description { get; set; }
    /// <summary>Au format du moteur (mode + paramètres + fenêtre), jamais ponctuelle.</summary>
    public RecurrenceDto? Recurrence { get; set; }
    /// <summary>Fixe (défaut, non assignée), Alternance ou MoinsLAFait.</summary>
    public string? Strategie { get; set; }
}

/// <summary>La forme brute du fichier : le programme de la maison et un pack par catégorie.</summary>
public sealed class FichierDePacks
{
    public List<ItemDePack> Maison { get; set; } = [];
    public Dictionary<string, List<ItemDePack>> ParCategorie { get; set; } = [];
}

/// <summary>
/// Les packs d'entretien retenus après validation (vault : D-2026-09-28 Packs
/// D'entretien En Fichier De Données). Immuable, lu une fois au démarrage : c'est de la
/// donnée d'édition, pas de l'état.
/// </summary>
public sealed class PacksEntretien
{
    public static readonly PacksEntretien Vide = new([], new Dictionary<CategorieEquipement, IReadOnlyList<ItemDePack>>());

    private readonly Dictionary<string, ItemDePack> _parCle;

    public PacksEntretien(
        IReadOnlyList<ItemDePack> maison,
        IReadOnlyDictionary<CategorieEquipement, IReadOnlyList<ItemDePack>> parCategorie)
    {
        Maison = maison;
        ParCategorie = parCategorie;
        _parCle = maison.Concat(parCategorie.Values.SelectMany(items => items))
            .ToDictionary(item => item.Cle, StringComparer.Ordinal);
    }

    public IReadOnlyList<ItemDePack> Maison { get; }
    public IReadOnlyDictionary<CategorieEquipement, IReadOnlyList<ItemDePack>> ParCategorie { get; }

    public bool EstVide => _parCle.Count == 0;
    public int Nombre => _parCle.Count;

    public IReadOnlyList<ItemDePack> Pour(CategorieEquipement categorie) =>
        ParCategorie.TryGetValue(categorie, out var items) ? items : [];

    public ItemDePack? Trouver(string cle) =>
        _parCle.TryGetValue(cle.Trim(), out var item) ? item : null;
}

/// <summary>
/// La lecture du fichier de packs, une fois au démarrage, sur le patron de la banque du
/// hasard (<c>LectureDeLaBanque</c>). Un item qui ne fait pas une récurrence valide
/// est écarté et journalisé, jamais fatal : une virgule de trop dans un pack ne doit
/// pas empêcher l'app de démarrer.
/// </summary>
public static class LecturePacks
{
    public const string NomDuFichierParDefaut = "Features/Entretien/packs-entretien.qc.json";

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static PacksEntretien Lire(string? cheminConfigure, string racineDeContenu, ILogger journal)
    {
        var configure = string.IsNullOrWhiteSpace(cheminConfigure) == false;
        var chemin = configure
            ? cheminConfigure!.Trim()
            : Path.Combine(racineDeContenu, NomDuFichierParDefaut);

        if (File.Exists(chemin) == false)
        {
            journal.LogWarning("Packs d'entretien introuvables ({Chemin}) : aucune proposition d'entretien.", chemin);
            return PacksEntretien.Vide;
        }

        try
        {
            var fichier = JsonSerializer.Deserialize<FichierDePacks>(File.ReadAllText(chemin), Options);
            if (fichier is null)
            {
                journal.LogWarning("Packs d'entretien vides ({Chemin}) : aucune proposition d'entretien.", chemin);
                return PacksEntretien.Vide;
            }
            var packs = Retenir(fichier, journal, DateOnly.FromDateTime(DateTime.Now));
            journal.LogInformation(
                "Packs d'entretien lus ({Chemin}) : {Maison} items pour la maison, {Categories} catégories, {Total} au total.",
                chemin, packs.Maison.Count, packs.ParCategorie.Count, packs.Nombre);
            return packs;
        }
        catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException)
        {
            journal.LogWarning(e, "Packs d'entretien illisibles ({Chemin}) : aucune proposition d'entretien.", chemin);
            return PacksEntretien.Vide;
        }
    }

    /// <summary>
    /// Garde ce qui tient debout : clé unique, titre borné comme celui d'une tâche,
    /// récurrence qui passe la conversion du moteur (donc jamais ponctuelle), stratégie
    /// connue, catégorie de la liste fermée et différente d'<c>Autre</c>.
    /// </summary>
    public static PacksEntretien Retenir(FichierDePacks fichier, ILogger journal, DateOnly aujourdhui)
    {
        var cles = new HashSet<string>(StringComparer.Ordinal);
        var maison = fichier.Maison.Where(item => Valide(item, "maison", cles, journal, aujourdhui)).ToList();
        var parCategorie = new Dictionary<CategorieEquipement, IReadOnlyList<ItemDePack>>();
        foreach (var (nom, items) in fichier.ParCategorie)
        {
            if (ParseurEnum.Lire(nom, out CategorieEquipement categorie) == false
                || categorie == CategorieEquipement.Autre)
            {
                journal.LogWarning("Pack d'entretien « {Pack} » ignoré : catégorie inconnue ou sans pack.", nom);
                continue;
            }
            var retenus = items.Where(item => Valide(item, nom, cles, journal, aujourdhui)).ToList();
            if (retenus.Count > 0)
            {
                parCategorie[categorie] = retenus;
            }
        }
        return new PacksEntretien(maison, parCategorie);
    }

    private static bool Valide(ItemDePack item, string pack, HashSet<string> cles, ILogger journal, DateOnly aujourdhui)
    {
        var raison = Probleme(item, cles, aujourdhui);
        if (raison is null)
        {
            item.Cle = item.Cle.Trim();
            item.Titre = item.Titre.Trim();
            item.Description = string.IsNullOrWhiteSpace(item.Description) ? null : item.Description.Trim();
            return true;
        }
        journal.LogWarning("Item d'entretien « {Cle} » (pack {Pack}) ignoré : {Raison}", item.Cle, pack, raison);
        return false;
    }

    private static string? Probleme(ItemDePack item, HashSet<string> cles, DateOnly aujourdhui)
    {
        if (string.IsNullOrWhiteSpace(item.Cle))
        {
            return "clé manquante.";
        }
        if (cles.Add(item.Cle.Trim()) == false)
        {
            return "clé en double.";
        }
        if (string.IsNullOrWhiteSpace(item.Titre) || item.Titre.Trim().Length > 200)
        {
            return "titre manquant ou plus long que 200 caractères.";
        }
        if (item.Recurrence is null)
        {
            return "récurrence manquante.";
        }
        var (spec, erreur) = OperationsTaches.ConvertirRecurrence(item.Recurrence, aujourdhui);
        if (erreur is not null)
        {
            return erreur;
        }
        if (spec.Mode == ModeRecurrence.Ponctuelle)
        {
            return "un pack ne propose que des tâches récurrentes.";
        }
        var (_, erreurStrategie) = OperationsTaches.ConvertirStrategie(item.Strategie);
        return erreurStrategie;
    }
}
