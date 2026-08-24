namespace HouseOs.Api.Domaine.Humeur;

public enum SourcePhrase
{
    Gabarit,
    Llm,
}

/// <summary>La phrase du héros d'Aujourd'hui, matérialisée matin et soir.
/// L'UI lit cette table — jamais d'appel LLM dans le chemin de requête.</summary>
public class PhraseDuJour
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public MomentJournee Moment { get; set; }
    public required string Titre { get; set; }
    public required string SousTitre { get; set; }
    public SourcePhrase Source { get; set; }
    public DateTimeOffset GenereLe { get; set; }
}
