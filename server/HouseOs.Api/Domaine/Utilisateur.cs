namespace HouseOs.Api.Domaine;

public class Utilisateur
{
    public Guid Id { get; set; }
    public required string NomUtilisateur { get; set; }
    public required string NomAffichage { get; set; }
    public required string MotDePasseHash { get; set; }

    /// <summary>Jeton secret du flux iCal personnel (généré à l'amorçage).</summary>
    public string? JetonIcal { get; set; }
}
