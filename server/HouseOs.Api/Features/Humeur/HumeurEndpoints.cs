using HouseOs.Api.Domaine.Humeur;
using HouseOs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HouseOs.Api.Features.Humeur;

public record PhraseDuJourDto(string Titre, string SousTitre, string Source, DateTimeOffset GenereLe);

public static class HumeurEndpoints
{
    public static IEndpointRouteBuilder MapHumeur(this IEndpointRouteBuilder app)
    {
        // La phrase la plus récente du jour (soir sinon matin). 404 → le client
        // retombe sur sa banque locale (humeur.ts).
        app.MapGet("/api/phrase-du-jour", async (HouseOsDbContext db) =>
        {
            var phrase = await PhraseCouranteAsync(db, DateOnly.FromDateTime(DateTime.Now));

            return phrase is null
                ? Results.NotFound()
                : Results.Ok(new PhraseDuJourDto(
                    phrase.Titre, phrase.SousTitre, phrase.Source.ToString(), phrase.GenereLe));
        });

        return app;
    }

    /// <summary>
    /// La phrase en vigueur à un <b>créneau donné</b> : la plus récente qui ne soit pas
    /// postérieure à lui (soir sinon matin, aujourd'hui sinon hier) — partagée avec la
    /// vue e-ink (Features/Affichage).
    ///
    /// <para>Le créneau compte, et pas seulement la date. Sans lui, un journal composé
    /// pour sept heures du matin irait chercher la phrase du soir du même jour :
    /// invisible en marche normale (le soir n'est écrit qu'à 17 h), mais tout de suite
    /// faux dès qu'on compose un tirage d'essai « du matin » en soirée — la phrase
    /// qu'on vient de faire écrire n'apparaîtrait pas.</para>
    /// </summary>
    public static Task<PhraseDuJour?> PhraseCouranteAsync(
        HouseOsDbContext db, DateOnly aujourdhui, MomentJournee creneau = MomentJournee.Soir)
    {
        var hier = aujourdhui.AddDays(-1);
        var phrases = db.PhrasesDuJour.Where(p => p.Date == aujourdhui || p.Date == hier);
        if (creneau == MomentJournee.Matin)
        {
            // Le soir du jour même appartient à plus tard dans la journée ; celui de la
            // veille, lui, est le repli normal d'un matin pas encore écrit.
            phrases = phrases.Where(p => p.Date < aujourdhui || p.Moment == MomentJournee.Matin);
        }
        return phrases
            .OrderByDescending(p => p.Date)
            // Colonne string : « Soir » passe avant « Matin » aussi en tri alphabétique.
            .ThenByDescending(p => p.Moment)
            .FirstOrDefaultAsync();
    }
}
