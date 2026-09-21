using HouseOs.Api.Domaine;
using HouseOs.Api.Domaine.Editorial;
using HouseOs.Api.Domaine.Ephemerides;
using HouseOs.Api.Domaine.Humeur;
using HouseOs.Api.Features.Editorial;
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
/// <paramref name="EcheanceFerme"/> est le troisième cas du plancher : une date qui
/// vient du dehors et ne se négocie pas (D-2026-09-20 Échéance Ferme Explicite Sur La Tâche).
/// </summary>
public record LigneEcranDto(string Titre, string? Assigne, bool Faite, int JoursDeRetard, bool EcheanceFerme);

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

public record PlancherEcranDto(string Raison, string Titre);

/// <summary>
/// L'édition du jour telle que l'écran la reçoit : ce qui est <b>figé pour la journée</b>
/// (vault : D-2026-09-20 Une Édition Par Jour Matérialisée). La sélection et l'ordre
/// des widgets, figés eux aussi, se lisent dans l'ordre de <see cref="DonneesEcran.Faits"/>.
/// </summary>
public record EditionEcranDto(
    string Rang,
    string Surtitre,
    string Manchette,
    string Chapeau,
    List<string> Paragraphes,
    PlancherEcranDto? Plancher,
    string Source);

/// <summary>
/// Ce que la journée donne à lire avant toute mise en page : les occurrences dues, le
/// prochain compte à rebours, les prévisions, et le fonds de tiroir déjà classé. C'est la
/// matière commune du rendu et de l'éditorialiste — une seule lecture, pour que les deux
/// voient la même journée.
/// </summary>
public sealed record SourcesDuJour(
    DateOnly Aujourdhui,
    List<OccurrenceDto> Ouvertes,
    MeteoDto? Previsions,
    (string Titre, DateOnly DateCible)? ProchainCompte,
    IReadOnlyList<FaitDeTiroir> Faits,
    string? Lieu);

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
    CompteEcranDto? ProchainCompte,
    string? Lieu,
    int? NumeroEdition,
    List<FaitEcranDto> Faits,
    EditionEcranDto? Edition);

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

    /// <summary>
    /// Lit tout ce qu'il faut puis compose. Une seule lecture d'horloge.
    /// <paramref name="persisterEdition"/> : vrai sur le chemin de l'appareil et de
    /// l'aperçu du jour même — l'édition manquante ou dépassée par le plancher est alors
    /// posée en gabarit et l'éditorialiste réveillé ; faux pour une horloge d'essai sur
    /// un autre jour, qu'on ne veut pas matérialiser.
    /// </summary>
    public static async Task<DonneesEcran> LireAsync(
        HouseOsDbContext db, DateTime maintenant, string? lieu = null, MeteoOptions? options = null,
        BanqueDuHasard? banqueDuHasard = null, MomentJournee creneau = MomentJournee.Soir,
        bool persisterEdition = false, SignalDeReedition? signal = null, ILogger? journal = null,
        CancellationToken ct = default)
    {
        var aujourdhui = DateOnly.FromDateTime(maintenant);
        // Bornes de la journée locale : le serveur vit en heure locale (TZ du
        // conteneur), et deux constructions séparées tiennent le jour du changement
        // d'heure (un AddDays sur l'offset ne le tiendrait pas). En UTC pour Npgsql,
        // qui refuse tout autre offset sur timestamptz.
        var debutJour = new DateTimeOffset(maintenant.Date).ToUniversalTime();
        var finJour = new DateTimeOffset(maintenant.Date.AddDays(1)).ToUniversalTime();

        // La mémoire des sept derniers jours d'abord : c'est elle qui classe le fonds.
        var memoire = await MemoireDesEditions.LireAsync(db, aujourdhui, ct);
        var sources = await LireLesSourcesAsync(db, maintenant, lieu, options, banqueDuHasard, memoire.Fraicheur);
        var edition = await GenerationEdition.AssurerAsync(db, sources, persisterEdition, signal, journal, ct);

        var faites = await OperationsTaches.ListerOccurrencesAsync(db, "faites", aujourdhui, debutJour, finJour);
        // La phrase du créneau composé, pas « la plus récente » : à sept heures du
        // matin, celle du soir n'a pas encore eu lieu.
        var phrase = await HumeurEndpoints.PhraseCouranteAsync(db, aujourdhui, creneau);
        var fin = aujourdhui.AddDays(FenetreJours);
        var evenements = await db.EvenementsExternes
            .Where(e => e.Date >= aujourdhui && e.Date < fin)
            .Join(db.FluxExternes, e => e.FluxExterneId, f => f.Id, (e, f) => new { e, f })
            .OrderBy(x => x.e.Date).ThenBy(x => x.e.Heure)
            .Select(x => new EvenementExterneDto(x.e.Titre, x.f.Type.ToString(), x.e.Date, x.e.Heure))
            .ToListAsync(ct);
        // Le numéro d'édition compte les jours depuis la première chose que la maison
        // a consignée. Journal vide (une installation neuve) : pas de numéro plutôt
        // qu'un « N° 1 » qui vieillirait mal.
        var premiereEntree = await db.Journal
            .OrderBy(e => e.CompleteeLe)
            .Select(e => (DateTimeOffset?)e.CompleteeLe)
            .FirstOrDefaultAsync(ct);

        return Composer(
            maintenant, sources.Ouvertes, faites, phrase, sources.Previsions, evenements,
            sources.ProchainCompte, lieu,
            premiereEntree is { } d ? DateOnly.FromDateTime(d.LocalDateTime) : null,
            sources.Faits, edition);
    }

    /// <summary>
    /// La matière commune du rendu et de l'éditorialiste : les occurrences dues, le
    /// prochain compte à rebours, les prévisions, et le fonds de tiroir classé avec la
    /// mémoire fournie. Aucune écriture.
    /// </summary>
    public static async Task<SourcesDuJour> LireLesSourcesAsync(
        HouseOsDbContext db, DateTime maintenant, string? lieu, MeteoOptions? options,
        BanqueDuHasard? banqueDuHasard, HistoriqueDeParution historique)
    {
        var aujourdhui = DateOnly.FromDateTime(maintenant);
        var ouvertes = await OperationsTaches.ListerOccurrencesAsync(db, "aujourdhui", aujourdhui, null, null);
        var previsions = await MeteoEndpoints.LireAsync(db, maintenant);
        var compte = await db.ComptesARebours
            .Where(c => c.DateCible >= aujourdhui)
            .OrderBy(c => c.DateCible)
            .Select(c => new { c.Titre, c.DateCible })
            .FirstOrDefaultAsync();
        // Les zones extérieures : le seul signal « la journée est physique » que le
        // modèle porte vraiment. Il n'y a pas de catégorie sur la tâche, et il n'y en
        // aura pas (vault : D-2026-09-20 Regroupement Sans Catégorie De Tâche).
        var zonesDehors = await db.Zones
            .Where(z => z.Type == TypeZone.Exterieur)
            .Select(z => z.Id)
            .ToListAsync();

        var jour = previsions?.Jours.FirstOrDefault(j => j.Date == aujourdhui);
        var climat = options is null ? null : await LireLeClimatAsync(db, aujourdhui, options);
        // Le maximum du jour vient des prévisions : c'est lui qui transforme « il a
        // fait 14 °C l'an dernier » en comparaison. Absent, le fait dira autre chose
        // plutôt que de se taire.
        var faits = FondsDuJour(
            aujourdhui, ouvertes,
            options is null ? null : new ReglagesDuCiel(
                new Lieu(options.Latitude, options.Longitude), options.Fuseau(), zonesDehors.ToHashSet()),
            await LireLaMaisonAsync(db, aujourdhui),
            await LireLeCalendrierAsync(db, aujourdhui),
            await LireLaVilleAsync(db, aujourdhui, new DateTimeOffset(maintenant)),
            banqueDuHasard,
            climat is null ? null : climat with { MaxDAujourdhuiC = jour?.TempMax },
            historique);

        return new SourcesDuJour(
            aujourdhui, ouvertes, previsions,
            compte is null ? null : (compte.Titre, compte.DateCible),
            faits,
            string.IsNullOrWhiteSpace(lieu) ? null : lieu.Trim());
    }

    /// <summary>
    /// Les collectes et les événements municipaux, par flux — la matière de la famille
    /// « la ville » (vault : Fonds De Tiroir). Aucune connaissance municipale ici : ce
    /// sont des flux externes comme les autres, et c'est leur <b>type</b> qui les range
    /// (vault : D-2026-09-20 Sources Municipales Séparées Par Solidité).
    ///
    /// <para>L'âge de chaque flux part d'ici : le fonds ne lit pas l'horloge, et c'est
    /// lui qui décide qu'un flux qu'on n'alimente plus cesse de parler. La fenêtre est
    /// celle de l'ingestion, pas celle de l'affichage — c'est sur ce que le flux porte
    /// qu'on peut dire d'une collecte qu'elle sort de l'ordinaire.</para>
    /// </summary>
    private static async Task<EtatDeLaVille> LireLaVilleAsync(
        HouseOsDbContext db, DateOnly aujourdhui, DateTimeOffset maintenant)
    {
        var debut = aujourdhui.AddDays(-1);
        var fin = aujourdhui.AddDays(FenetreJours);
        var flux = await db.FluxExternes
            .Where(f => f.Actif
                        && (f.Type == TypeFluxExterne.Collecte || f.Type == TypeFluxExterne.Municipal))
            .Select(f => new
            {
                f.Type,
                f.DernierRafraichissementLe,
                Evenements = f.Evenements
                    .Where(e => e.Date >= debut && e.Date < fin)
                    .Select(e => new EvenementDeLaVille(e.Date, e.Titre, e.Heure))
                    .ToList(),
            })
            .ToListAsync();

        List<FluxDeLaVille> DeType(TypeFluxExterne type) =>
            [.. flux
                .Where(f => f.Type == type)
                .Select(f => new FluxDeLaVille(
                    f.DernierRafraichissementLe is { } recuLe
                        ? Math.Max(0, (int)(maintenant - recuLe).TotalDays)
                        : null,
                    f.Evenements))];

        return new EtatDeLaVille(
            DeType(TypeFluxExterne.Collecte), DeType(TypeFluxExterne.Municipal), FenetreJours);
    }

    /// <summary>
    /// Les normales matérialisées du lieu, et la journée d'il y a un an — la matière de
    /// la famille « le climat » (vault : Fonds De Tiroir). Aucun appel réseau ici : les
    /// normales sont calculées une fois l'an par le worker, et relues telles quelles
    /// (vault : D-2026-09-20 Normales Climatiques Depuis L'archive Open-Meteo).
    ///
    /// <para>La clé des coordonnées fait le tri : des normales calculées pour l'ancienne
    /// adresse ne sont pas lues, elles sont absentes — jusqu'à ce que le worker les
    /// recalcule, la famille se tait plutôt que d'annoncer le gel d'ailleurs.</para>
    /// </summary>
    private static async Task<EtatDuClimat?> LireLeClimatAsync(
        HouseOsDbContext db, DateOnly aujourdhui, MeteoOptions options)
    {
        var cle = options.CleCoordonnees;
        var normales = await OperationsNormales.LireAsync(db, cle);
        if (normales is null)
        {
            return null;
        }
        var anDernier = await OperationsNormales.LireLeJourAsync(db, cle, aujourdhui.AddYears(-1));
        return EtatDuClimat.Depuis(normales, anDernier);
    }

    /// <summary>
    /// Le journal de complétion, les équipements et les zones, réduits à la matière de
    /// la famille « la maison » (vault : Fonds De Tiroir). Aucune table neuve : c'est
    /// tout l'intérêt de cette famille.
    ///
    /// <para>Le journal se lit <b>en entier</b>, parce que la série record se compte
    /// depuis le premier jour. C'est une seule colonne de dates sur une table qui
    /// grandit de quelques centaines de lignes par an dans un foyer de deux adultes ;
    /// le jour où ce n'est plus vrai, c'est un problème mesurable, pas supposé.</para>
    /// </summary>
    private static async Task<EtatDeLaMaison> LireLaMaisonAsync(HouseOsDbContext db, DateOnly aujourdhui)
    {
        // Jointure À GAUCHE, et c'est tout le sujet : le journal de complétion survit à
        // la suppression d'une tâche (HouseOsDbContext : « les ids restent comme
        // références historiques »). Une jointure interne perdrait ces entrées en
        // silence — une série de douze jours retomberait à quatre parce qu'une tâche a
        // été effacée, et le mur afficherait un chiffre faux sans que rien ne le dise.
        var journal = await db.Journal
            .GroupJoin(db.Taches, e => e.TacheId, t => t.Id, (e, taches) => new { e, taches })
            .SelectMany(x => x.taches.DefaultIfEmpty(), (x, t) => new
            {
                x.e.CompleteeLe,
                x.e.Cout,
                x.e.TacheId,
                Titre = t == null ? null : t.Titre,
                ZoneId = t == null ? null : t.ZoneId,
            })
            .ToListAsync();

        // L'heure du foyer, pas celle d'UTC : une complétion à 20 h le 19 septembre au
        // Québec est stockée au 20 en UTC, et compterait pour le mauvais jour de série.
        var jours = journal
            .Select(e => DateOnly.FromDateTime(e.CompleteeLe.LocalDateTime))
            .Distinct()
            .Order()
            .ToList();

        // Une tâche effacée ne peut plus être nommée : ses complétions comptent dans la
        // série, mais elles ne font pas de « 27 séances de quoi ? ».
        var seances = journal
            .Where(e => e.Titre != null)
            .GroupBy(e => e.TacheId)
            .Select(g => new SeancesDeTache(
                g.First().Titre!,
                g.Count(),
                g.Min(e => DateOnly.FromDateTime(e.CompleteeLe.LocalDateTime))))
            .ToList();

        var anneeCourante = journal
            .Where(e => e.Cout is > 0 && e.CompleteeLe.LocalDateTime.Year == aujourdhui.Year)
            .ToList();

        // Ordre explicite : Postgres rend les lignes comme il veut, et le fait
        // « ce jour-là l'an dernier » ne cite que le premier titre. Sans tri, il dirait
        // « C'était Boîtes » à un rendu et « C'était Tondre » au suivant — un fait dont
        // le texte change dans la journée ferait mentir l'édition matérialisée
        // (ContexteDuJour : ce qui en dépend est figé pour la journée entière).
        var lAnDernier = aujourdhui.AddYears(-1);
        var faitLAnDernier = journal
            .Where(e => e.Titre != null && DateOnly.FromDateTime(e.CompleteeLe.LocalDateTime) == lAnDernier)
            .OrderBy(e => e.CompleteeLe)
            .ThenBy(e => e.Titre, StringComparer.Ordinal)
            .Select(e => e.Titre!)
            .ToList();

        // Le prochain entretien d'un équipement : l'échéance ouverte la plus proche
        // parmi ses tâches. Rien d'ouvert, pas d'entretien à annoncer.
        var entretiens = await db.Occurrences
            .Where(o => o.Statut == StatutOccurrence.EnAttente && o.Echeance >= aujourdhui)
            .Join(db.Taches, o => o.TacheId, t => t.Id, (o, t) => new { t.EquipementId, o.Echeance })
            .Where(x => x.EquipementId != null)
            .GroupBy(x => x.EquipementId!.Value)
            .Select(g => new { EquipementId = g.Key, Prochaine = g.Min(x => x.Echeance) })
            .ToListAsync();
        var parEquipement = entretiens.ToDictionary(e => e.EquipementId, e => e.Prochaine);

        var equipements = await db.Equipements
            .Select(e => new { e.Id, e.Nom, e.DateAchat })
            .ToListAsync();

        var tachesParZone = await db.Taches
            .Where(t => t.ZoneId != null)
            .GroupBy(t => t.ZoneId!.Value)
            .Select(g => new { ZoneId = g.Key, Nombre = g.Count() })
            .ToListAsync();
        var comptesParZone = tachesParZone.ToDictionary(z => z.ZoneId, z => z.Nombre);
        var derniereParZone = journal
            .Where(e => e.ZoneId is not null)
            .GroupBy(e => e.ZoneId!.Value)
            .ToDictionary(g => g.Key, g => g.Max(e => DateOnly.FromDateTime(e.CompleteeLe.LocalDateTime)));

        var zones = await db.Zones.Select(z => new { z.Id, z.Nom }).ToListAsync();

        // Les anniversaires : l'arrivée d'un équipement, et les dates qui ont déjà eu
        // lieu au compte à rebours (l'emménagement d'il y a deux ans est toujours un
        // anniversaire, même quand le compte à rebours est retombé à zéro).
        var jalons = await db.ComptesARebours
            .Where(c => c.DateCible < aujourdhui)
            .Select(c => new { c.Titre, c.DateCible })
            .ToListAsync();

        return new EtatDeLaMaison(
            jours,
            seances,
            [.. equipements.Select(e => new EquipementDeLaMaison(
                e.Nom, e.DateAchat, parEquipement.TryGetValue(e.Id, out var p) ? p : null))],
            [.. zones.Select(z => new ZoneDeLaMaison(
                z.Nom,
                comptesParZone.TryGetValue(z.Id, out var n) ? n : 0,
                derniereParZone.TryGetValue(z.Id, out var d) ? d : null))],
            anneeCourante.Sum(e => e.Cout!.Value),
            anneeCourante.Count,
            [
                .. equipements.Where(e => e.DateAchat is not null)
                    .Select(e => new AnniversaireDeLaMaison(e.Nom, e.DateAchat!.Value)),
                .. jalons.Select(c => new AnniversaireDeLaMaison(c.Titre, c.DateCible)),
            ],
            faitLAnDernier);
    }

    /// <summary>
    /// Les comptes à rebours, les échéances à venir, les fenêtres saisonnières et les
    /// papiers qui expirent — la matière de la famille « le calendrier ».
    /// </summary>
    private static async Task<EtatDuCalendrier> LireLeCalendrierAsync(HouseOsDbContext db, DateOnly aujourdhui)
    {
        var horizon = aujourdhui.AddDays(FenetreJours);

        var compte = await db.ComptesARebours
            .Where(c => c.DateCible >= aujourdhui)
            .OrderBy(c => c.DateCible)
            .Select(c => new CompteDuCalendrier(c.Titre, c.DateCible))
            .FirstOrDefaultAsync();

        var caSEnVient = await db.Occurrences
            .Where(o => o.Statut == StatutOccurrence.EnAttente
                        && o.Echeance > aujourdhui && o.Echeance <= horizon)
            .Join(db.Taches, o => o.TacheId, t => t.Id, (o, t) => new EcheanceProchaine(t.Titre, o.Echeance!.Value))
            .ToListAsync();

        // Les fenêtres saisonnières du moteur, exposées comme donnée lisible : quatre
        // nombres en base deviennent deux dates autour d'aujourd'hui.
        // AsNoTracking : la spec de récurrence est une entité possédée, et EF refuse de
        // suivre un possédé sans son propriétaire. Cette lecture ne modifie rien.
        var saisonnieres = await db.Taches
            .AsNoTracking()
            .Where(t => t.Recurrence.FenetreDebutMois != null)
            .Select(t => new { t.Titre, t.Recurrence })
            .ToListAsync();
        var saisons = saisonnieres
            .Select(t => (t.Titre, Fenetre: t.Recurrence.FenetreAutour(aujourdhui)))
            .Where(t => t.Fenetre is not null)
            .Select(t => new FenetreDeSaison(t.Titre, t.Fenetre!.Value.Debut, t.Fenetre!.Value.Fin))
            .ToList();

        var garanties = await db.Equipements
            .Where(e => e.FinGarantie != null && e.FinGarantie >= aujourdhui)
            .Select(e => new ExpirationProchaine(e.Nom, e.FinGarantie!.Value, true))
            .ToListAsync();
        var papiers = await db.Documents
            .Where(d => d.Echeance != null && d.Echeance >= aujourdhui)
            .Select(d => new ExpirationProchaine(d.Titre, d.Echeance!.Value, false))
            .ToListAsync();

        return new EtatDuCalendrier(compte, caSEnVient, saisons, [.. garanties, .. papiers]);
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
    /// Le fonds de tiroir du jour, déjà classé. L'historique de parution est la mémoire
    /// des sept dernières éditions (vault : D-2026-09-20 Une Édition Par Jour
    /// Matérialisée) ; vide, c'est l'état d'une installation neuve, et aucun fait n'est
    /// pénalisé.
    /// </summary>
    public static IReadOnlyList<FaitDeTiroir> FondsDuJour(
        DateOnly aujourdhui,
        IReadOnlyList<OccurrenceDto> ouvertes,
        ReglagesDuCiel? ciel,
        EtatDeLaMaison? maison,
        EtatDuCalendrier? calendrier,
        EtatDeLaVille? ville,
        BanqueDuHasard? hasard,
        EtatDuClimat? climat,
        HistoriqueDeParution? historique = null)
    {
        // Chaque famille a sa source, et chacune est facultative : la composition sort
        // avec ce qu'elle a, jamais en mode dégradé.
        var contexte = new ContexteDuJour(
            aujourdhui,
            ciel is null ? null : new PointDObservation(ciel.Coordonnees, ciel.Fuseau),
            ciel is not null && ouvertes.Any(o => o.ZoneId is { } zone && ciel.ZonesExterieures.Contains(zone)),
            climat,
            maison,
            calendrier,
            ville,
            hasard);

        return Tiroir.Ouvrir(contexte, historique ?? HistoriqueDeParution.Vide);
    }

    /// <summary>Les types de flux dont la famille « la ville » a la charge
    /// (vault : Fonds De Tiroir).</summary>
    private static bool EstDeLaVille(string type) =>
        type == nameof(TypeFluxExterne.Collecte) || type == nameof(TypeFluxExterne.Municipal);

    /// <summary>La composition pure depuis les sources brutes — testée sans base.</summary>
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
        ReglagesDuCiel? ciel = null,
        EtatDeLaMaison? maison = null,
        EtatDuCalendrier? calendrier = null,
        EtatDeLaVille? ville = null,
        BanqueDuHasard? hasard = null,
        EtatDuClimat? climat = null,
        HistoriqueDeParution? historique = null,
        Edition? edition = null)
    {
        var aujourdhui = DateOnly.FromDateTime(maintenant);
        var jour = meteo?.Jours.FirstOrDefault(j => j.Date == aujourdhui);
        var faits = FondsDuJour(
            aujourdhui, ouvertes, ciel, maison, calendrier, ville, hasard,
            climat is null ? null : climat with { MaxDAujourdhuiC = jour?.TempMax },
            historique);
        var compte = comptes.Where(c => c.DateCible >= aujourdhui).OrderBy(c => c.DateCible).FirstOrDefault();
        return Composer(
            maintenant, ouvertes, faites, phrase, meteo, evenements,
            compte is null ? null : (compte.Titre, compte.DateCible),
            lieu, premiereParution, faits, edition);
    }

    /// <summary>
    /// La composition pure depuis le fonds déjà classé et l'édition du jour. L'édition
    /// fige la sélection et l'ordre des widgets : les faits qu'elle a publiés passent
    /// d'abord, dans son ordre, et les autres suivent au score du moment — un fait
    /// apparu dans la journée ne bouscule pas ce que le matin a choisi.
    /// </summary>
    public static DonneesEcran Composer(
        DateTime maintenant,
        IReadOnlyList<OccurrenceDto> ouvertes,
        IReadOnlyList<OccurrenceDto> faites,
        PhraseDuJour? phrase,
        MeteoDto? meteo,
        IReadOnlyList<EvenementExterneDto> evenements,
        (string Titre, DateOnly DateCible)? prochainCompte,
        string? lieu,
        DateOnly? premiereParution,
        IReadOnlyList<FaitDeTiroir> faits,
        Edition? edition)
    {
        var aujourdhui = DateOnly.FromDateTime(maintenant);

        // Les ouvertes arrivent triées par échéance (les retards d'abord) ; les faites
        // suivent, barrées. Le plafond s'applique au total : mieux vaut « + 3 autres »
        // qu'une liste qui déborde du cadre.
        var toutes = ouvertes
            .Select(o => new LigneEcranDto(o.Titre, o.AssigneA?.NomAffichage, Faite: false,
                JoursDeRetard: o.Echeance is { } e && e < aujourdhui ? aujourdhui.DayNumber - e.DayNumber : 0,
                EcheanceFerme: o.EcheanceFerme))
            .Concat(faites.Select(o => new LigneEcranDto(
                o.Titre, o.CompleteePar?.NomAffichage ?? o.AssigneA?.NomAffichage, Faite: true, JoursDeRetard: 0,
                EcheanceFerme: false)))
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
            // Les collectes et la ville ont leur famille au fonds de tiroir depuis
            // l'étape 6, et elle sait se taire quand son flux n'est plus alimenté. Les
            // laisser aussi dans le bandeau du jour, qui ne juge rien, publierait deux
            // fois le même événement — et publierait celui d'un gratteur mort.
            evenements.Where(e => e.Date == aujourdhui && EstDeLaVille(e.Type) == false).ToList(),
            prochainCompte is { } c ? new CompteEcranDto(c.Titre, c.DateCible) : null,
            string.IsNullOrWhiteSpace(lieu) ? null : lieu.Trim(),
            NumeroEdition(aujourdhui, premiereParution),
            [.. DansLOrdreDeLEdition(faits, edition)
                .Select(f => new FaitEcranDto(f.Cle, f.Famille.ToString(), f.Etiquette, f.Valeur, f.Texte))],
            edition is null ? null : new EditionEcranDto(
                edition.Rang.ToString(),
                edition.Surtitre,
                edition.Manchette,
                edition.Chapeau,
                edition.Paragraphes,
                edition.Plancher is { } p ? new PlancherEcranDto(p.Raison.ToString(), p.Titre) : null,
                edition.Source.ToString()));
    }

    /// <summary>Les faits publiés par l'édition d'abord, dans son ordre ; le reste au score.</summary>
    public static IEnumerable<FaitDeTiroir> DansLOrdreDeLEdition(IReadOnlyList<FaitDeTiroir> faits, Edition? edition)
    {
        if (edition is null || edition.ClesPubliees.Count == 0)
        {
            return faits;
        }
        var parCle = faits.ToDictionary(f => f.Cle, StringComparer.Ordinal);
        var publies = edition.ClesPubliees
            .Where(parCle.ContainsKey)
            .Select(cle => parCle[cle])
            .ToList();
        var dejaPris = publies.Select(f => f.Cle).ToHashSet(StringComparer.Ordinal);
        return publies.Concat(faits.Where(f => dejaPris.Contains(f.Cle) == false));
    }
}
