namespace HouseOs.Api.Domaine;

/// <summary>
/// Le cœur du système : calcul pur (aucune I/O) des prochaines échéances.
/// La prochaine occurrence est matérialisée à la complétion, jamais calculée à la
/// lecture (D-2026-08-23 Moteur De Récurrence Trois Modes).
/// </summary>
public static class MoteurRecurrence
{
    // Garde-fou d'itération : > 2 ans couvre toute fenêtre saisonnière + année bissextile.
    private const int JoursMax = 1500;

    /// <summary>
    /// Échéance de l'occurrence suivante après une complétion.
    /// Ponctuelle → null. Intervalle → complétion + N jours (clampé à la fenêtre).
    /// Fixe → prochaine date planifiée strictement après max(complétion, échéance courante).
    /// </summary>
    public static DateOnly? ProchaineEcheance(
        SpecRecurrence spec,
        DateOnly dateCompletion,
        DateOnly? echeanceCourante = null)
    {
        switch (spec.Mode)
        {
            case ModeRecurrence.Ponctuelle:
                return null;

            case ModeRecurrence.Intervalle:
            {
                var candidate = dateCompletion.AddDays(spec.IntervalleJours
                    ?? throw new InvalidOperationException("IntervalleJours requis en mode intervalle."));
                return spec.DansFenetre(candidate) ? candidate : DebutProchaineFenetre(spec, candidate);
            }

            case ModeRecurrence.Fixe:
            {
                var reference = echeanceCourante is { } e && e > dateCompletion ? e : dateCompletion;
                return ProchainePlanifiee(spec, reference.AddDays(1));
            }

            default:
                throw new InvalidOperationException($"Mode inconnu : {spec.Mode}.");
        }
    }

    /// <summary>
    /// Première date planifiée ≥ aPartirDe pour une tâche fixe, en respectant la
    /// fenêtre saisonnière. Sert à la création (première échéance) et au rollover.
    /// </summary>
    public static DateOnly ProchainePlanifiee(SpecRecurrence spec, DateOnly aPartirDe)
    {
        if (spec.Mode != ModeRecurrence.Fixe)
        {
            throw new InvalidOperationException("ProchainePlanifiee ne s'applique qu'au mode fixe.");
        }

        var date = aPartirDe;
        for (var i = 0; i < JoursMax; i++, date = date.AddDays(1))
        {
            if (JourPlanifie(spec, date) && spec.DansFenetre(date))
            {
                return date;
            }
        }
        throw new InvalidOperationException("Aucune date planifiée trouvée (spec invalide?).");
    }

    /// <summary>
    /// Première échéance d'une tâche récurrente à sa création (aucune complétion encore).
    /// Fixe → prochaine date planifiée ≥ aujourd'hui. Intervalle → aujourd'hui + N
    /// (clampé à la fenêtre).
    /// </summary>
    public static DateOnly PremiereEcheance(SpecRecurrence spec, DateOnly aujourdhui) => spec.Mode switch
    {
        ModeRecurrence.Fixe => ProchainePlanifiee(spec, aujourdhui),
        ModeRecurrence.Intervalle => ProchaineEcheance(spec, aujourdhui)!.Value,
        _ => throw new InvalidOperationException("Une ponctuelle n'a pas d'échéance calculée."),
    };

    /// <summary>Début (première date) de la prochaine fenêtre saisonnière ≥ aPartirDe.</summary>
    public static DateOnly DebutProchaineFenetre(SpecRecurrence spec, DateOnly aPartirDe)
    {
        if (spec.AFenetre == false)
        {
            return aPartirDe;
        }
        var date = aPartirDe;
        for (var i = 0; i < JoursMax; i++, date = date.AddDays(1))
        {
            if (spec.DansFenetre(date))
            {
                return date;
            }
        }
        throw new InvalidOperationException("Fenêtre saisonnière introuvable (spec invalide?).");
    }

    private static bool JourPlanifie(SpecRecurrence spec, DateOnly date) => spec.FixeType switch
    {
        TypeFixe.JoursSemaine => spec.JourSemainePlanifie(date.DayOfWeek),
        // Jour du mois clampé : « le 31 » tombe le 28/29 février, le 30 avril, etc.
        TypeFixe.JourDuMois => date.Day == Math.Min(
            spec.JourDuMois ?? throw new InvalidOperationException("JourDuMois requis."),
            DateTime.DaysInMonth(date.Year, date.Month)),
        TypeFixe.Annuelle => date.Month == spec.MoisAnnuel && date.Day == Math.Min(
            spec.JourAnnuel ?? throw new InvalidOperationException("JourAnnuel requis."),
            DateTime.DaysInMonth(date.Year, date.Month)),
        _ => throw new InvalidOperationException("FixeType requis en mode fixe."),
    };
}
