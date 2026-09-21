using HouseOs.Api.Domaine.Editorial;
using HouseOs.Api.Features.FondsDeTiroir;
using HouseOs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HouseOs.Api.Features.Editorial;

/// <summary>
/// Les sept dernières éditions, lues une fois pour deux usages : la pénalité de
/// fraîcheur du fonds de tiroir (par clé publiée) et le prompt anti-radotage (par
/// texte). Une seule table, une seule lecture
/// (D-2026-09-20 Une Édition Par Jour Matérialisée).
/// </summary>
public sealed record MemoireDesEditions(HistoriqueDeParution Fraicheur, IReadOnlyList<EditionPrecedente> Precedentes)
{
    public static readonly MemoireDesEditions Vide = new(HistoriqueDeParution.Vide, []);

    /// <summary>
    /// Les éditions de la semaine <b>avant</b> la date : l'édition du jour lui-même n'est
    /// jamais sa propre mémoire, sinon une réédition pénaliserait à zéro les faits qu'elle
    /// vient de choisir.
    /// </summary>
    public static async Task<MemoireDesEditions> LireAsync(HouseOsDbContext db, DateOnly date, CancellationToken ct = default)
    {
        var depuis = date.AddDays(-HistoriqueDeParution.JoursDeMemoire);
        var editions = await db.Editions
            .AsNoTracking()
            .Where(e => e.Date >= depuis && e.Date < date)
            .OrderByDescending(e => e.Date)
            .ToListAsync(ct);
        if (editions.Count == 0)
        {
            return Vide;
        }

        var dernieres = new Dictionary<string, DateOnly>(StringComparer.Ordinal);
        foreach (var edition in editions)
        {
            foreach (var cle in edition.ClesPubliees)
            {
                if (dernieres.TryGetValue(cle, out var deja) == false || deja < edition.Date)
                {
                    dernieres[cle] = edition.Date;
                }
            }
        }
        return new MemoireDesEditions(
            new HistoriqueDeParution(dernieres),
            [.. editions.Select(e => new EditionPrecedente(e.Date, e.Surtitre, e.Manchette, e.Chapeau))]);
    }
}
