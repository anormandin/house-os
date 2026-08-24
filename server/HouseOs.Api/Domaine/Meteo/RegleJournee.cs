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

    /// <summary>Les heures dans [maintenant − avant ; maintenant + après].</summary>
    protected static List<PrevisionHoraire> Fenetre(ApercuMeteo apercu, int heuresAvant, int heuresApres) =>
        apercu.Heures
            .Where(h => h.Heure >= apercu.Maintenant.AddHours(-heuresAvant)
                && h.Heure <= apercu.Maintenant.AddHours(heuresApres))
            .OrderBy(h => h.Heure)
            .ToList();

    protected VerdictRegle SansDonnees() =>
        new(Nom, EtatVerdict.Defavorable, "Pas encore de prévisions.");
}
