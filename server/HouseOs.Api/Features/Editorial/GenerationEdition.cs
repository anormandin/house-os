using HouseOs.Api.Domaine.Editorial;
using HouseOs.Api.Domaine.Humeur;
using HouseOs.Api.Features.Affichage;
using HouseOs.Api.Features.FondsDeTiroir;
using HouseOs.Api.Features.Humeur;
using HouseOs.Api.Features.Meteo;
using HouseOs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HouseOs.Api.Features.Editorial;

/// <summary>
/// La fabrication d'une édition, au même endroit pour ses trois appelants :
/// <list type="bullet">
/// <item>le <b>rendu</b> (<see cref="AssurerAsync"/>), qui veut une édition tout de
/// suite et sans LLM — il pose un gabarit quand elle manque ou quand le plancher a
/// changé, lève le drapeau, et réveille le service ;</item>
/// <item>le <b>service de fond</b> (<see cref="GenererAsync"/>, <c>remplacer: false</c>),
/// qui écrit avec Opus ce qui manque ou ce qui porte le drapeau ;</item>
/// <item>la <b>régénération à la main</b> (<c>remplacer: true</c>), qui réécrit même ce
/// qui existe.</item>
/// </list>
/// Une seule couture, sinon le prompt et le repli divergeraient un jour.
/// </summary>
public static class GenerationEdition
{
    /// <summary>
    /// Les clés que le journal montre <b>ailleurs</b> qu'en widget et qui ne comptent
    /// donc pas dans le budget du rang — le pendant serveur de `CLES_DEJA_AU_JOURNAL`
    /// (`web/src/lib/ecran-vues.ts`). Elles sont publiées quand même : l'encadré et la
    /// place fixe de la collecte les dessinent quoi qu'il arrive.
    /// </summary>
    public static readonly IReadOnlySet<string> ClesDejaAuJournal =
        new HashSet<string>(StringComparer.Ordinal) { "calendrier.compte-a-rebours", "ville.collecte" };

    /// <summary>Le rang, le plancher et les clés publiées : ce que l'édition fige et qui
    /// se calcule sans modèle.</summary>
    public sealed record Cadre(RangEdition Rang, PlancherDuJour? Plancher, List<string> ClesPubliees);

    public static Cadre Cadrer(SourcesDuJour sources)
    {
        var plancher = Plancher.Evaluer(
            sources.Ouvertes.Select(o => new OccurrenceDue(o.Titre, o.Echeance, o.EcheanceFerme)),
            sources.ProchainCompte,
            sources.Aujourdhui);
        var rang = RangDuJour.Calculer(sources.Ouvertes.Count, plancher is not null);
        return new Cadre(rang, plancher, ClesAPublier(sources.Faits, rang));
    }

    /// <summary>
    /// Les faits que l'édition publie, dans l'ordre du score : le budget du rang, plus
    /// ceux que le journal dessine à part. C'est cette liste qui devient la mémoire de
    /// fraîcheur des sept prochains jours.
    /// </summary>
    public static List<string> ClesAPublier(IReadOnlyList<FaitDeTiroir> faits, RangEdition rang)
    {
        var budget = RangDuJour.BudgetDeWidgets(rang);
        var cles = new List<string>();
        var comptees = 0;
        foreach (var fait in faits)
        {
            if (ClesDejaAuJournal.Contains(fait.Cle))
            {
                cles.Add(fait.Cle);
            }
            else if (comptees < budget)
            {
                cles.Add(fait.Cle);
                comptees++;
            }
        }
        return cles;
    }

    /// <summary>
    /// L'édition du jour pour le rendu. Jamais d'appel LLM ici : quand elle manque, ou
    /// quand le plancher réévalué ne correspond plus à celui de l'édition, un gabarit
    /// prend la place tout de suite, avec le drapeau levé pour que l'éditorialiste
    /// repasse (D-2026-09-20 Une Édition Par Jour Matérialisée).
    ///
    /// <para><paramref name="persister"/> est faux pour une horloge d'essai sur un
    /// autre jour : on compose l'édition de ce jour-là sans l'écrire, sinon un aperçu
    /// de Noël matérialiserait une édition de Noël écrite avec les faits de septembre.</para>
    /// </summary>
    public static async Task<Edition> AssurerAsync(
        HouseOsDbContext db,
        SourcesDuJour sources,
        bool persister,
        SignalDeReedition? signal,
        ILogger? journal,
        CancellationToken ct)
    {
        var cadre = Cadrer(sources);
        // Sans persistance, on lit sans suivre : l'édition composée pour un aperçu ne
        // doit pas rester modifiée dans le contexte, où un SaveChanges ultérieur
        // l'écrirait par-dessus une édition d'Opus.
        var lecture = persister ? db.Editions : db.Editions.AsNoTracking();
        var existante = await lecture.SingleOrDefaultAsync(e => e.Date == sources.Aujourdhui, ct);

        if (existante is not null && existante.Plancher == cadre.Plancher)
        {
            return existante;
        }

        var edition = existante ?? new Edition { Date = sources.Aujourdhui, Manchette = "" };
        var raison = existante is null ? "édition manquante" : "plancher changé";
        Appliquer(edition, GabaritEdition.Ecrire(cadre.Plancher, await PhraseDeRepli(db, sources.Aujourdhui, ct)),
            cadre, SourceEdition.Gabarit, modele: null);

        if (persister == false)
        {
            return edition;
        }
        edition.ReeditionEnAttente = true;
        if (existante is null)
        {
            db.Editions.Add(edition);
        }
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException) when (existante is null)
        {
            // Deux premiers rendus de la journée en même temps (l'appareil et un
            // navigateur) : l'autre a gagné l'index unique. On sert la sienne plutôt
            // qu'un 500 au mur.
            db.Entry(edition).State = EntityState.Detached;
            return await db.Editions.SingleAsync(e => e.Date == sources.Aujourdhui, ct);
        }
        journal?.LogInformation("Édition du {Date} posée en gabarit ({Raison}) — l'éditorialiste repassera.",
            sources.Aujourdhui, raison);
        signal?.Demander();
        return edition;
    }

    /// <summary>
    /// L'édition écrite par l'éditorialiste. <paramref name="remplacer"/> à faux, une
    /// édition déjà écrite et sans drapeau est rendue telle quelle et <b>aucun appel LLM
    /// n'a lieu</b> — c'est le cas du service de fond, qui repasse à chaque réveil.
    /// <paramref name="maintenant"/> est l'heure <b>vraie</b> : les sources se lisent à
    /// cette heure quand <paramref name="date"/> est aujourd'hui, et au matin de la date
    /// demandée sinon — jamais les tâches d'aujourd'hui sous la date d'hier.
    /// <paramref name="avantLeCreneau"/> : vrai quand on écrit la journée avant son
    /// créneau du matin (un rattrapage à minuit dix, un rendu de nuit) — l'édition
    /// garde alors son drapeau, et le créneau la réécrit avec les faits du matin (la
    /// météo fraîche, la ville poussée à 5 h 17) au lieu de la trouver déjà faite.
    /// </summary>
    /// <returns>L'édition et si elle vient d'être écrite.</returns>
    public static async Task<(Edition Edition, bool Generee)> GenererAsync(
        HouseOsDbContext db,
        IRedacteurEdition redacteur,
        MeteoOptions? meteo,
        BanqueDuHasard? banque,
        string? lieu,
        ILogger journal,
        DateOnly date,
        DateTime maintenant,
        bool remplacer,
        CancellationToken ct,
        bool avantLeCreneau = false)
    {
        var existante = await db.Editions.SingleOrDefaultAsync(e => e.Date == date, ct);
        if (existante is not null && remplacer == false && existante.ReeditionEnAttente == false)
        {
            return (existante, false);
        }

        var autreJour = DateOnly.FromDateTime(maintenant) != date;
        var horloge = autreJour ? date.ToDateTime(MomentDEssai.Matin) : maintenant;
        var memoire = await MemoireDesEditions.LireAsync(db, date, ct);
        var sources = await ComposerDonneesEcran.LireLesSourcesAsync(
            db, horloge, lieu, meteo, banque, memoire.Fraicheur);
        var cadre = Cadrer(sources);
        var matiere = Matiere(sources, cadre, memoire);

        var texte = await redacteur.RedigerAsync(matiere, ct);
        var source = texte is null ? SourceEdition.Gabarit : SourceEdition.Llm;
        texte ??= GabaritEdition.Ecrire(cadre.Plancher, await PhraseDeRepli(db, date, ct));

        var edition = existante ?? new Edition { Date = date, Manchette = "" };
        var modele = source == SourceEdition.Llm ? redacteur.Modele : null;
        Appliquer(edition, texte, cadre, source, modele);
        // Le drapeau tombe même en gabarit : un modèle qui a échoué ne se rappelle pas
        // à chaque réveil, il se rappelle au second essai. Il reste levé sur une édition
        // écrite pour un autre jour (un essai) ou avant son créneau du matin : le matin
        // venu, l'éditorialiste la réécrit avec les faits de ce matin-là.
        var aReecrire = autreJour || avantLeCreneau;
        edition.ReeditionEnAttente = aReecrire;
        if (existante is null)
        {
            db.Editions.Add(edition);
        }
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException) when (existante is null)
        {
            // Le rattrapage du démarrage et une régénération à la main ont écrit la
            // même journée à la même minute (vu en prod au release de l'étape 7 : deux
            // appels Opus, une seule ligne possible). Le perdant relit la ligne du
            // gagnant ; s'il devait remplacer, il y pose son texte, sinon il la sert.
            db.Entry(edition).State = EntityState.Detached;
            var gagnante = await db.Editions.SingleAsync(e => e.Date == date, ct);
            if (remplacer == false)
            {
                return (gagnante, false);
            }
            Appliquer(gagnante, texte, cadre, source, modele);
            gagnante.ReeditionEnAttente = aReecrire;
            await db.SaveChangesAsync(ct);
            edition = gagnante;
        }
        journal.LogInformation("Édition du {Date} écrite via {Source} — rang {Rang}, {Cles} clés publiées.",
            date, source, cadre.Rang, cadre.ClesPubliees.Count);
        return (edition, true);
    }

    /// <summary>
    /// Le second essai de la journée : quand le modèle n'a pas répondu au premier (une
    /// API surchargée à 5 h 31), l'édition est restée un gabarit et le service de fond
    /// repasse <b>une fois</b>, plus tard. Rien à faire si entre-temps quelqu'un a écrit
    /// avec le modèle — une régénération à la main, par exemple. Un second échec
    /// n'écrit <b>rien</b> : le gabarit garde son heure, et c'est elle qui borne la
    /// fenêtre du second essai (<see cref="EditorialisteService.MomentDuReessai"/>) —
    /// réécrire le gabarit rouvrirait la fenêtre à chaque heure.
    /// </summary>
    /// <returns>Vrai si un essai a eu lieu.</returns>
    public static async Task<bool> ReessayerAsync(
        HouseOsDbContext db,
        IRedacteurEdition redacteur,
        MeteoOptions? meteo,
        BanqueDuHasard? banque,
        string? lieu,
        ILogger journal,
        DateOnly date,
        DateTime maintenant,
        CancellationToken ct)
    {
        var existante = await db.Editions.SingleOrDefaultAsync(e => e.Date == date, ct);
        if (existante is null || existante.Source != SourceEdition.Gabarit)
        {
            return false;
        }
        journal.LogInformation("Édition du {Date} restée en gabarit — second essai.", date);
        var memoire = await MemoireDesEditions.LireAsync(db, date, ct);
        var sources = await ComposerDonneesEcran.LireLesSourcesAsync(db, maintenant, lieu, meteo, banque, memoire.Fraicheur);
        var cadre = Cadrer(sources);
        var texte = await redacteur.RedigerAsync(Matiere(sources, cadre, memoire), ct);
        if (texte is null)
        {
            journal.LogWarning("Édition du {Date} : le second essai n'a rien donné — gabarit jusqu'à demain.", date);
            return true;
        }
        Appliquer(existante, texte, cadre, SourceEdition.Llm, redacteur.Modele);
        existante.ReeditionEnAttente = false;
        await db.SaveChangesAsync(ct);
        journal.LogInformation("Édition du {Date} écrite via Llm au second essai — rang {Rang}.", date, cadre.Rang);
        return true;
    }

    private static void Appliquer(Edition edition, TexteDEdition texte, Cadre cadre, SourceEdition source, string? modele)
    {
        edition.Rang = cadre.Rang;
        edition.PlancherRaison = cadre.Plancher?.Raison;
        edition.PlancherTitre = cadre.Plancher?.Titre;
        edition.ClesPubliees = cadre.ClesPubliees;
        edition.Surtitre = texte.Surtitre;
        edition.Manchette = texte.Manchette;
        edition.Chapeau = texte.Chapeau;
        edition.Paragraphes = [.. texte.Paragraphes];
        edition.Rubriques = [.. texte.Rubriques];
        edition.Source = source;
        edition.Modele = modele;
        edition.GenereLe = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// La manchette et le chapeau du gabarit : la phrase du matin quand le titre
    /// d'humeur l'a déjà écrite, sinon sa banque — exactement ce que le mur montrait
    /// avant l'éditorialiste.
    /// </summary>
    private static async Task<(string Titre, string SousTitre)> PhraseDeRepli(
        HouseOsDbContext db, DateOnly date, CancellationToken ct)
    {
        var phrase = await db.PhrasesDuJour.AsNoTracking()
            .SingleOrDefaultAsync(p => p.Date == date && p.Moment == MomentJournee.Matin, ct);
        if (phrase is not null)
        {
            return (phrase.Titre, phrase.SousTitre);
        }
        return BanquePhrases.Generer(await ConstruireEtat.Construire(db, date, MomentJournee.Matin, ct));
    }

    /// <summary>Tout ce que l'éditorialiste reçoit — des faits, jamais une table.</summary>
    public static MatiereDEdition Matiere(SourcesDuJour sources, Cadre cadre, MemoireDesEditions memoire)
    {
        var aujourdhui = sources.Aujourdhui;
        var publiees = cadre.ClesPubliees.ToHashSet(StringComparer.Ordinal);
        var jour = sources.Previsions?.Jours.FirstOrDefault(j => j.Date == aujourdhui);
        return new MatiereDEdition(
            aujourdhui,
            cadre.Rang,
            cadre.Plancher,
            [.. sources.Ouvertes.Select(o =>
            {
                var groupe = (sources.Noms ?? NomsDeLaMaison.Vide).Grouper(o);
                return new TachePourEdition(
                    o.Titre,
                    o.Echeance is { } e && e < aujourdhui ? aujourdhui.DayNumber - e.DayNumber : 0,
                    o.EcheanceFerme,
                    o.AssigneA?.NomAffichage,
                    groupe.Zone,
                    groupe.Equipement);
            })],
            sources.ProchainCompte is { } c
                ? new CompteProcheDEdition(c.Titre, c.DateCible.DayNumber - aujourdhui.DayNumber)
                : null,
            jour is null ? null : new MeteoDEdition(MotsDuCiel(jour.CodeMeteo), jour.TempMin, jour.TempMax),
            [.. sources.Faits.Select(f => new FaitPourEdition(
                f.Cle, f.Famille.ToString(), f.Etiquette, f.Valeur, f.Texte, publiees.Contains(f.Cle)))],
            memoire.Precedentes,
            sources.Lieu);
    }

    /// <summary>Le code WMO en mots, grossièrement : le modèle n'a pas à connaître la
    /// table, et ne doit pas la deviner.</summary>
    private static string MotsDuCiel(int code) => code switch
    {
        0 => "ciel dégagé",
        1 or 2 => "quelques nuages",
        3 => "nuageux",
        45 or 48 => "brouillard",
        >= 51 and <= 67 => "pluie",
        >= 71 and <= 77 => "neige",
        >= 80 and <= 82 => "averses",
        85 or 86 => "averses de neige",
        >= 95 => "orages",
        _ => "temps variable",
    };
}
