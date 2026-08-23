namespace HouseOs.Api.Domaine;

/// <summary>
/// Journal de complétion — table d'historique séparée, jamais une simple date mutée
/// (voir la décision « Moteur De Récurrence Trois Modes » dans le vault).
/// </summary>
public class EntreeJournal
{
    public Guid Id { get; set; }
    public Guid TacheId { get; set; }
    public Guid OccurrenceId { get; set; }
    public Guid UtilisateurId { get; set; }
    public Utilisateur? Utilisateur { get; set; }
    public DateTimeOffset CompleteeLe { get; set; }
    public string? Notes { get; set; }
    public decimal? Cout { get; set; }
}
