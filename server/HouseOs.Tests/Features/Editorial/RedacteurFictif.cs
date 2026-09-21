using HouseOs.Api.Domaine.Editorial;
using HouseOs.Api.Features.Editorial;

namespace HouseOs.Tests.Features.Editorial;

/// <summary>
/// L'éditorialiste sous test : un compteur d'appels, la dernière matière reçue, et le
/// texte qu'il rend (null = il se tait, comme sans clé). C'est lui qui prouve qu'un
/// second rendu dans la même journée n'appelle personne.
/// </summary>
public sealed class RedacteurFictif : IRedacteurEdition
{
    public TexteDEdition? Texte { get; set; }
    public int Appels { get; private set; }
    public MatiereDEdition? DerniereMatiere { get; private set; }
    public string Modele => "modele-fictif";
    /// <summary>Vrai par défaut : le fictif a « une clé ». Faux pour jouer l'installation sans clé.</summary>
    public bool PeutEcrire { get; set; } = true;

    public Task<TexteDEdition?> RedigerAsync(MatiereDEdition matiere, CancellationToken ct)
    {
        Appels++;
        DerniereMatiere = matiere;
        return Task.FromResult(Texte);
    }
}
