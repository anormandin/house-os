using System.ComponentModel;
using HouseOs.Api.Features.FluxIcal;
using HouseOs.Api.Infrastructure;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace HouseOs.Api.Features.Mcp;

[McpServerToolType]
public static class OutilsIcal
{
    [McpServerTool(Name = "mon_flux_ical")]
    [Description("Chemin du flux calendrier iCal personnel d'une personne (occurrences en attente " +
        "qui la concernent + comptes à rebours), et son URL publique (Funnel) si configurée. " +
        "Le chemin est à préfixer de l'URL de base de l'app pour un abonnement interne " +
        "(ex. http://serveur:8080/ical/….ics).")]
    public static async Task<object> MonFluxIcal(
        HouseOsDbContext db,
        IConfiguration config,
        [Description("La personne : 'alain' ou 'ariane'.")] string agirComme,
        [Description("true = régénérer le jeton (révocation : l'ancienne URL meurt, " +
            "les abonnements existants sont à refaire).")] bool? regenerer = null)
    {
        var utilisateur = await AgirComme.ResoudreAsync(db, agirComme);
        if (regenerer == true)
        {
            utilisateur.JetonIcal = JetonIcal.Generer();
            await db.SaveChangesAsync();
        }
        if (string.IsNullOrEmpty(utilisateur.JetonIcal))
        {
            throw new McpException("Aucun jeton iCal pour cette personne (regénéré au prochain démarrage).");
        }
        return JetonIcal.ReponseFlux(utilisateur.JetonIcal, config);
    }
}
