using HouseOs.Api.Domaine.Lettre;
using HouseOs.Api.Features.Lettre;

namespace HouseOs.Tests.Features.Lettre;

/// <summary>Le rédacteur à compteur : écrit ce qu'on lui a donné, ou se tait, et compte
/// chaque appel — la preuve qu'un passage n'appelle personne.</summary>
public sealed class RedacteurLettreFictif : IRedacteurLettre
{
    public TexteDeLettre? Texte { get; set; }
    public bool PeutEcrire { get; set; } = true;
    public int Appels { get; private set; }
    public MatiereDeLettre? DerniereMatiere { get; private set; }
    public string Modele => "modele-fictif";

    public Task<TexteDeLettre?> RedigerAsync(MatiereDeLettre matiere, CancellationToken ct)
    {
        Appels++;
        DerniereMatiere = matiere;
        return Task.FromResult(PeutEcrire ? Texte : null);
    }
}
