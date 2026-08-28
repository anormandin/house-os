namespace HouseOs.Api.Domaine;

public enum StatutOccurrence
{
    EnAttente,
    Completee,
    Passee,
}

public class Occurrence
{
    public Guid Id { get; set; }
    public Guid TacheId { get; set; }
    public Tache? Tache { get; set; }
    public DateOnly? Echeance { get; set; }
    public Guid? AssigneAId { get; set; }
    public Utilisateur? AssigneA { get; set; }
    public StatutOccurrence Statut { get; set; } = StatutOccurrence.EnAttente;
    public Guid? CompleteeParId { get; set; }
    public Utilisateur? CompleteePar { get; set; }
    public DateTimeOffset? CompleteeLe { get; set; }
    public DateTimeOffset? PasseeLe { get; set; }

    public EntreeJournal Completer(Guid utilisateurId, DateTimeOffset maintenant, string? notes = null)
    {
        if (Statut != StatutOccurrence.EnAttente)
        {
            throw new InvalidOperationException("Occurrence déjà traitée.");
        }

        Statut = StatutOccurrence.Completee;
        CompleteeParId = utilisateurId;
        CompleteeLe = maintenant;

        return new EntreeJournal
        {
            Id = Guid.NewGuid(),
            TacheId = TacheId,
            OccurrenceId = Id,
            UtilisateurId = utilisateurId,
            CompleteeLe = maintenant,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
        };
    }

    /// <summary>
    /// Défait une complétion : l'occurrence redevient en attente avec son échéance
    /// d'origine — elle redevient donc éligible au rollover, ce qui est voulu.
    /// La suppression de l'entrée de journal appartient à l'appelant.
    /// </summary>
    public void AnnulerCompletion()
    {
        if (Statut != StatutOccurrence.Completee)
        {
            throw new InvalidOperationException("Occurrence non complétée.");
        }

        Statut = StatutOccurrence.EnAttente;
        CompleteeParId = null;
        CompleteeLe = null;
    }

    /// <summary>Saute l'occurrence sans la marquer faite : trace datée, pas de journal.</summary>
    public void Passer(DateTimeOffset maintenant)
    {
        if (Statut != StatutOccurrence.EnAttente)
        {
            throw new InvalidOperationException("Occurrence déjà traitée.");
        }

        Statut = StatutOccurrence.Passee;
        PasseeLe = maintenant;
    }

    /// <summary>Glisse l'échéance de cette occurrence sans toucher la définition de la tâche.</summary>
    public void Reporter(DateOnly nouvelleEcheance)
    {
        if (Statut != StatutOccurrence.EnAttente)
        {
            throw new InvalidOperationException("Occurrence déjà traitée.");
        }

        Echeance = nouvelleEcheance;
    }
}
