namespace HouseOs.Api.Domaine.Lettre;

/// <summary>Qui a écrit : le modèle, ou la note de quatre lignes composée en gabarit.</summary>
public enum SourceLettre
{
    Llm,
    Gabarit,
}

/// <summary>
/// La lettre du matin d'une journée, matérialisée : une ligne par date, marquée envoyée
/// après l'envoi — le garde-fou contre le double envoi survit au redémarrage, et la
/// matière se rejoue dans l'atelier (D-2026-09-21 Lettre Matérialisée Et Rattrapée Le
/// Jour Même, D-2026-09-21 Lettre Écrite À Part Sur La Même Matière).
/// </summary>
public class LettreDuMatin
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }

    public required string Sujet { get; set; }
    public List<string> Paragraphes { get; set; } = [];

    public SourceLettre Source { get; set; }

    /// <summary>Le modèle qui a écrit ; nul sur un gabarit.</summary>
    public string? Modele { get; set; }

    /// <summary>Le JSON exact reçu par le modèle, posé chaque fois qu'on lui a demandé
    /// d'écrire — même quand il s'est tu.</summary>
    public string? Matiere { get; set; }

    public DateTimeOffset ComposeeLe { get; set; }

    /// <summary>Nul tant que rien n'est parti. Un envoi d'essai ne le pose pas.</summary>
    public DateTimeOffset? EnvoyeeLe { get; set; }

    /// <summary>Les adresses auxquelles la lettre du jour est partie.</summary>
    public List<string> Destinataires { get; set; } = [];

    public bool Envoyee => EnvoyeeLe is not null;
}
