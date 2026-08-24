using System.ComponentModel;
using HouseOs.Api.Infrastructure;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace HouseOs.Api.Features.Mcp;

[McpServerToolType]
public static class OutilsIcal
{
    [McpServerTool(Name = "mon_flux_ical")]
    [Description("Chemin du flux calendrier iCal personnel d'une personne (occurrences en attente " +
        "qui la concernent + comptes à rebours). À préfixer de l'URL de base de l'app pour " +
        "l'abonnement (ex. http://serveur:8080/ical/….ics).")]
    public static async Task<object> MonFluxIcal(
        HouseOsDbContext db,
        [Description("La personne : 'alain' ou 'ariane'.")] string agirComme)
    {
        var utilisateur = await AgirComme.ResoudreAsync(db, agirComme);
        if (string.IsNullOrEmpty(utilisateur.JetonIcal))
        {
            throw new McpException("Aucun jeton iCal pour cette personne (regénéré au prochain démarrage).");
        }
        return new { chemin = $"/ical/{utilisateur.JetonIcal}.ics" };
    }
}
