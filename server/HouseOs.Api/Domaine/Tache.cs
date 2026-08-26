namespace HouseOs.Api.Domaine;

public enum ModeRecurrence
{
    Ponctuelle,
    Fixe,
    Intervalle,
}

public class Tache
{
    public Guid Id { get; set; }
    public required string Titre { get; set; }
    public string? Description { get; set; }

    /// <summary>Assigné par défaut (stratégie Fixe) et repli des autres stratégies.</summary>
    public Guid? AssigneAId { get; set; }
    public Utilisateur? AssigneA { get; set; }

    public Guid? ZoneId { get; set; }
    public Zone? Zone { get; set; }
    public Guid? EquipementId { get; set; }
    public Equipement? Equipement { get; set; }

    /// <summary>Documents de référence (rapport, manuel, contrat…) — plusieurs par tâche.</summary>
    public List<Document> Documents { get; } = [];

    public SpecRecurrence Recurrence { get; set; } = SpecRecurrence.Ponctuelle();
    public StrategieAssignation Strategie { get; set; } = StrategieAssignation.Fixe;

    public Guid CreeParId { get; set; }
    public DateTimeOffset CreeLe { get; set; }
    public List<Occurrence> Occurrences { get; } = [];

    public static Tache CreerPonctuelle(
        string titre,
        string? description,
        DateOnly? echeance,
        Guid? assigneAId,
        Guid creeParId,
        DateTimeOffset maintenant)
    {
        var tache = new Tache
        {
            Id = Guid.NewGuid(),
            Titre = titre,
            Description = description,
            AssigneAId = assigneAId,
            Recurrence = SpecRecurrence.Ponctuelle(),
            CreeParId = creeParId,
            CreeLe = maintenant,
        };
        tache.Occurrences.Add(new Occurrence
        {
            Id = Guid.NewGuid(),
            TacheId = tache.Id,
            Echeance = echeance,
            AssigneAId = assigneAId,
        });
        return tache;
    }

    /// <summary>
    /// Crée une tâche récurrente et matérialise sa première occurrence
    /// (échéance fournie, sinon calculée par le moteur).
    /// </summary>
    public static Tache CreerRecurrente(
        string titre,
        string? description,
        SpecRecurrence recurrence,
        StrategieAssignation strategie,
        Guid? assigneAId,
        DateOnly? premiereEcheance,
        Guid creeParId,
        DateTimeOffset maintenant,
        DateOnly aujourdhui)
    {
        if (recurrence.Mode == ModeRecurrence.Ponctuelle)
        {
            throw new InvalidOperationException("Utiliser CreerPonctuelle pour une tâche ponctuelle.");
        }

        var tache = new Tache
        {
            Id = Guid.NewGuid(),
            Titre = titre,
            Description = description,
            AssigneAId = assigneAId,
            Recurrence = recurrence,
            Strategie = strategie,
            CreeParId = creeParId,
            CreeLe = maintenant,
        };
        tache.Occurrences.Add(new Occurrence
        {
            Id = Guid.NewGuid(),
            TacheId = tache.Id,
            Echeance = premiereEcheance ?? MoteurRecurrence.PremiereEcheance(recurrence, aujourdhui),
            AssigneAId = assigneAId,
        });
        return tache;
    }

    /// <summary>
    /// Génère l'occurrence suivante après une complétion (null pour une ponctuelle).
    /// L'assigné est choisi en amont via <see cref="Assignation"/> (le journal est en base).
    /// </summary>
    public Occurrence? GenererProchaineOccurrence(
        DateOnly dateCompletion,
        DateOnly? echeanceCompletee,
        Guid? assigneAId)
    {
        var prochaine = MoteurRecurrence.ProchaineEcheance(Recurrence, dateCompletion, echeanceCompletee);
        if (prochaine is null)
        {
            return null;
        }
        return new Occurrence
        {
            Id = Guid.NewGuid(),
            TacheId = Id,
            Echeance = prochaine,
            AssigneAId = assigneAId,
        };
    }
}
