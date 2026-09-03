using HouseOs.Api.Domaine;
using HouseOs.Api.Domaine.Humeur;
using HouseOs.Api.Features.FluxExternes;
using HouseOs.Api.Features.Humeur;
using HouseOs.Api.Features.Meteo;
using HouseOs.Api.Features.Taches;
using HouseOs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HouseOs.Api.Features.Affichage;

/// <summary>Une ligne de la liste du jour, déjà réduite à ce que l'écran montre.</summary>
public record LigneEcranDto(string Titre, string? Assigne, bool Faite, bool EnRetard);

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
    CompteEcranDto? ProchainCompte);

public static class ComposerDonneesEcran
{
    /// <summary>Au-delà, on élague : un écran mural ne se fait pas défiler.</summary>
    public const int MaxLignes = 10;

    /// <summary>Fenêtre de recherche de la prochaine collecte et du prochain compte à rebours.</summary>
    private const int FenetreJours = 60;

    /// <summary>Lit tout ce qu'il faut puis compose. Une seule lecture d'horloge.</summary>
    public static async Task<DonneesEcran> LireAsync(HouseOsDbContext db, DateTime maintenant)
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
        var meteo = await MeteoEndpoints.LireAsync(db, maintenant);
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

        return Composer(maintenant, ouvertes, faites, phrase, meteo, evenements, comptes);
    }

    /// <summary>La composition pure — testée sans base.</summary>
    public static DonneesEcran Composer(
        DateTime maintenant,
        IReadOnlyList<OccurrenceDto> ouvertes,
        IReadOnlyList<OccurrenceDto> faites,
        PhraseDuJour? phrase,
        MeteoDto? meteo,
        IReadOnlyList<EvenementExterneDto> evenements,
        IReadOnlyList<CompteARebours> comptes)
    {
        var aujourdhui = DateOnly.FromDateTime(maintenant);

        // Les ouvertes arrivent triées par échéance (les retards d'abord) ; les faites
        // suivent, barrées. Le plafond s'applique au total : mieux vaut « + 3 autres »
        // qu'une liste qui déborde du cadre.
        var toutes = ouvertes
            .Select(o => new LigneEcranDto(o.Titre, o.AssigneA?.NomAffichage, Faite: false,
                EnRetard: o.Echeance is { } e && e < aujourdhui))
            .Concat(faites.Select(o => new LigneEcranDto(
                o.Titre, o.CompleteePar?.NomAffichage ?? o.AssigneA?.NomAffichage, Faite: true, EnRetard: false)))
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
                .Select(c => new CompteEcranDto(c.Titre, c.DateCible)).FirstOrDefault());
    }
}
