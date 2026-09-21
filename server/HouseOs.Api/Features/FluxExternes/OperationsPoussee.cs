using HouseOs.Api.Domaine;
using HouseOs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HouseOs.Api.Features.FluxExternes;

/// <summary>Un événement reçu du dehors, avant toute borne et toute fenêtre.</summary>
/// <param name="Uid">L'identifiant de la source, s'il en a un : deux poussées du même
/// événement portent alors le même. Absent, le titre en tient lieu — le remplacement
/// étant complet, rien n'en dépend vraiment.</param>
public sealed record EvenementPousse(string? Titre, DateOnly Date, TimeOnly? Heure = null, string? Uid = null);

/// <summary>Ce qu'une poussée a fait : ce qu'elle apportait, ce qui est resté, ce que
/// la fenêtre a écarté.</summary>
public sealed record ResultatPoussee(int Recus, int Retenus, int Ignores);

/// <summary>
/// La poussée d'un flux externe : un programme extérieur remplace les événements d'un
/// flux, en transaction, exactement comme le fait le téléchargement ICS
/// (vault : D-2026-09-20 Flux Externe Poussé).
///
/// <para>Séparée du endpoint pour que la règle se teste sans HTTP, et parce que l'outil
/// MCP fait le même geste que REST — parité MCP (vault : Serveur MCP).</para>
/// </summary>
public static class OperationsPoussee
{
    /// <summary>
    /// Au-delà, ce n'est pas le calendrier d'une ville : c'est un programme qui
    /// déraille, et la table du foyer n'a pas à encaisser sa boucle.
    /// </summary>
    public const int MaxEvenements = 500;

    /// <summary>
    /// Pourquoi cette poussée ne peut <b>pas</b> être reçue — ou <c>null</c> si elle le
    /// peut. Pure, donc testable sans base.
    /// </summary>
    public static string? PourquoiRefuser(FluxExterne flux, IReadOnlyList<EvenementPousse> evenements)
    {
        if (flux.Source != SourceFluxExterne.Poussee)
        {
            // Accepter ici viderait le flux à la prochaine passe de six heures : ce
            // n'est pas une poussée, c'est un abonnement qui se télécharge tout seul.
            return "Ce calendrier est un abonnement iCal : ses événements viennent de son URL, "
                   + "pas d'une poussée.";
        }
        if (evenements.Count > MaxEvenements)
        {
            return $"Trop d'événements d'un coup (maximum {MaxEvenements}).";
        }
        return null;
    }

    /// <summary>
    /// Remplace <b>tous</b> les événements du flux par ceux-ci, en transaction, et note
    /// la réception. Le contrat est celui du rafraîchissement ICS : ce qui n'est plus
    /// poussé disparaît, et la même poussée deux fois de suite laisse la même table.
    ///
    /// <para>La fenêtre est celle de l'ICS — d'hier à soixante jours — et ce qui en
    /// sort est <b>écarté</b> plutôt que refusé : une ville qui publie son calendrier
    /// de l'année entière ne doit pas voir sa poussée rejetée pour autant.</para>
    /// </summary>
    public static async Task<ResultatPoussee> RemplacerAsync(
        HouseOsDbContext db,
        FluxExterne flux,
        IReadOnlyList<EvenementPousse> evenements,
        DateOnly aujourdhui,
        DateTimeOffset maintenant,
        CancellationToken ct = default)
    {
        var debut = aujourdhui.AddDays(-1);
        var finExclue = aujourdhui.AddDays(FluxExternesRafraichissement.FenetreJours);
        var retenus = evenements
            .Where(e => e.Date >= debut && e.Date < finExclue)
            .Select(e => new EvenementExterne
            {
                FluxExterneId = flux.Id,
                Uid = BornesDuFlux.Uid(e.Uid ?? e.Titre, e.Date),
                Titre = BornesDuFlux.Titre(e.Titre),
                Date = e.Date,
                Heure = e.Heure,
            })
            .OrderBy(e => e.Date)
            .ThenBy(e => e.Heure)
            .ToList();

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.EvenementsExternes.Where(e => e.FluxExterneId == flux.Id).ExecuteDeleteAsync(ct);
        db.EvenementsExternes.AddRange(retenus);
        flux.DernierRafraichissementLe = maintenant;
        flux.DerniereErreur = null;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return new ResultatPoussee(evenements.Count, retenus.Count, evenements.Count - retenus.Count);
    }
}
