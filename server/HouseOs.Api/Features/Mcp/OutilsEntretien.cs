using System.ComponentModel;
using HouseOs.Api.Features.Entretien;
using HouseOs.Api.Features.Synchro;
using HouseOs.Api.Infrastructure;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace HouseOs.Api.Features.Mcp;

/// <summary>
/// Les packs d'entretien pour l'agent, en parité avec <c>/api/entretien</c>
/// (vault : D-2026-09-28 Packs D'entretien En Fichier De Données).
/// </summary>
[McpServerToolType]
public static class OutilsEntretien
{
    [McpServerTool(Name = "proposer_entretiens")]
    [Description("Propose les tâches d'entretien d'un pack : celui de la catégorie d'un équipement " +
        "(equipementId), ou le programme de la maison (sans equipementId). Chaque proposition porte " +
        "une clé, un titre, une récurrence complète et « dejaPresente » quand une tâche de même titre " +
        "existe déjà. Un équipement sans catégorie, ou de catégorie Autre, renvoie une liste vide avec " +
        "la raison. Faire confirmer la sélection par l'humain, puis adopter_entretiens.")]
    public static async Task<PropositionsDto> ProposerEntretiens(
        HouseOsDbContext db,
        PacksEntretien packs,
        [Description("Id d'un équipement (via lister_equipements), ou null pour le programme de la maison.")]
        Guid? equipementId = null) =>
        await OperationsEntretien.ProposerAsync(db, packs, equipementId)
            ?? throw new McpException($"Équipement introuvable : {equipementId}.");

    [McpServerTool(Name = "adopter_entretiens")]
    [Description("Crée les tâches d'un pack d'entretien à partir de leurs clés (proposer_entretiens), " +
        "en une transaction tout-ou-rien, liées à l'équipement et à sa pièce quand equipementId est " +
        "donné. Une clé inconnue, ou hors du pack visé, refuse le lot ; un titre déjà présent aussi " +
        "(rien n'est créé). Toujours faire confirmer par l'humain avant d'appeler.")]
    public static async Task<object> AdopterEntretiens(
        HouseOsDbContext db,
        PacksEntretien packs,
        IDiffuseurSynchro diffuseur,
        [Description("Nom d'utilisateur du membre au nom de qui les tâches sont créées (via lister_utilisateurs).")]
        string agirComme,
        [Description("Clés des propositions à adopter.")] string[] cles,
        [Description("Id de l'équipement (pack de sa catégorie), ou null pour le programme de la maison.")]
        Guid? equipementId = null)
    {
        var createur = await AgirComme.ResoudreAsync(db, agirComme);
        var resultat = await OperationsEntretien.AdopterAsync(
            db, packs, new AdopterRequete(cles ?? [], equipementId), createur.Id,
            DateTimeOffset.UtcNow, DateOnly.FromDateTime(DateTime.Now));
        if (resultat.Erreur is { } erreur)
        {
            throw new McpException(resultat.Statut == 409
                ? $"Rien n'a été créé : {erreur.Message}"
                : erreur.Message);
        }

        var creees = resultat.Creees!;
        await diffuseur.DiffuserAsync(new EvenementSynchro(
            ModulesSynchro.Taches,
            EvenementSynchro.GenreTachesCreees,
            EvenementSynchro.SourceMcp,
            createur.Id,
            createur.NomAffichage,
            creees.Count == 1 ? creees[0].Titre : null,
            creees.Count));
        return new { creees };
    }
}
