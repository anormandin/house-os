using HouseOs.Api.Domaine;
using HouseOs.Api.Domaine.Lettre;
using HouseOs.Api.Features.Affichage;
using HouseOs.Api.Features.Editorial;
using HouseOs.Api.Features.FondsDeTiroir;
using HouseOs.Api.Features.Meteo;
using HouseOs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HouseOs.Api.Features.Lettre;

/// <summary>
/// La couture de la lettre : lire la journée, l'étendre de ce que la lettre seule veut,
/// demander au modèle, écrire la ligne, envoyer. Trois appelants — le service de fond,
/// l'API, le MCP — et une seule ligne par date
/// (D-2026-09-21 Lettre Matérialisée Et Rattrapée Le Jour Même).
/// </summary>
public static class GenerationLettre
{
    public const int JoursDevant = 7;

    /// <summary>Tout ce que la lettre reçoit — la matière de l'édition, étendue.</summary>
    public static async Task<MatiereDeLettre> MatiereAsync(
        HouseOsDbContext db, MeteoOptions? meteo, BanqueDuHasard? banque, string? lieu,
        DateOnly date, DateTime maintenant, CancellationToken ct)
    {
        var autreJour = DateOnly.FromDateTime(maintenant) != date;
        var horloge = autreJour ? date.ToDateTime(MomentDEssai.Matin) : maintenant;
        var memoireEditions = await MemoireDesEditions.LireAsync(db, date, ct);
        var sources = await ComposerDonneesEcran.LireLesSourcesAsync(
            db, horloge, lieu, meteo, banque, memoireEditions.Fraicheur);
        var cadre = GenerationEdition.Cadrer(sources);
        var edition = GenerationEdition.Matiere(sources, cadre, memoireEditions);

        var fin = date.AddDays(JoursDevant);
        var semaine = await db.Occurrences.AsNoTracking()
            .Include(o => o.Tache)
            .Include(o => o.AssigneA)
            .Where(o => o.Statut == StatutOccurrence.EnAttente && o.Echeance > date && o.Echeance <= fin)
            .OrderBy(o => o.Echeance)
            .ToListAsync(ct);

        var precedentes = await MemoireDesLettres.LireAsync(db, date, ct);
        var derniereEnvoyee = await db.Lettres.AsNoTracking()
            .Where(l => l.Date < date && l.EnvoyeeLe != null)
            .OrderByDescending(l => l.Date)
            .Select(l => l.EnvoyeeLe)
            .FirstOrDefaultAsync(ct);
        // Npgsql n'écrit et ne compare que des instants UTC : tout ce qui part en
        // paramètre ou en colonne est normalisé ici, comme GenereLe sur l'édition.
        var depuis = derniereEnvoyee ?? new DateTimeOffset(horloge).ToUniversalTime().AddHours(-24);
        var faites = await db.Journal.AsNoTracking()
            .Where(j => j.CompleteeLe >= depuis)
            .Join(db.Taches, j => j.TacheId, t => t.Id, (j, t) => new { j.CompleteeLe, t.Titre, j.UtilisateurId })
            .Join(db.Utilisateurs, x => x.UtilisateurId, u => u.Id, (x, u) => new { x.CompleteeLe, x.Titre, u.NomAffichage })
            .OrderBy(x => x.CompleteeLe)
            .ToListAsync(ct);

        var tacheIds = sources.Ouvertes.Select(o => o.TacheId).Distinct().ToList();
        var depuisSerie = new DateTimeOffset(date.AddDays(-7 * Serie.Plafond - 1).ToDateTime(TimeOnly.MinValue)).ToUniversalTime();
        var coches = await db.Journal.AsNoTracking()
            .Where(j => tacheIds.Contains(j.TacheId) && j.CompleteeLe >= depuisSerie)
            .Select(j => new { j.TacheId, j.CompleteeLe })
            .ToListAsync(ct);
        var parTache = coches
            .GroupBy(c => c.TacheId)
            .ToDictionary(g => g.Key, g => g.Select(c => DateOnly.FromDateTime(c.CompleteeLe.ToLocalTime().DateTime)).ToList());
        var series = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var o in sources.Ouvertes)
        {
            var n = Serie.Compter(date, parTache.GetValueOrDefault(o.TacheId) ?? []);
            if (n > 0)
            {
                series[o.Titre] = n;
            }
        }

        return new MatiereDeLettre(
            edition,
            [.. semaine.Select(o => new EcheanceDevant(o.Echeance!.Value, o.Tache!.Titre, o.AssigneA?.NomAffichage, o.Tache.EcheanceFerme))],
            [.. faites.Select(f => new TacheFaite(DateOnly.FromDateTime(f.CompleteeLe.ToLocalTime().DateTime), f.Titre, f.NomAffichage))],
            series,
            precedentes);
    }

    /// <summary>
    /// La lettre composée et écrite en base, mais pas envoyée. <paramref name="remplacer"/>
    /// à faux, une lettre déjà composée est rendue telle quelle et <b>aucun appel LLM n'a
    /// lieu</b>. Quand le modèle se tait, la ligne porte la note de repli et sa matière,
    /// source Gabarit : c'est au service de décider s'il réessaie ou s'il l'envoie.
    /// </summary>
    public static async Task<(LettreDuMatin Lettre, bool Composee)> ComposerAsync(
        HouseOsDbContext db, IRedacteurLettre redacteur, MeteoOptions? meteo, BanqueDuHasard? banque,
        string? lieu, ILogger journal, DateOnly date, DateTime maintenant, bool remplacer, CancellationToken ct)
    {
        var existante = await db.Lettres.SingleOrDefaultAsync(l => l.Date == date, ct);
        if (existante is not null && remplacer == false)
        {
            return (existante, false);
        }

        var matiere = await MatiereAsync(db, meteo, banque, lieu, date, maintenant, ct);
        var matiereJson = RedactionLettre.SerialiserMatiere(matiere);
        var texte = await redacteur.RedigerAsync(matiere, ct);
        var source = texte is null ? SourceLettre.Gabarit : SourceLettre.Llm;
        texte ??= NoteDeRepli.Composer(matiere);

        var lettre = existante ?? new LettreDuMatin { Date = date, Sujet = "" };
        Appliquer(lettre, texte, source, source == SourceLettre.Llm ? redacteur.Modele : null, matiereJson, maintenant);
        if (existante is null)
        {
            db.Lettres.Add(lettre);
        }
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException) when (existante is null)
        {
            // Le rattrapage du démarrage et une régénération à la main ont composé la
            // même journée à la même minute : le perdant relit la ligne du gagnant.
            db.Entry(lettre).State = EntityState.Detached;
            return (await db.Lettres.SingleAsync(l => l.Date == date, ct), false);
        }
        journal.LogInformation("Lettre du {Date} composée via {Source} — {Paragraphes} paragraphe(s), sujet « {Sujet} ».",
            date, source, lettre.Paragraphes.Count, lettre.Sujet);
        return (lettre, true);
    }

    /// <summary>La lettre composée sans rien écrire : l'aperçu d'une autre journée, ou
    /// de celle-ci avant l'heure. Sans rédacteur, c'est la note — un aperçu qui ne fait
    /// pas attendre Opus.</summary>
    public static async Task<LettreDuMatin> ApercuAsync(
        HouseOsDbContext db, IRedacteurLettre? redacteur, MeteoOptions? meteo, BanqueDuHasard? banque,
        string? lieu, DateOnly date, DateTime maintenant, CancellationToken ct)
    {
        var matiere = await MatiereAsync(db, meteo, banque, lieu, date, maintenant, ct);
        var texte = redacteur is null ? null : await redacteur.RedigerAsync(matiere, ct);
        var source = texte is null ? SourceLettre.Gabarit : SourceLettre.Llm;
        texte ??= NoteDeRepli.Composer(matiere);
        var lettre = new LettreDuMatin { Date = date, Sujet = "" };
        Appliquer(lettre, texte, source, source == SourceLettre.Llm ? redacteur!.Modele : null,
            RedactionLettre.SerialiserMatiere(matiere), maintenant);
        return lettre;
    }

    /// <summary>Les habitants qui ont une adresse — ceux à qui la maison écrit.</summary>
    public static async Task<List<Destinataire>> DestinatairesAsync(HouseOsDbContext db, CancellationToken ct) =>
        await db.Utilisateurs.AsNoTracking()
            .Where(u => u.Courriel != null && u.Courriel != "")
            .OrderBy(u => u.NomAffichage)
            .Select(u => new Destinataire(u.NomAffichage, u.Courriel!))
            .ToListAsync(ct);

    /// <summary>
    /// L'envoi du jour : à tous ceux qui ont une adresse, puis la ligne est marquée
    /// envoyée. Faux, et rien de marqué, quand la porte est fermée ou qu'il n'y a
    /// personne à qui écrire — ce n'est pas une erreur, c'est journalisé. Une erreur
    /// SMTP, elle, remonte : c'est au service de repasser.
    /// </summary>
    public static async Task<bool> EnvoyerAsync(
        HouseOsDbContext db, IEnvoyeurDeCourriel envoyeur, LettreOptions options, LettreDuMatin lettre,
        ILogger journal, DateTime maintenant, CancellationToken ct)
    {
        if (envoyeur.Actif == false)
        {
            journal.LogInformation("Lettre du {Date} composée, pas envoyée : aucun SMTP configuré (Lettre:Smtp).", lettre.Date);
            return false;
        }
        var destinataires = await DestinatairesAsync(db, ct);
        if (destinataires.Count == 0)
        {
            journal.LogWarning("Lettre du {Date} composée, pas envoyée : aucun compte n'a d'adresse (COMPTE_n_COURRIEL).", lettre.Date);
            return false;
        }
        await envoyeur.EnvoyerAsync(RenduCourriel.Composer(lettre, destinataires, options), ct);
        lettre.EnvoyeeLe = new DateTimeOffset(maintenant).ToUniversalTime();
        lettre.Destinataires = [.. destinataires.Select(d => d.Adresse)];
        await db.SaveChangesAsync(ct);
        journal.LogInformation("Lettre du {Date} envoyée à {Nombre} adresse(s) via {Source}.",
            lettre.Date, destinataires.Count, lettre.Source);
        return true;
    }

    private static void Appliquer(
        LettreDuMatin lettre, TexteDeLettre texte, SourceLettre source, string? modele, string matiere, DateTime maintenant)
    {
        lettre.Sujet = texte.Sujet;
        lettre.Paragraphes = [.. texte.Paragraphes];
        lettre.Source = source;
        lettre.Modele = modele;
        lettre.Matiere = matiere;
        lettre.ComposeeLe = new DateTimeOffset(maintenant).ToUniversalTime();
    }
}
