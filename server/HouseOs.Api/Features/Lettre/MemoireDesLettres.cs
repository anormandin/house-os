using HouseOs.Api.Domaine.Lettre;
using HouseOs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HouseOs.Api.Features.Lettre;

/// <summary>Les sept dernières lettres parties avant la date : le sujet et la première
/// ligne, pour que la maison ne radote pas. La lettre du jour n'est jamais sa propre
/// mémoire.</summary>
public static class MemoireDesLettres
{
    public const int JoursDeMemoire = 7;

    public static async Task<IReadOnlyList<LettrePrecedente>> LireAsync(
        HouseOsDbContext db, DateOnly date, CancellationToken ct = default)
    {
        var depuis = date.AddDays(-JoursDeMemoire);
        var lettres = await db.Lettres
            .AsNoTracking()
            .Where(l => l.Date >= depuis && l.Date < date && l.EnvoyeeLe != null)
            .OrderByDescending(l => l.Date)
            .ToListAsync(ct);
        return [.. lettres.Select(l => new LettrePrecedente(
            l.Date, l.Sujet, new TexteDeLettre(l.Sujet, l.Paragraphes).PremiereLigne(RedactionLettre.LongueurPremiereLigne)))];
    }
}
