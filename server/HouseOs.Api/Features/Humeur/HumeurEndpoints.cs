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

    /// <summary>La phrase la plus récente (soir sinon matin, aujourd'hui sinon hier) —
    /// partagée avec la vue e-ink (Features/Affichage).</summary>
    public static Task<PhraseDuJour?> PhraseCouranteAsync(HouseOsDbContext db, DateOnly aujourdhui)
    {
        var hier = aujourdhui.AddDays(-1);
        return db.PhrasesDuJour
            .Where(p => p.Date == aujourdhui || p.Date == hier)
            .OrderByDescending(p => p.Date)
            // Colonne string : « Soir » passe avant « Matin » aussi en tri alphabétique.
            .ThenByDescending(p => p.Moment)
            .FirstOrDefaultAsync();
    }
}
