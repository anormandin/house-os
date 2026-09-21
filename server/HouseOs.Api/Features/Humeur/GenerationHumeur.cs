using HouseOs.Api.Domaine.Humeur;
using HouseOs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HouseOs.Api.Features.Humeur;

/// <summary>
/// La fabrication d'une phrase du jour, au même endroit pour les deux appelants :
/// <see cref="HumeurService"/>, qui la veut <b>si elle manque</b> à l'heure du
/// créneau, et la demande à la main, qui la veut <b>même si elle existe</b>.
///
/// <para>Sans cette couture, forcer une régénération aurait voulu dire copier la
/// génération dans un second endroit — et la voir diverger le jour où le prompt
/// change.</para>
/// </summary>
public static class GenerationHumeur
{
    /// <summary>
    /// La phrase du créneau. <paramref name="remplacer"/> à faux, une phrase déjà
    /// matérialisée est rendue telle quelle et <b>aucun appel LLM n'a lieu</b> — c'est
    /// le cas du service de fond, qui repasse sur le même créneau à chaque réveil. À
    /// vrai, la phrase existante est remplacée : régénérer veut dire régénérer.
    /// </summary>
    /// <returns>La phrase et si elle vient d'être écrite.</returns>
    public static async Task<(PhraseDuJour Phrase, bool Generee)> GenererAsync(
        HouseOsDbContext db,
        HumeurOptions options,
        ILogger journal,
        DateOnly date,
        MomentJournee moment,
        bool remplacer,
        CancellationToken ct)
    {
        var existante = await db.PhrasesDuJour
            .SingleOrDefaultAsync(p => p.Date == date && p.Moment == moment, ct);
        if (existante is not null && remplacer == false)
        {
            return (existante, false);
        }

        var etat = await ConstruireEtat.Construire(db, date, moment, ct);
        var (titre, sousTitre, source) = await Ecrire(etat, options, journal, ct);

        if (existante is not null)
        {
            existante.Titre = titre;
            existante.SousTitre = sousTitre;
            existante.Source = source;
            existante.GenereLe = DateTimeOffset.UtcNow;
        }
        else
        {
            existante = new PhraseDuJour
            {
                Date = date,
                Moment = moment,
                Titre = titre,
                SousTitre = sousTitre,
                Source = source,
                GenereLe = DateTimeOffset.UtcNow,
            };
            db.PhrasesDuJour.Add(existante);
        }
        await db.SaveChangesAsync(ct);
        journal.LogInformation("Humeur : phrase du {Date} ({Moment}) générée via {Source}.",
            date, moment, source);
        return (existante, true);
    }

    /// <summary>
    /// L'état structuré, poli par le LLM quand une clé existe, sinon par la banque de
    /// gabarits. Le repli n'est pas un mode dégradé : c'est le fonctionnement normal
    /// d'une installation sans clé API.
    /// </summary>
    private static async Task<(string Titre, string SousTitre, SourcePhrase Source)> Ecrire(
        EtatMaison etat, HumeurOptions options, ILogger journal, CancellationToken ct)
    {
        var cle = options.CleEffective();
        if (cle is not null)
        {
            try
            {
                var polie = await PolissageLlm.Polir(etat, cle, options.Modele, ct);
                if (polie is not null)
                {
                    return (polie.Value.Titre, polie.Value.SousTitre, SourcePhrase.Llm);
                }
                journal.LogWarning("Humeur : réponse LLM inutilisable — repli sur la banque.");
            }
            catch (Exception ex) when (ct.IsCancellationRequested == false)
            {
                journal.LogWarning(ex, "Humeur : appel LLM raté — repli sur la banque.");
            }
        }

        var (titre, sousTitre) = BanquePhrases.Generer(etat);
        return (titre, sousTitre, SourcePhrase.Gabarit);
    }
}
