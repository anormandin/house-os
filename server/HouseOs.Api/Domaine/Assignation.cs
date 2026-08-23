namespace HouseOs.Api.Domaine;

public enum StrategieAssignation
{
    Fixe,
    Alternance,
    MoinsLAFait,
}

/// <summary>
/// Choix pur de l'assigné d'une nouvelle occurrence (équité à deux adultes —
/// stratégies validées par la recherche Grocy/Donetick).
/// </summary>
public static class Assignation
{
    /// <param name="completionsRecentes">Complétions de CETTE tâche par utilisateur
    /// (90 derniers jours), utilisateurs sans complétion inclus à 0.</param>
    public static Guid? ChoisirAssigne(
        StrategieAssignation strategie,
        Guid? assigneParDefautId,
        Guid? dernierCompleteurId,
        IReadOnlyList<Guid> utilisateurs,
        IReadOnlyDictionary<Guid, int> completionsRecentes)
    {
        switch (strategie)
        {
            case StrategieAssignation.Fixe:
                return assigneParDefautId;

            case StrategieAssignation.Alternance:
                return Alterner(dernierCompleteurId, assigneParDefautId, utilisateurs);

            case StrategieAssignation.MoinsLAFait:
            {
                if (utilisateurs.Count == 0)
                {
                    return null;
                }
                var minimum = utilisateurs.Min(u => completionsRecentes.GetValueOrDefault(u));
                var candidats = utilisateurs.Where(u => completionsRecentes.GetValueOrDefault(u) == minimum).ToList();
                return candidats.Count == 1
                    ? candidats[0]
                    : Alterner(dernierCompleteurId, assigneParDefautId, candidats);
            }

            default:
                throw new InvalidOperationException($"Stratégie inconnue : {strategie}.");
        }
    }

    private static Guid? Alterner(Guid? dernierCompleteurId, Guid? assigneParDefautId, IReadOnlyList<Guid> utilisateurs)
    {
        if (utilisateurs.Count == 0)
        {
            return null;
        }
        if (dernierCompleteurId is { } dernier)
        {
            var autre = utilisateurs.Where(u => u != dernier).ToList();
            if (autre.Count > 0)
            {
                return autre[0];
            }
        }
        return assigneParDefautId ?? utilisateurs[0];
    }
}
