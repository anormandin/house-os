namespace HouseOs.Api.Domaine.Humeur;

public enum MomentJournee
{
    Matin,
    Soir,
}

/// <summary>Catégorie d'état de la maison, dérivée des faits — pilote le choix de
/// gabarit et sert de contexte au polissage LLM.</summary>
public enum CleEtat
{
    ToutFait,
    RienAuProgramme,
    Retard,
    JourneeChargee,
    Calme,
}

public record CompteProche(string Titre, int Dodos);

public record MeteoDuJour(
    double TemperatureMin,
    double TemperatureMax,
    int ProbabilitePrecipitationPct,
    IReadOnlyList<string> VerdictsFavorables);

/// <summary>
/// Couche 1 du titre d'humeur : l'état structuré de la maison, calculé en C# —
/// tout ce qui est numérique ou factuel vient d'ici, jamais du LLM. La même
/// structure nourrira la vue e-ink.
/// </summary>
public record EtatMaison(
    DateOnly Date,
    MomentJournee Moment,
    int Ouvertes,
    int EnRetard,
    int FaitesAujourdhui,
    IReadOnlyList<CompteProche> ComptesProches,
    MeteoDuJour? Meteo)
{
    private const int SeuilJourneeChargee = 5;

    public CleEtat Cle
    {
        get
        {
            if (Ouvertes == 0 && FaitesAujourdhui > 0)
            {
                return CleEtat.ToutFait;
            }
            if (Ouvertes == 0)
            {
                return CleEtat.RienAuProgramme;
            }
            if (EnRetard > 0)
            {
                return CleEtat.Retard;
            }
            if (Ouvertes >= SeuilJourneeChargee)
            {
                return CleEtat.JourneeChargee;
            }
            return CleEtat.Calme;
        }
    }
}
