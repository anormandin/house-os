namespace HouseOs.Api.Domaine;

/// <summary>
/// Ce qu'est un équipement, dans une liste fermée (vault : D-2026-09-28 Catégorie
/// D'équipement En Liste Fermée). C'est la clé des packs d'entretien : une thermopompe
/// ne demande pas les gestes d'un chauffe-eau. <c>Autre</c> est la catégorie du reste,
/// sans pack ; null sur l'entité veut dire « pas encore classé ».
/// </summary>
public enum CategorieEquipement
{
    Chauffage,
    EauChaude,
    Plomberie,
    Electricite,
    Toiture,
    Exterieur,
    PetitsMoteurs,
    Electromenager,
    Vehicule,
    Autre,
}

/// <summary>
/// Un actif de la maison (fournaise, chauffe-eau, tondeuse…). Les specs libres
/// (taille de filtre, code de peinture) vivent en JSONB.
/// </summary>
public class Equipement
{
    public Guid Id { get; set; }
    public required string Nom { get; set; }
    public Guid? ZoneId { get; set; }
    public Zone? Zone { get; set; }
    public CategorieEquipement? Categorie { get; set; }
    public string? Marque { get; set; }
    public string? Modele { get; set; }
    public string? NumeroSerie { get; set; }
    public DateOnly? DateAchat { get; set; }
    public DateOnly? FinGarantie { get; set; }
    public string? Notes { get; set; }
    public Dictionary<string, string> Specs { get; set; } = [];
    public DateTimeOffset CreeLe { get; set; }
}
