using System.Diagnostics;
using System.Globalization;
using HouseOs.Api.Domaine.Humeur;
using HouseOs.Api.Features.Editorial;
using HouseOs.Api.Features.FondsDeTiroir;
using HouseOs.Api.Features.Humeur;
using HouseOs.Api.Features.Meteo;
using HouseOs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HouseOs.Api.Features.Affichage;

/// <summary>Ce qu'un tirage demandé à la main a produit — la matière, puis l'image.</summary>
/// <param name="Source">Llm quand le modèle a écrit la phrase, Gabarit quand la banque
/// a pris le relais (pas de clé API, ou réponse inutilisable). C'est le seul moyen de
/// savoir si l'appel a vraiment eu lieu.</param>
/// <param name="EditionSource">Même chose pour l'édition du journal : Llm quand
/// l'éditorialiste (Opus) l'a écrite, Gabarit sinon.</param>
/// <param name="DifferentDuDernier">Faux quand le bitmap est identique à celui que
/// l'appareil a déjà : le firmware compare les noms de fichier et ne repeint pas pour
/// rien. Régénérer la phrase ne garantit donc pas un mur différent — une journée
/// identique peut se dire pareil.</param>
public record TirageDuMurDto(
    DateOnly Date,
    string Moment,
    string Titre,
    string SousTitre,
    string Source,
    bool PhraseReecrite,
    string Surtitre,
    string Manchette,
    string Chapeau,
    string EditionSource,
    int Rubriques,
    string? Appareil,
    int Largeur,
    int Hauteur,
    string Fichier,
    bool DifferentDuDernier,
    int Octets,
    int DureeMs);

/// <summary>
/// Régénérer le journal du mur à la demande : réécrire la phrase du créneau (appel
/// LLM compris), puis tirer l'image <b>par le chemin de l'appareil</b> — même capture,
/// même seuillage 1-bit, même taille — pour que ce qu'on vérifie soit exactement ce
/// que le mur recevra.
///
/// <para><b>Ce que ça ne fait pas : réveiller l'appareil.</b> Le protocole TRMNL est
/// en tirage, pas en poussée ; le reTerminal dort et redemande son écran à la cadence
/// configurée. La phrase neuve part donc au prochain réveil, pas à la seconde. C'est
/// une limite du protocole, pas un oubli (vault : Affichage E-ink).</para>
///
/// <para><c>horloge</c> date le tirage (le pied du journal, le surtitre d'édition) ;
/// <c>maintenant</c> est l'heure vraie, dont l'éditorialiste a besoin pour savoir s'il
/// écrit la journée courante ou un essai daté d'un autre jour.</para>
/// </summary>
public static class TirageDuMur
{
    /// <summary>
    /// Le créneau demandé. Vide : celui que l'heure courante commande, c'est-à-dire
    /// celui que le service de fond aurait produit.
    /// </summary>
    public static bool LireMoment(string? demande, DateTime maintenant, HumeurOptions humeur,
        out DateOnly date, out MomentJournee moment)
    {
        date = DateOnly.FromDateTime(maintenant);
        var heure = TimeOnly.FromDateTime(maintenant);

        switch ((demande ?? "").Trim().ToLowerInvariant())
        {
            case "matin":
                moment = MomentJournee.Matin;
                return true;
            case "soir":
                moment = MomentJournee.Soir;
                return true;
            case "":
            case "maintenant":
                // La même règle que le service de fond : avant l'heure du matin, on est
                // encore sur le soir de la veille.
                if (heure >= humeur.HeureSoir)
                {
                    moment = MomentJournee.Soir;
                    return true;
                }
                if (heure >= humeur.HeureMatin)
                {
                    moment = MomentJournee.Matin;
                    return true;
                }
                date = date.AddDays(-1);
                moment = MomentJournee.Soir;
                return true;
            default:
                moment = MomentJournee.Matin;
                return false;
        }
    }

    public const string Erreur = "Moment inconnu : « matin », « soir », ou vide pour le créneau courant.";

    /// <summary>
    /// Une autre journée que celle de l'horloge (YYYY-MM-DD) : l'édition et la phrase de
    /// ce jour-là sont écrites et gardées. C'est l'outil des essais — relire sept
    /// éditions à la suite sans attendre sept jours — pas celui du mur, qui vit à
    /// l'heure vraie.
    /// </summary>
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

    public const string ErreurDate = "Date illisible : YYYY-MM-DD, ou vide pour aujourd'hui.";

    public static async Task<TirageDuMurDto> RegenererAsync(
        HouseOsDbContext db,
        HumeurOptions humeur,
        IRedacteurEdition redacteur,
        MeteoOptions? meteo,
        BanqueDuHasard? banque,
        string? lieu,
        IRenduEcran rendu,
        CacheImages cache,
        ILogger journal,
        DateOnly date,
        MomentJournee moment,
        DateTime horloge,
        DateTime maintenant,
        CancellationToken ct)
    {
        // 1. La matière : la phrase du créneau puis l'édition du jour, réécrites même
        //    si elles existent déjà — c'est toute la différence avec le service de fond.
        //    La phrase d'abord : le gabarit de l'édition la prend en repli, et sans clé
        //    API c'est elle qui fait la manchette.
        var (phrase, reecrite) = await GenerationHumeur.GenererAsync(
            db, humeur, journal, date, moment, remplacer: true, ct);
        var (edition, _) = await GenerationEdition.GenererAsync(
            db, redacteur, meteo, banque, lieu, journal, date, maintenant, remplacer: true, ct);

        // 2. L'image, par le chemin de l'appareil. La taille est celle de l'appareil
        //    enrôlé quand il y en a un : tirer à une autre taille ne prouverait rien du
        //    mur, et c'est au mur qu'on s'intéresse.
        //    Le tri se fait en mémoire : un foyer a un écran mural, deux au grand
        //    maximum, et Sqlite (les tests) ne sait pas trier un DateTimeOffset. Rien
        //    à gagner à le demander à la base.
        var appareils = await db.AppareilsAffichage.AsNoTracking().ToListAsync(ct);
        var appareil = appareils.OrderBy(a => a.EnroleLe).FirstOrDefault();

        var demande = new DemandeCapture(
            appareil?.Largeur ?? AffichageOptions.LargeurParDefaut,
            appareil?.Hauteur ?? AffichageOptions.HauteurParDefaut,
            Telemetrie.PileEnPourcent(appareil?.TensionPile),
            Moment: horloge);

        var chrono = Stopwatch.StartNew();
        var bitmap = Seuillage.EnUnBit(await rendu.CapturerAsync(demande, ct));
        chrono.Stop();
        var fichier = Seuillage.Signature(bitmap);

        // Le bitmap frais remplace ce que le cache gardait pour cet appareil : s'il se
        // réveille dans la seconde, il télécharge celui-ci. On ne touche PAS à
        // DernierFichier — il veut dire « le dernier bitmap servi à l'appareil », et un
        // tirage à la main n'est jamais allé jusqu'au mur.
        if (appareil is not null)
        {
            cache.Deposer(appareil.Id, fichier, bitmap);
        }

        journal.LogInformation(
            "Tirage du mur — {Date} ({Moment}), édition via {EditionSource}, phrase via {Source}, image {Fichier} en {Duree} ms.",
            date, moment, edition.Source, phrase.Source, fichier, chrono.ElapsedMilliseconds);

        return new TirageDuMurDto(
            date,
            moment.ToString(),
            phrase.Titre,
            phrase.SousTitre,
            phrase.Source.ToString(),
            reecrite,
            edition.Surtitre,
            edition.Manchette,
            edition.Chapeau,
            edition.Source.ToString(),
            edition.Rubriques.Count,
            appareil?.Identifiant,
            demande.Largeur,
            demande.Hauteur,
            fichier,
            fichier != appareil?.DernierFichier,
            bitmap.Length,
            (int)chrono.ElapsedMilliseconds);
    }
}
