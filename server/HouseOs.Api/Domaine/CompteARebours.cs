namespace HouseOs.Api.Domaine;

public enum IconeCompteARebours
{
    Camion,
    Sapin,
    Avion,
    Valise,
    Gateau,
    Cadeau,
    Coeur,
    Soleil,
}

/// <summary>Un compte à rebours du foyer (déménagement, Noël, un voyage…). Pas d'assignation.</summary>
public class CompteARebours
{
    public Guid Id { get; set; }
    public required string Titre { get; set; }
    public DateOnly DateCible { get; set; }
    public IconeCompteARebours Icone { get; set; } = IconeCompteARebours.Soleil;
}
