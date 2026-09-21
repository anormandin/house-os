namespace HouseOs.Api.Features.FondsDeTiroir;

/// <summary>Un événement d'un flux externe, réduit à ce que la famille « la ville » a
/// besoin d'en savoir. Le titre vient du dehors : il vit dans le <b>texte long</b> du
/// fait, jamais dans son étiquette (vault : Fonds De Tiroir).</summary>
public sealed record EvenementDeLaVille(DateOnly Date, string Titre, TimeOnly? Heure = null);

/// <summary>
/// Un flux de la ville et l'âge de ce qu'il porte. La fraîcheur est une propriété du
/// <b>flux</b>, pas de l'événement : c'est le flux qui cesse d'être alimenté.
/// </summary>
/// <param name="JoursDepuisReception">Jours écoulés depuis le dernier remplacement —
/// un téléchargement ICS réussi ou une poussée reçue. <c>null</c> : rien n'est jamais
/// entré, ce qui est aussi périmé que trop vieux.</param>
public sealed record FluxDeLaVille(int? JoursDepuisReception, IReadOnlyList<EvenementDeLaVille> Evenements)
{
    /// <summary>Un flux qu'on n'alimente plus ne dit plus rien de vrai.</summary>
    public bool Perime(int fraicheurMaxJours) =>
        JoursDepuisReception is not { } jours || jours > fraicheurMaxJours;
}

/// <summary>
/// Ce que la ville a à dire aujourd'hui : les collectes (un ICS, régénéré une fois l'an
/// hors du dépôt) et les événements municipaux (un flux poussé par un programme
/// extérieur) — vault : D-2026-09-20 Sources Municipales Séparées Par Solidité.
///
/// <para>Aucune horloge et aucune base ici : ce qui arrive est déjà arrêté, y compris
/// l'âge des flux, converti en jours par le lecteur.</para>
/// </summary>
/// <param name="FenetreJours">Le nombre de jours que les listes couvrent — c'est la
/// fenêtre d'ingestion des flux externes. La rareté d'un fait de la ville s'en déduit :
/// on ne peut compter la fréquence d'un événement que sur ce qu'on voit de lui.</param>
public sealed record EtatDeLaVille(
    IReadOnlyList<FluxDeLaVille> Collectes,
    IReadOnlyList<FluxDeLaVille> Municipaux,
    int FenetreJours);
