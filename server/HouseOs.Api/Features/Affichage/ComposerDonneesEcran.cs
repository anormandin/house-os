using HouseOs.Api.Domaine;
using HouseOs.Api.Domaine.Ephemerides;
using HouseOs.Api.Domaine.Humeur;
using HouseOs.Api.Features.FluxExternes;
using HouseOs.Api.Features.FondsDeTiroir;
using HouseOs.Api.Features.Humeur;
using HouseOs.Api.Features.Meteo;
using HouseOs.Api.Features.Taches;
using HouseOs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HouseOs.Api.Features.Affichage;

/// <summary>
/// Une ligne de la liste du jour, déjà réduite à ce que l'écran montre.
/// <paramref name="JoursDeRetard"/> est 0 quand la ligne n'est pas en retard : le
/// journal en a besoin en jours, pas en booléen, parce que son plancher se déclenche
/// à partir d'un retard de trois jours (vault : Journal De La Maison).
/// </summary>
public record LigneEcranDto(string Titre, string? Assigne, bool Faite, int JoursDeRetard);

public record PhraseEcranDto(string Titre, string SousTitre);

public record MeteoEcranDto(
    int CodeMeteo,
    double TemperatureC,
    double TempMin,
    double TempMax,
    int ProbabilitePrecipitation,
    List<VerdictDto> Verdicts);

public record CompteEcranDto(string Titre, DateOnly DateCible);

/// <summary>
/// Un fait du fonds de tiroir, tel que l'écran le reçoit. La <paramref name="Valeur"/>
/// est la forme courte et le <paramref name="Texte"/> la forme longue : c'est le
/// journal qui choisit celle qu'il a la place de montrer, jamais le fonds
/// (vault : D-2026-09-20 Fonds De Tiroir Séparé Du Journal).
/// </summary>
public record FaitEcranDto(string Cle, string Famille, string Etiquette, string Valeur, string Texte);

/// <summary>
/// Ce qu'il faut à la composition pour ouvrir le fonds de tiroir : d'où l'on regarde
/// le ciel, sous quel fuseau, et quelles zones sont dehors. Les coordonnées et le
/// fuseau viennent du `.env` (Meteo:Latitude/Longitude, Meteo:FuseauHoraire).
/// </summary>
public sealed record ReglagesDuCiel(Lieu Coordonnees, TimeZoneInfo Fuseau, IReadOnlySet<Guid> ZonesExterieures);

/// <summary>
/// Tout ce que la page /ecran affiche, en un seul document : la page ne compose rien,
/// elle met en forme. Les compteurs (ouvertes, en retard, faites) servent au repli
/// client de la phrase du jour quand le serveur n'en a pas.
/// </summary>
public record DonneesEcran(
    DateOnly Date,
    DateTimeOffset RenduLe,
    PhraseEcranDto? Phrase,
    List<LigneEcranDto> Lignes,
    int LignesEnPlus,
    int Ouvertes,
    int EnRetard,
    int Faites,
    MeteoEcranDto? Meteo,
    List<EvenementExterneDto> EvenementsDuJour,
    EvenementExterneDto? ProchaineCollecte,
    CompteEcranDto? ProchainCompte,
    string? Lieu,
    int? NumeroEdition,
    List<FaitEcranDto> Faits);

public static class ComposerDonneesEcran
{
    /// <summary>
    /// Au-delà, on élague : un écran mural ne se fait pas défiler. Mesuré en montant
    /// les maquettes du journal, pas estimé — à ce corps, trois colonnes tiennent
    /// ~9 items chacune, et la journée la plus chargée de toute la prod en compte 14.
    /// </summary>
    public const int MaxLignes = 27;

    /// <summary>Fenêtre de recherche de la prochaine collecte et du prochain compte à rebours.</summary>
    private const int FenetreJours = 60;

    /// <summary>Lit tout ce qu'il faut puis compose. Une seule lecture d'horloge.</summary>
    public static async Task<DonneesEcran> LireAsync(
        HouseOsDbContext db, DateTime maintenant, string? lieu = null, MeteoOptions? options = null)
    {
        var aujourdhui = DateOnly.FromDateTime(maintenant);
        // Bornes de la journée locale : le serveur vit en heure locale (TZ du
        // conteneur), et deux constructions séparées tiennent le jour du changement
        // d'heure (un AddDays sur l'offset ne le tiendrait pas). En UTC pour Npgsql,
        // qui refuse tout autre offset sur timestamptz.
        var debutJour = new DateTimeOffset(maintenant.Date).ToUniversalTime();
        var finJour = new DateTimeOffset(maintenant.Date.AddDays(1)).ToUniversalTime();

        var ouvertes = await OperationsTaches.ListerOccurrencesAsync(db, "aujourdhui", aujourdhui, null, null);
        var faites = await OperationsTaches.ListerOccurrencesAsync(db, "faites", aujourdhui, debutJour, finJour);
        var phrase = await HumeurEndpoints.PhraseCouranteAsync(db, aujourdhui);
        var previsions = await MeteoEndpoints.LireAsync(db, maintenant);
        var fin = aujourdhui.AddDays(FenetreJours);
        var evenements = await db.EvenementsExternes
            .Where(e => e.Date >= aujourdhui && e.Date < fin)
            .Join(db.FluxExternes, e => e.FluxExterneId, f => f.Id, (e, f) => new { e, f })
            .OrderBy(x => x.e.Date).ThenBy(x => x.e.Heure)
            .Select(x => new EvenementExterneDto(x.e.Titre, x.f.Type.ToString(), x.e.Date, x.e.Heure))
            .ToListAsync();
        var comptes = await db.ComptesARebours
            .Where(c => c.DateCible >= aujourdhui)
            .OrderBy(c => c.DateCible)
            .Take(1)
            .ToListAsync();
        // Le numéro d'édition compte les jours depuis la première chose que la maison
        // a consignée. Journal vide (une installation neuve) : pas de numéro plutôt
        // qu'un « N° 1 » qui vieillirait mal.
        var premiereEntree = await db.Journal
            .OrderBy(e => e.CompleteeLe)
            .Select(e => (DateTimeOffset?)e.CompleteeLe)
            .FirstOrDefaultAsync();
        // Les zones extérieures : le seul signal « la journée est physique » que le
        // modèle porte vraiment. Il n'y a pas de catégorie sur la tâche, et il n'y en
        // aura pas (vault : D-2026-09-20 Regroupement Sans Catégorie De Tâche).
        var zonesDehors = await db.Zones
            .Where(z => z.Type == TypeZone.Exterieur)
            .Select(z => z.Id)
            .ToListAsync();

        return Composer(
            maintenant, ouvertes, faites, phrase, previsions, evenements, comptes, lieu,
            premiereEntree is { } d ? DateOnly.FromDateTime(d.LocalDateTime) : null,
            options is null ? null : new ReglagesDuCiel(
                new Lieu(options.Latitude, options.Longitude), Fuseau(options), zonesDehors.ToHashSet()));
    }

    /// <summary>
    /// Le numéro de l'édition : une par jour depuis la première chose consignée dans
    /// le journal de complétion. Rien avant cette date (une reprise d'un vieux
    /// journal ne doit pas sortir un numéro négatif), rien sans journal.
    /// </summary>
    public static int? NumeroEdition(DateOnly aujourdhui, DateOnly? premiereParution)
    {
        if (premiereParution is not { } debut || debut > aujourdhui)
        {
            return null;
        }
        return aujourdhui.DayNumber - debut.DayNumber + 1;
    }

    /// <summary>
    /// Le fonds de tiroir du jour, déjà classé. L'historique de parution est
    /// <b>vide</b> à cette étape : il se branche aux éditions matérialisées à l'étape 7
    /// du plan (vault : D-2026-09-20 Une Édition Par Jour Matérialisée), et d'ici là
    /// aucun fait n'est pénalisé.
    /// </summary>
    private static List<FaitEcranDto> FondsDuJour(
        DateOnly aujourdhui, IReadOnlyList<OccurrenceDto> ouvertes, ReglagesDuCiel? ciel)
    {
        if (ciel is null)
        {
            return [];
        }
        var contexte = new ContexteDuJour(
            aujourdhui,
            ciel.Coordonnees,
            ciel.Fuseau,
            ouvertes.Any(o => o.ZoneId is { } zone && ciel.ZonesExterieures.Contains(zone)));

        return [.. Tiroir.Ouvrir(contexte, HistoriqueDeParution.Vide)
            .Select(f => new FaitEcranDto(f.Cle, f.Famille.ToString(), f.Etiquette, f.Valeur, f.Texte))];
    }

    /// <summary>
    /// Le fuseau du foyer. Un identifiant inconnu (faute de frappe dans le `.env`, base
    /// tzdata absente de l'image) ne doit pas faire tomber l'écran : on retombe sur
    /// celui du conteneur, qui est déjà réglé par FUSEAU_HORAIRE.
    /// </summary>
    private static TimeZoneInfo Fuseau(MeteoOptions options)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(options.FuseauHoraire);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return TimeZoneInfo.Local;
        }
    }

    /// <summary>La composition pure — testée sans base.</summary>
    public static DonneesEcran Composer(
        DateTime maintenant,
        IReadOnlyList<OccurrenceDto> ouvertes,
        IReadOnlyList<OccurrenceDto> faites,
        PhraseDuJour? phrase,
        MeteoDto? meteo,
        IReadOnlyList<EvenementExterneDto> evenements,
        IReadOnlyList<CompteARebours> comptes,
        string? lieu = null,
        DateOnly? premiereParution = null,
        ReglagesDuCiel? ciel = null)
    {
        var aujourdhui = DateOnly.FromDateTime(maintenant);

        // Les ouvertes arrivent triées par échéance (les retards d'abord) ; les faites
        // suivent, barrées. Le plafond s'applique au total : mieux vaut « + 3 autres »
        // qu'une liste qui déborde du cadre.
        var toutes = ouvertes
            .Select(o => new LigneEcranDto(o.Titre, o.AssigneA?.NomAffichage, Faite: false,
                JoursDeRetard: o.Echeance is { } e && e < aujourdhui ? aujourdhui.DayNumber - e.DayNumber : 0))
            .Concat(faites.Select(o => new LigneEcranDto(
                o.Titre, o.CompleteePar?.NomAffichage ?? o.AssigneA?.NomAffichage, Faite: true, JoursDeRetard: 0)))
            .ToList();

        MeteoEcranDto? meteoEcran = null;
        var jour = meteo?.Jours.FirstOrDefault(j => j.Date == aujourdhui);
        if (jour is not null)
        {
            meteoEcran = new MeteoEcranDto(
                meteo!.Maintenant?.CodeMeteo ?? jour.CodeMeteo,
                meteo.Maintenant?.TemperatureC ?? jour.TempMax,
                jour.TempMin,
                jour.TempMax,
                jour.ProbabilitePrecipitation,
                meteo.Verdicts);
        }

        // Quand ça déborde, la mention « + N autres » occupe la dernière place :
        // l'écran a exactement MaxLignes rangées, jamais une de plus.
        var visibles = toutes.Count > MaxLignes ? MaxLignes - 1 : toutes.Count;

        return new DonneesEcran(
            aujourdhui,
            new DateTimeOffset(maintenant),
            phrase is null ? null : new PhraseEcranDto(phrase.Titre, phrase.SousTitre),
            toutes.Take(visibles).ToList(),
            toutes.Count - visibles,
            ouvertes.Count,
            ouvertes.Count(o => o.Echeance is { } e && e < aujourdhui),
            faites.Count,
            meteoEcran,
            evenements.Where(e => e.Date == aujourdhui).ToList(),
            evenements.FirstOrDefault(e => e.Type == nameof(TypeFluxExterne.Collecte) && e.Date >= aujourdhui),
            comptes.Where(c => c.DateCible >= aujourdhui).OrderBy(c => c.DateCible)
                .Select(c => new CompteEcranDto(c.Titre, c.DateCible)).FirstOrDefault(),
            string.IsNullOrWhiteSpace(lieu) ? null : lieu.Trim(),
            NumeroEdition(aujourdhui, premiereParution),
            FondsDuJour(aujourdhui, ouvertes, ciel));
    }
}
