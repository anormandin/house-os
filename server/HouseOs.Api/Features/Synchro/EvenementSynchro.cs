namespace HouseOs.Api.Features.Synchro;

/// <summary>
/// Le seul message poussé aux clients connectés. Deux tiers partagent ce contrat :
/// le tier grossier (IntercepteurSynchro) n'envoie que <see cref="Module"/> et sert à
/// invalider les caches ; le tier fin ajoute <see cref="Genre"/> et l'acteur, ce qui
/// autorise un toast. Rien n'est persisté : un client déconnecté ne rate rien, il
/// refait ses requêtes en se reconnectant.
/// </summary>
/// <param name="Module">Domaine touché — la clé de la table d'invalidation côté web.</param>
/// <param name="Genre">Nul pour le tier grossier ; sinon l'événement précis à annoncer.</param>
/// <param name="Source">« web » ou « mcp » : distingue un geste humain d'une écriture de l'agent.</param>
/// <param name="Nombre">Cardinalité d'une opération en lot — le client fusionne sur ce compte.</param>
public record EvenementSynchro(
    string Module,
    string? Genre = null,
    string? Source = null,
    Guid? ActeurId = null,
    string? ActeurNom = null,
    string? Libelle = null,
    int Nombre = 1)
{
    public const string SourceWeb = "web";
    public const string SourceMcp = "mcp";

    public const string GenreOccurrenceCompletee = "occurrence.completee";
    public const string GenreOccurrenceAnnulee = "occurrence.annulee";
    public const string GenreTachesCreees = "taches.creees";
}
