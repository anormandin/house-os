using HouseOs.Api.Domaine.Editorial;

namespace HouseOs.Api.Domaine.Lettre;

/// <summary>Une échéance des sept prochains jours, telle que la lettre la voit.</summary>
public sealed record EcheanceDevant(DateOnly Date, string Titre, string? Assigne, bool EcheanceFerme);

/// <summary>Une tâche cochée depuis la dernière lettre : le journal de complétion a
/// enfin un endroit où sortir.</summary>
public sealed record TacheFaite(DateOnly Date, string Titre, string? Par);

/// <summary>Ce qu'une lettre précédente a dit — le sujet et ce qui partait dans
/// l'aperçu : la mémoire anti-radotage.</summary>
public sealed record LettrePrecedente(DateOnly Date, string Sujet, string PremiereLigne);

/// <summary>
/// Tout ce que la lettre reçoit, et rien d'autre : la matière de l'édition, étendue de
/// ce que la lettre seule utilise — la semaine devant, ce qui a été fait, la série de
/// chaque tâche due, et les sept lettres précédentes
/// (D-2026-09-21 Lettre Écrite À Part Sur La Même Matière).
/// </summary>
public sealed record MatiereDeLettre(
    MatiereDEdition Edition,
    IReadOnlyList<EcheanceDevant> SemaineDevant,
    IReadOnlyList<TacheFaite> FaitesDepuisLaDerniere,
    IReadOnlyDictionary<string, int> Series,
    IReadOnlyList<LettrePrecedente> Precedentes)
{
    public DateOnly Date => Edition.Date;

    /// <summary>La série d'une tâche due : combien de fois de suite elle a été faite ce
    /// même jour de semaine. Zéro quand rien ne le dit.</summary>
    public int Serie(string titre) => Series.TryGetValue(titre, out var n) ? n : 0;
}

/// <summary>
/// La série : « la même que les dix derniers dimanches » ne se dit que si les dix
/// derniers dimanches l'ont vue cochée. On remonte de sept jours en sept jours tant
/// qu'une complétion tombe sur la date attendue ; le premier trou arrête le compte.
/// </summary>
public static class Serie
{
    public const int Plafond = 52;

    public static int Compter(DateOnly aujourdhui, IEnumerable<DateOnly> completions)
    {
        var jours = completions.ToHashSet();
        var n = 0;
        for (var d = aujourdhui.AddDays(-7); jours.Contains(d) && n < Plafond; d = d.AddDays(-7))
        {
            n++;
        }
        return n;
    }
}
