using HouseOs.Api.Domaine.Ephemerides;

namespace HouseOs.Api.Features.FondsDeTiroir;

/// <summary>D'où le foyer regarde le ciel, et sous quelle horloge.</summary>
public sealed record PointDObservation(Lieu Lieu, TimeZoneInfo Fuseau);

/// <summary>
/// Ce que le fonds de tiroir a besoin de savoir de la journée pour juger de la
/// pertinence. Rien de plus : pas d'horloge, pas de base, pas de sortie. Ce qui en
/// dépend est figé pour la journée entière — un fait dont le texte changerait d'un
/// rendu à l'autre ferait mentir l'édition matérialisée de l'étape 7.
///
/// <para><b>Une famille sans source se tait.</b> Chaque famille a ici son matériau, et
/// il est facultatif : sans coordonnées, le ciel ne sort pas ; sans journal de
/// complétion, la maison ne sort pas. C'est la même règle que pour un fait isolé dont
/// la source manque — une absence normale, jamais une erreur.</para>
/// </summary>
/// <param name="TachesDehors">Vrai si au moins une tâche ouverte du jour appartient à
/// une zone extérieure. C'est le seul signal « la journée est physique » que le modèle
/// porte vraiment : il n'y a pas de catégorie sur la tâche, et il n'y en aura pas
/// (vault : D-2026-09-20 Regroupement Sans Catégorie De Tâche).</param>
public sealed record ContexteDuJour(
    DateOnly Date,
    PointDObservation? Ciel,
    bool TachesDehors,
    EtatDeLaMaison? Maison = null,
    EtatDuCalendrier? Calendrier = null);
