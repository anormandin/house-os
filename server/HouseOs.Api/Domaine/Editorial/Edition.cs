namespace HouseOs.Api.Domaine.Editorial;

/// <summary>
/// Le rang du jour, calqué sur la table de bascule du journal
/// (vault : D-2026-09-20 Une Seule Mise En Page À Rangs). Le même mot que
/// `web/src/lib/ecran-vues.ts` : c'est le rang pour lequel la prose a été écrite.
/// </summary>
public enum RangEdition
{
    Chronique,
    Manchette,
    Resserre,
    Court,
    Sommaire,
    Evenement,
}

public enum SourceEdition
{
    Gabarit,
    Llm,
}

/// <summary>Les trois cas du plancher non négociable (vault : Journal De La Maison).</summary>
public enum RaisonDePlancher
{
    /// <summary>Un compte à rebours tombé à zéro.</summary>
    Compte,
    /// <summary>Une tâche en retard de plus de trois jours.</summary>
    Retard,
    /// <summary>Une échéance ferme, cochée à la main sur la tâche
    /// (D-2026-09-20 Échéance Ferme Explicite Sur La Tâche).</summary>
    Ferme,
}

/// <summary>Une rubrique du sommaire des journées chargées : son nom et les titres
/// de tâches qu'elle regroupe — des titres réels, jamais inventés.</summary>
public sealed record RubriqueEdition(string Nom, List<string> Taches);

/// <summary>
/// L'édition du jour, matérialisée une fois par jour au créneau du matin
/// (vault : D-2026-09-20 Une Édition Par Jour Matérialisée). Elle porte ce qui est
/// <b>figé pour la journée</b> : le rang, la sélection et l'ordre des widgets publiés,
/// le surtitre, la manchette, le chapeau, le corps, les rubriques. La liste des
/// occurrences, les cochées, la météo, l'heure et la pile restent vivantes à chaque
/// rendu et ne sont pas ici.
///
/// <para>Les sept dernières lignes de cette table sont la mémoire du fonds de tiroir :
/// <see cref="ClesPubliees"/> alimente la pénalité de fraîcheur, et les textes
/// alimentent le prompt anti-radotage.</para>
/// </summary>
public class Edition
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public RangEdition Rang { get; set; }

    /// <summary>La ligne éditoriale au-dessus de la manchette ; vide plutôt que de
    /// répéter la dateline.</summary>
    public string Surtitre { get; set; } = "";
    public required string Manchette { get; set; }
    public string Chapeau { get; set; } = "";
    /// <summary>Les deux paragraphes de corps ; vides quand le gabarit a écrit.</summary>
    public List<string> Paragraphes { get; set; } = [];
    public List<RubriqueEdition> Rubriques { get; set; } = [];
    /// <summary>Les clés des faits que l'édition a choisi de publier, dans l'ordre.</summary>
    public List<string> ClesPubliees { get; set; } = [];

    public RaisonDePlancher? PlancherRaison { get; set; }
    public string? PlancherTitre { get; set; }

    public SourceEdition Source { get; set; }
    /// <summary>Le modèle qui a écrit, quand c'est un LLM — pour relire les éditions
    /// d'un modèle à l'autre.</summary>
    public string? Modele { get; set; }
    public DateTimeOffset GenereLe { get; set; }

    /// <summary>
    /// Vrai quand le chemin de requête a dû matérialiser un gabarit — édition manquante,
    /// ou plancher qui a changé après coup — et que l'éditorialiste doit repasser. Le
    /// service de fond le consomme ; s'il échoue, le gabarit reste et le drapeau tombe,
    /// pour ne pas rappeler le modèle à chaque réveil.
    /// </summary>
    public bool ReeditionEnAttente { get; set; }

    public PlancherDuJour? Plancher =>
        PlancherRaison is { } raison ? new PlancherDuJour(raison, PlancherTitre ?? "") : null;
}
