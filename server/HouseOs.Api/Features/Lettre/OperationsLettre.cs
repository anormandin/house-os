using System.Globalization;
using HouseOs.Api.Domaine.Lettre;
using HouseOs.Api.Features.FondsDeTiroir;
using HouseOs.Api.Features.Meteo;
using HouseOs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HouseOs.Api.Features.Lettre;

/// <summary>La lettre telle que l'app et le MCP la voient : le texte, l'état de l'envoi,
/// et le courriel rendu tel qu'il part.</summary>
public record LettreDto(
    DateOnly Date,
    string Sujet,
    List<string> Paragraphes,
    string Source,
    string? Modele,
    DateTimeOffset ComposeeLe,
    DateTimeOffset? EnvoyeeLe,
    List<string> Destinataires,
    /// <summary>Faux pour un aperçu composé à la volée, jamais écrit.</summary>
    bool Ecrite,
    bool EnvoiActif,
    List<string> DestinatairesPrevus,
    string Texte,
    string Html);

/// <summary>
/// Les trois appelants — REST, MCP, la page — passent par ici : lire (ou composer un
/// aperçu sans écrire), réécrire (appel LLM compris) et envoyer, envoyer un essai à
/// une seule personne sans marquer la journée.
/// </summary>
public static class OperationsLettre
{
    public const string ErreurDate = "Date illisible : YYYY-MM-DD, ou vide pour aujourd'hui.";

    public static bool LireDate(string? demande, out DateOnly? date)
    {
        date = null;
        if (string.IsNullOrWhiteSpace(demande))
        {
            return true;
        }
        if (DateOnly.TryParseExact(demande.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var lue))
        {
            date = lue;
            return true;
        }
        return false;
    }

    public sealed record Contexte(
        HouseOsDbContext Db,
        IRedacteurLettre Redacteur,
        IEnvoyeurDeCourriel Envoyeur,
        LettreOptions Options,
        MeteoOptions? Meteo,
        BanqueDuHasard? Banque,
        string? Lieu,
        ILogger Journal);

    /// <summary>La lettre écrite pour la date ; sinon un aperçu en note, composé sans
    /// modèle et sans rien écrire — un GET ne fait pas attendre Opus.</summary>
    public static async Task<LettreDto> LireAsync(Contexte c, DateOnly date, DateTime maintenant, CancellationToken ct)
    {
        var lettre = await c.Db.Lettres.AsNoTracking().SingleOrDefaultAsync(l => l.Date == date, ct);
        if (lettre is not null)
        {
            return await Dto(c, lettre, ecrite: true, ct);
        }
        var apercu = await GenerationLettre.ApercuAsync(c.Db, null, c.Meteo, c.Banque, c.Lieu, date, maintenant, ct);
        return await Dto(c, apercu, ecrite: false, ct);
    }

    /// <summary>Réécrit la lettre de la date, appel LLM compris, même si elle existe —
    /// et l'envoie si demandé, même si elle est déjà partie : c'est un geste explicite.</summary>
    public static async Task<LettreDto> RegenererAsync(
        Contexte c, DateOnly date, bool envoyer, DateTime maintenant, CancellationToken ct)
    {
        var (lettre, _) = await GenerationLettre.ComposerAsync(
            c.Db, c.Redacteur, c.Meteo, c.Banque, c.Lieu, c.Journal, date, maintenant, remplacer: true, ct);
        if (envoyer)
        {
            await GenerationLettre.EnvoyerAsync(c.Db, c.Envoyeur, c.Options, lettre, c.Journal, maintenant, ct);
        }
        return await Dto(c, lettre, ecrite: true, ct);
    }

    /// <summary>Envoie la lettre de la date à une seule adresse, sans toucher à
    /// <c>EnvoyeeLe</c> : la lettre écrite si elle existe, sinon une composée par le
    /// modèle et jamais écrite. Null si l'envoi est désactivé.</summary>
    public static async Task<string?> EssaiAsync(
        Contexte c, DateOnly date, Destinataire destinataire, DateTime maintenant, CancellationToken ct)
    {
        if (c.Envoyeur.Actif == false)
        {
            return null;
        }
        var lettre = await c.Db.Lettres.AsNoTracking().SingleOrDefaultAsync(l => l.Date == date, ct)
            ?? await GenerationLettre.ApercuAsync(c.Db, c.Redacteur, c.Meteo, c.Banque, c.Lieu, date, maintenant, ct);
        await c.Envoyeur.EnvoyerAsync(RenduCourriel.Composer(lettre, [destinataire], c.Options), ct);
        c.Journal.LogInformation("Lettre du {Date} : essai envoyé à {Adresse}.", date, destinataire.Adresse);
        return destinataire.Adresse;
    }

    private static async Task<LettreDto> Dto(Contexte c, LettreDuMatin lettre, bool ecrite, CancellationToken ct)
    {
        var prevus = await GenerationLettre.DestinatairesAsync(c.Db, ct);
        return new LettreDto(
            lettre.Date, lettre.Sujet, lettre.Paragraphes, lettre.Source.ToString(), lettre.Modele,
            lettre.ComposeeLe, lettre.EnvoyeeLe, lettre.Destinataires, ecrite, c.Envoyeur.Actif,
            [.. prevus.Select(d => d.Adresse)],
            RenduCourriel.Texte(lettre, c.Options), RenduCourriel.Html(lettre, c.Options));
    }
}
