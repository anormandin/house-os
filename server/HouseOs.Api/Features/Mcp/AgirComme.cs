using HouseOs.Api.Domaine;
using HouseOs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol;

namespace HouseOs.Api.Features.Mcp;

/// <summary>
/// Résolution du paramètre agirComme (nom d'utilisateur → Utilisateur). La clé API étant
/// partagée, c'est ce paramètre qui dit au nom de qui une action est enregistrée.
/// </summary>
public static class AgirComme
{
    /// <summary>Cœur pur et testable : null si le nom est vide ou inconnu.</summary>
    public static Guid? Resoudre(string? agirComme, IEnumerable<(Guid Id, string NomUtilisateur)> utilisateurs)
    {
        if (string.IsNullOrWhiteSpace(agirComme))
        {
            return null;
        }
        var nom = agirComme.Trim().ToLowerInvariant();
        foreach (var utilisateur in utilisateurs)
        {
            if (utilisateur.NomUtilisateur == nom)
            {
                return utilisateur.Id;
            }
        }
        return null;
    }

    public static async Task<Utilisateur> ResoudreAsync(HouseOsDbContext db, string? agirComme)
    {
        var utilisateurs = await db.Utilisateurs.ToListAsync();
        var id = Resoudre(agirComme, utilisateurs.Select(u => (u.Id, u.NomUtilisateur)));
        if (id is null)
        {
            throw new McpException(
                $"Paramètre agirComme invalide ou manquant ({ValeursValides(utilisateurs)}).");
        }
        return utilisateurs.Single(u => u.Id == id);
    }

    public static string ValeursValides(IEnumerable<Utilisateur> utilisateurs) =>
        string.Join(" ou ", utilisateurs.OrderBy(u => u.NomUtilisateur).Select(u => $"'{u.NomUtilisateur}'"));
}
