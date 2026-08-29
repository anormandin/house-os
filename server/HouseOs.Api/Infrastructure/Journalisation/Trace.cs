using System.Diagnostics;

namespace HouseOs.Api.Infrastructure.Journalisation;

/// <summary>
/// L'identifiant de corrélation d'une requête, sous la seule forme qu'on expose :
/// celle qu'Alain peut copier de la bannière d'erreur et coller dans Seq.
/// </summary>
public static class Trace
{
    /// <summary>En-tête de réponse portant l'identifiant, lu par le client web.</summary>
    public const string EnTete = "X-Trace-Id";

    /// <summary>
    /// Le <c>TraceId</c> de l'activité courante, avec repli sur l'identifiant de
    /// connexion de Kestrel : jamais vide, pour qu'un incident soit toujours citable.
    /// </summary>
    public static string Identifiant(HttpContext contexte) =>
        Activity.Current?.TraceId.ToString() ?? contexte.TraceIdentifier;
}
