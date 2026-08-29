using HouseOs.Api.Features.Taches;

namespace HouseOs.Api.Infrastructure;

/// <summary>
/// Les refus de validation de toutes les tranches passent par ici. Le corps était
/// copié à l'identique dans huit fichiers ; le rassembler donne surtout un endroit
/// unique où un refus laisse une trace — sans quoi « l'app a dit non » reste invisible
/// dans Seq et ne se distingue pas d'une panne.
/// </summary>
public static class ResultatsApi
{
    public static IResult Erreur(ILogger journal, string champ, string message)
    {
        journal.LogWarning("Refus de validation — {Champ} : {Raison}", champ, message);
        return Results.ValidationProblem(new Dictionary<string, string[]> { [champ] = [message] });
    }

    public static IResult Erreur(ILogger journal, ErreurValidation erreur) =>
        Erreur(journal, erreur.Champ, erreur.Message);
}
