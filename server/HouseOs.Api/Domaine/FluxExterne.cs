namespace HouseOs.Api.Domaine;

public enum TypeFluxExterne
{
    Collecte,
    Ecole,
    Autre,
}

/// <summary>Un abonnement à un calendrier ICS externe (collectes Recollect,
/// calendrier scolaire…), géré dans l'app.</summary>
public class FluxExterne
{
    public Guid Id { get; set; }
    public required string Nom { get; set; }
    public required string Url { get; set; }
    public TypeFluxExterne Type { get; set; } = TypeFluxExterne.Autre;
    public bool Actif { get; set; } = true;
    public DateTimeOffset? DernierRafraichissementLe { get; set; }
    public string? DerniereErreur { get; set; }

    public List<EvenementExterne> Evenements { get; set; } = [];
}

/// <summary>Un événement normalisé d'un flux externe — un fait affiché, jamais
/// une tâche. Récurrences déjà expansées à l'ingestion.</summary>
public class EvenementExterne
{
    public int Id { get; set; }
    public Guid FluxExterneId { get; set; }
    public required string Uid { get; set; }
    public required string Titre { get; set; }
    public DateOnly Date { get; set; }
    /// <summary>Heure locale de début ; null = toute la journée.</summary>
    public TimeOnly? Heure { get; set; }
}
