using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace HouseOs.Api.Infrastructure.Journalisation;

/// <summary>
/// Filet de sécurité du pipeline : jusqu'ici une exception non gérée mourait dans la
/// réponse 500 par défaut, sans une ligne de log — c'est précisément ce qui a rendu le
/// 503 de complétion impossible à enquêter.
///
/// Journalise l'exception avec l'identifiant de trace, puis rend un ProblemDetails qui
/// porte le même identifiant : l'écran affiche ce que Seq permet de retrouver.
/// </summary>
public sealed class GestionnaireExceptions(
    IProblemDetailsService problemes,
    ILogger<GestionnaireExceptions> journal) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext contexte, Exception exception, CancellationToken annulation)
    {
        var identifiant = Trace.Identifiant(contexte);

        // Un client qui referme son onglet en plein vol n'est pas une panne : le
        // distinguer évite de noyer les vraies exceptions sous du bruit.
        if (contexte.RequestAborted.IsCancellationRequested && exception is OperationCanceledException)
        {
            journal.LogWarning(
                "Requête abandonnée par le client — {Methode} {Chemin} (trace {TraceId}).",
                contexte.Request.Method, contexte.Request.Path.Value, identifiant);
            return false;
        }

        journal.LogError(
            exception,
            "Exception non gérée — {Methode} {Chemin} (trace {TraceId}).",
            contexte.Request.Method, contexte.Request.Path.Value, identifiant);

        contexte.Response.StatusCode = StatusCodes.Status500InternalServerError;
        return await problemes.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = contexte,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Erreur serveur",
                // Volontairement sans le message de l'exception : l'écran donne
                // l'identifiant, le détail reste dans Seq.
                Detail = $"Une erreur inattendue est survenue. Référence : {identifiant}",
            },
        });
    }
}
