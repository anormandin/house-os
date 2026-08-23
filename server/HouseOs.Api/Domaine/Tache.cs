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
    public Guid? AssigneAId { get; set; }
    public Utilisateur? AssigneA { get; set; }
    public ModeRecurrence Mode { get; set; } = ModeRecurrence.Ponctuelle;
    public bool Rollover { get; set; }
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
            Mode = ModeRecurrence.Ponctuelle,
            CreeParId = creeParId,
            CreeLe = maintenant,
        };
        tache.Occurrences.Add(new Occurrence
        {
            Id = Guid.NewGuid(),
            TacheId = tache.Id,
            Echeance = echeance,
        });
        return tache;
    }

    /// <summary>
    /// Génère l'occurrence suivante après une complétion. Une tâche ponctuelle n'en a
    /// jamais ; les modes fixe/intervalle arrivent avec le moteur de récurrence (V1).
    /// </summary>
    public Occurrence? GenererProchaineOccurrence(DateOnly dateCompletion) => Mode switch
    {
        ModeRecurrence.Ponctuelle => null,
        _ => throw new NotSupportedException($"Mode {Mode} : moteur de récurrence prévu en V1."),
    };
}
