namespace HouseOs.Api.Features.FondsDeTiroir;

/// <summary>Le compte à rebours le plus proche, celui qui tient le calendrier du foyer.</summary>
public sealed record CompteDuCalendrier(string Titre, DateOnly DateCible);

/// <summary>Une occurrence ouverte dont l'échéance est encore loin — le « ça s'en vient ».</summary>
public sealed record EcheanceProchaine(string Titre, DateOnly Echeance);

/// <summary>
/// Une fenêtre saisonnière du moteur de récurrence, exposée comme donnée lisible
/// plutôt que comme quatre nombres (<see cref="Domaine.SpecRecurrence.FenetreAutour"/>).
/// </summary>
public sealed record FenetreDeSaison(string Titre, DateOnly Ouverture, DateOnly Fermeture);

/// <param name="EstUneGarantie">Une garantie d'équipement, sinon l'échéance d'un
/// document (assurance, permis…). Les deux se disent, mais pas avec les mêmes mots.</param>
public sealed record ExpirationProchaine(string Quoi, DateOnly Date, bool EstUneGarantie);

/// <summary>
/// Les comptes à rebours, les occurrences à venir, les fenêtres saisonnières et les
/// papiers qui expirent, réduits à ce que la famille « le calendrier » a besoin de
/// savoir (vault : Fonds De Tiroir). Que des lectures de tables existantes.
/// </summary>
public sealed record EtatDuCalendrier(
    CompteDuCalendrier? ProchainCompte,
    IReadOnlyList<EcheanceProchaine> CaSEnVient,
    IReadOnlyList<FenetreDeSaison> Saisons,
    IReadOnlyList<ExpirationProchaine> Expirations)
{
    /// <summary>Un calendrier vide — rien de prévu, rien qui expire.</summary>
    public static readonly EtatDuCalendrier Vide = new(null, [], [], []);
}
