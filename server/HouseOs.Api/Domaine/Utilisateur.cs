namespace HouseOs.Api.Domaine;

public class Utilisateur
{
    public Guid Id { get; set; }
    public required string NomUtilisateur { get; set; }
    public required string NomAffichage { get; set; }
    public required string MotDePasseHash { get; set; }

    /// <summary>Jeton secret du flux iCal personnel (généré à l'amorçage).</summary>
    public string? JetonIcal { get; set; }

    /// <summary>Adresse de courriel, seedée depuis la config (COMPTE_n_COURRIEL) ; nulle =
    /// ne reçoit pas la lettre du matin (D-2026-09-21 Adresse De Courriel Sur
    /// L'Utilisateur).</summary>
    public string? Courriel { get; set; }
}
