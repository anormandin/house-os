namespace HouseOs.Api.Features.FondsDeTiroir;

/// <summary>Une tâche et ce que le journal de complétion en a retenu.</summary>
/// <param name="Nombre">Combien de fois elle a été cochée, depuis toujours.</param>
/// <param name="Premiere">La date de la première complétion — le « depuis le… ».</param>
public sealed record SeancesDeTache(string Titre, int Nombre, DateOnly Premiere);

/// <param name="ProchainEntretien">L'échéance ouverte la plus proche parmi les tâches
/// rattachées à cet équipement, s'il y en a une.</param>
public sealed record EquipementDeLaMaison(string Nom, DateOnly? DateAchat, DateOnly? ProchainEntretien);

/// <param name="Taches">Combien de tâches vivent dans cette zone. Une zone sans tâche
/// n'est pas négligée, elle est vide — et le journal n'a rien à en dire.</param>
/// <param name="DerniereCompletion">La dernière fois qu'on y a coché quelque chose.</param>
public sealed record ZoneDeLaMaison(string Nom, int Taches, DateOnly? DerniereCompletion);

/// <param name="Quoi">Ce qui a un anniversaire aujourd'hui : un équipement, un
/// événement du foyer.</param>
/// <param name="Depuis">La date d'origine — le jour et le mois sont ceux d'aujourd'hui.</param>
public sealed record AnniversaireDeLaMaison(string Quoi, DateOnly Depuis);

/// <summary>
/// Le journal de complétion, les équipements et les zones, réduits à ce que la famille
/// « la maison » a besoin de savoir (vault : Fonds De Tiroir). Aucune lecture d'horloge
/// ici : tout est déjà arrêté à la date du jour, pour que deux rendus de la même
/// journée donnent le même journal.
///
/// <para>C'est la famille la plus personnelle et la plus muette : une installation
/// neuve n'a pas de série, pas de coût et pas d'an dernier. Chaque fait le dit à sa
/// façon — en ne sortant pas.</para>
/// </summary>
/// <param name="JoursActifs">Les jours où au moins une chose a été cochée, depuis
/// toujours, triés. C'est la matière de la série et du record.</param>
/// <param name="CoutDeLAnnee">La somme des coûts consignés depuis le 1er janvier.</param>
/// <param name="InterventionsDeLAnnee">Combien d'entrées de journal portaient un coût.</param>
/// <param name="FaitLAnDernier">Ce qui a été coché un an jour pour jour avant
/// aujourd'hui — vide jusqu'à la deuxième année, et c'est voulu.</param>
public sealed record EtatDeLaMaison(
    IReadOnlyList<DateOnly> JoursActifs,
    IReadOnlyList<SeancesDeTache> Seances,
    IReadOnlyList<EquipementDeLaMaison> Equipements,
    IReadOnlyList<ZoneDeLaMaison> Zones,
    decimal CoutDeLAnnee,
    int InterventionsDeLAnnee,
    IReadOnlyList<AnniversaireDeLaMaison> Anniversaires,
    IReadOnlyList<string> FaitLAnDernier)
{
    /// <summary>Une maison qui n'a encore rien à raconter.</summary>
    public static readonly EtatDeLaMaison Vide = new([], [], [], [], 0, 0, [], []);
}
