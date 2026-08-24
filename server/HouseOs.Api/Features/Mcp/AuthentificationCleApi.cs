using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace HouseOs.Api.Features.Mcp;

/// <summary>
/// Authentification par clé API partagée (config Mcp:Cle) pour le endpoint /mcp.
/// L'identité de la personne n'est pas portée par la clé : elle vient du paramètre
/// agirComme des outils. Clé absente de la config = tout est refusé (fail closed).
/// </summary>
public sealed class AuthentificationCleApiHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string NomScheme = "CleApi";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var entete = Request.Headers.Authorization.ToString();
        const string prefixe = "Bearer ";
        if (entete.StartsWith(prefixe, StringComparison.OrdinalIgnoreCase) == false)
        {
            return Task.FromResult(AuthenticateResult.Fail("En-tête « Authorization: Bearer <clé> » requis."));
        }

        var fournie = entete[prefixe.Length..].Trim();
        if (ClesEgales(fournie, configuration["Mcp:Cle"]) == false)
        {
            return Task.FromResult(AuthenticateResult.Fail("Clé API invalide."));
        }

        var identite = new ClaimsIdentity([new Claim("houseos:client", "mcp")], NomScheme);
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identite), NomScheme)));
    }

    /// <summary>Comparaison à temps constant ; clé configurée vide/absente → toujours faux.</summary>
    public static bool ClesEgales(string? fournie, string? configuree)
    {
        if (string.IsNullOrEmpty(fournie) || string.IsNullOrEmpty(configuree))
        {
            return false;
        }
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(fournie), Encoding.UTF8.GetBytes(configuree));
    }
}
