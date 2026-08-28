namespace HouseOs.Api.Domaine.Meteo;

public enum EtatVerdict
{
    Bon,
    Passable,
    Defavorable,
}

public record VerdictRegle(string Regle, EtatVerdict Etat, string Raison);

/// <summary>Photo météo normalisée sur laquelle les règles raisonnent : l'instant
/// d'évaluation, les heures disponibles (passé récent inclus) et les jours.</summary>
public record ApercuMeteo(
    DateTime Maintenant,
    IReadOnlyList<PrevisionHoraire> Heures,
    IReadOnlyList<PrevisionQuotidienne> Jours);

/// <summary>Une règle « bonne journée pour… » : du C# pur et testé, évalué à la
/// lecture sur le modèle normalisé (D-2026-08-24 Tables Météo Normalisées).</summary>
public abstract class RegleJournee
{
    public abstract string Nom { get; }
    public abstract VerdictRegle Evaluer(ApercuMeteo apercu);

    public static readonly IReadOnlyList<RegleJournee> Toutes =
        [new RegleTonte(), new RegleAeration(), new RegleJourneeDehors()];

    /// <summary>Le début de l'heure en cours — l'ancre des fenêtres : à 13 h 30,
    /// la ligne horaire de 13 h décrit le moment présent et doit compter.</summary>
    protected static DateTime HeureCourante(ApercuMeteo apercu) =>
        apercu.Maintenant.Date.AddHours(apercu.Maintenant.Hour);

    /// <summary>Les heures dans [heure courante − avant ; heure courante + après].
    /// Ancré sur le début de l'heure en cours : sinon, une fenêtre « à venir »
    /// excluait l'heure en train de se passer — il pleuvait dehors et la règle
    /// regardait à partir de la prochaine heure pleine (issue #58).</summary>
    protected static List<PrevisionHoraire> Fenetre(ApercuMeteo apercu, int heuresAvant, int heuresApres)
    {
        var ancre = HeureCourante(apercu);
        return apercu.Heures
            .Where(h => h.Heure >= ancre.AddHours(-heuresAvant)
                && h.Heure <= ancre.AddHours(heuresApres))
            .OrderBy(h => h.Heure)
            .ToList();
    }

    protected VerdictRegle SansDonnees() =>
        new(Nom, EtatVerdict.Defavorable, "Pas encore de prévisions.");
}
