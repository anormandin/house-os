namespace HouseOs.Api.Domaine;

public enum TypeFluxExterne
{
    Collecte,
    Ecole,
    Municipal,
    Autre,
}

/// <summary>D'où viennent les événements d'un flux (vault : D-2026-09-20 Flux Externe
/// Poussé). <see cref="Ics"/> : l'app télécharge une URL toutes les 6 h.
/// <see cref="Poussee"/> : un programme extérieur les remplace par l'API — l'app ne va
/// rien chercher, et le rafraîchissement ICS doit laisser ces flux tranquilles.</summary>
public enum SourceFluxExterne
{
    Ics,
    Poussee,
}

/// <summary>Un calendrier externe géré dans l'app : soit un abonnement ICS que l'app
/// télécharge (collectes, calendrier scolaire), soit un flux <b>poussé</b> qu'un
/// programme extérieur remplit par l'API (vault : D-2026-09-20 Flux Externe Poussé).</summary>
public class FluxExterne
{
    public Guid Id { get; set; }
    public required string Nom { get; set; }

    /// <summary>L'URL du calendrier ICS — <b>nulle</b> pour un flux poussé, qui n'en a
    /// pas : c'est le monde extérieur qui vient à lui.</summary>
    public string? Url { get; set; }

    public TypeFluxExterne Type { get; set; } = TypeFluxExterne.Autre;
    public SourceFluxExterne Source { get; set; } = SourceFluxExterne.Ics;
    public bool Actif { get; set; } = true;

    /// <summary>La dernière fois que les événements de ce flux ont été remplacés — un
    /// téléchargement réussi, ou une poussée reçue. C'est l'horodatage que l'UI montre
    /// et celui sur lequel le fonds de tiroir juge qu'un flux est périmé.</summary>
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
