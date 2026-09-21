using HouseOs.Api.Domaine.Meteo;
using HouseOs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HouseOs.Api.Features.Meteo;

/// <summary>
/// Les lectures et l'écriture des normales climatiques. Séparées du
/// <see cref="NormalesIngestionService"/> pour que la règle « quand faut-il
/// recalculer » et le remplacement en base se testent sans réseau.
/// </summary>
public static class OperationsNormales
{
    /// <summary>Les normales du lieu configuré, ou <c>null</c> si elles décrivent
    /// un autre endroit ou n'ont jamais été calculées.</summary>
    public static Task<NormalesClimatiques?> LireAsync(
        HouseOsDbContext db, string coordonnees, CancellationToken ct = default) =>
        db.NormalesClimatiques
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Coordonnees == coordonnees, ct);

    /// <summary>
    /// Faut-il retirer l'archive ? Oui si rien n'a jamais été calculé, si ce qui l'a
    /// été décrit <b>un autre lieu</b> (le `.env` a changé : c'est le cas du
    /// déménagement), ou si le calcul a pris de l'âge.
    /// </summary>
    public static async Task<bool> FautIlRecalculerAsync(
        HouseOsDbContext db, string coordonnees, DateTimeOffset maintenant, int ageMaxJours,
        CancellationToken ct = default)
    {
        var normales = await LireAsync(db, coordonnees, ct);
        if (normales is null)
        {
            return true;
        }
        return (maintenant - normales.CalculeesLe).TotalDays >= ageMaxJours;
    }

    /// <summary>
    /// Part de la fenêtre demandée qu'un tirage doit couvrir pour valoir la peine.
    /// </summary>
    private const double PartMinimaleDeLArchive = 0.9;

    /// <summary>
    /// Pourquoi ce tirage ne doit <b>pas</b> remplacer ce qu'on a déjà — ou <c>null</c>
    /// s'il le peut. Pur, donc testable sans réseau ni base.
    ///
    /// <para>Le danger n'est pas la panne, qui se voit : c'est le 200 maigre. Une
    /// réponse tronquée, une fenêtre rabotée par l'API, un
    /// <c>Meteo:NormalesAnnees</c> baissé par erreur, et dix ans d'archive seraient
    /// remplacés par trois mois — avec un horodatage tout neuf qui empêcherait de
    /// réessayer avant trois cent soixante jours. La famille « le climat » et « il a
    /// fait X° l'an dernier » se tairaient une année entière, sans une ligne d'erreur.
    /// `docs/configuration.md` promet le contraire : les normales connues restent
    /// servies. Trouvé en revue de code, étape 5.</para>
    /// </summary>
    public static string? PourquoiRefuser(
        int joursObtenus, int joursDemandes, NormalesClimatiques nouvelles, NormalesClimatiques? anciennes)
    {
        if (joursObtenus < joursDemandes * PartMinimaleDeLArchive)
        {
            return $"l'archive n'a rendu que {joursObtenus} journées sur {joursDemandes} demandées";
        }
        if (anciennes is { } connues && nouvelles.SaisonsCompletes < connues.SaisonsCompletes)
        {
            return $"le tirage ne donne que {nouvelles.SaisonsCompletes} saisons complètes "
                   + $"contre {connues.SaisonsCompletes} en base";
        }
        return null;
    }

    /// <summary>
    /// Remplace l'archive et les normales, en une transaction. Tout ce qui portait une
    /// <b>autre</b> clé s'en va : une seule localisation est configurée, et des
    /// journées de l'ancienne adresse ne serviraient qu'à mentir au journal.
    /// </summary>
    public static async Task RemplacerAsync(
        HouseOsDbContext db, IReadOnlyList<JourDeClimat> jours, NormalesClimatiques normales,
        CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.JoursDeClimat.ExecuteDeleteAsync(ct);
        await db.NormalesClimatiques.ExecuteDeleteAsync(ct);
        db.JoursDeClimat.AddRange(jours);
        db.NormalesClimatiques.Add(normales);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    /// <summary>La journée d'archive d'une date donnée, si l'archive la couvre.</summary>
    public static Task<JourDeClimat?> LireLeJourAsync(
        HouseOsDbContext db, string coordonnees, DateOnly date, CancellationToken ct = default) =>
        db.JoursDeClimat
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Coordonnees == coordonnees && j.Date == date, ct);
}
