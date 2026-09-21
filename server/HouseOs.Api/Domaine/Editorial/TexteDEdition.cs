namespace HouseOs.Api.Domaine.Editorial;

/// <summary>
/// Ce que l'éditorialiste écrit — et seulement ça. Tout le reste de l'édition (rang,
/// clés publiées, plancher, source) est calculé.
/// </summary>
public sealed record TexteDEdition(
    string Surtitre,
    string Manchette,
    string Chapeau,
    IReadOnlyList<string> Paragraphes,
    IReadOnlyList<RubriqueEdition> Rubriques);
