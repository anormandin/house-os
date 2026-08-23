namespace HouseOs.Api.Domaine;

public enum StatutOccurrence
{
    EnAttente,
    Completee,
}

public class Occurrence
{
    public Guid Id { get; set; }
    public Guid TacheId { get; set; }
    public Tache? Tache { get; set; }
    public DateOnly? Echeance { get; set; }
    public StatutOccurrence Statut { get; set; } = StatutOccurrence.EnAttente;
    public Guid? CompleteeParId { get; set; }
    public Utilisateur? CompleteePar { get; set; }
    public DateTimeOffset? CompleteeLe { get; set; }

    public EntreeJournal Completer(Guid utilisateurId, DateTimeOffset maintenant, string? notes = null)
    {
        if (Statut == StatutOccurrence.Completee)
        {
            throw new InvalidOperationException("Occurrence déjà complétée.");
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
            Notes = notes,
        };
    }
}
