namespace HouseOs.Api.Domaine.Editorial;

/// <summary>Une tâche due, telle que l'éditorialiste la voit.</summary>
public sealed record TachePourEdition(string Titre, int JoursDeRetard, bool EcheanceFerme, string? Assigne);

/// <summary>Un fait du fonds de tiroir, avec la marque de ceux que l'édition publie.</summary>
public sealed record FaitPourEdition(
    string Cle, string Famille, string Etiquette, string Valeur, string Texte, bool Publie);

public sealed record CompteProcheDEdition(string Titre, int Dodos);

public sealed record MeteoDEdition(string Description, double TempMin, double TempMax);

/// <summary>Ce qu'une édition précédente a dit — la mémoire anti-radotage.</summary>
public sealed record EditionPrecedente(DateOnly Date, string Surtitre, string Manchette, string Chapeau);

/// <summary>
/// Tout ce que l'éditorialiste reçoit, et rien d'autre : des faits déjà calculés, en
/// français. Aucun chiffre ni aucun titre ne peut venir d'ailleurs — c'est la garde
/// « aucun fait inventé » de D-2026-09-20 Édition Écrite Par Opus.
/// </summary>
public sealed record MatiereDEdition(
    DateOnly Date,
    RangEdition Rang,
    PlancherDuJour? Plancher,
    IReadOnlyList<TachePourEdition> TachesDues,
    CompteProcheDEdition? ProchainCompte,
    MeteoDEdition? Meteo,
    IReadOnlyList<FaitPourEdition> Faits,
    IReadOnlyList<EditionPrecedente> Precedentes,
    string? Lieu);
