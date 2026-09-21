using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace HouseOs.Api.Features.Mcp;

/// <summary>
/// Ce qui distingue deux clés partagées : où la clé se lit dans la configuration, et
/// quel appelant elle désigne. Le mécanisme, lui, est le même
/// (D-2026-08-24 Clé API Partagée Et AgirComme).
/// </summary>
public sealed class OptionsCleApi : AuthenticationSchemeOptions
{
    /// <summary>Chemin de la clé dans la configuration.</summary>
    public string CheminDeLaCle { get; set; } = "Mcp:Cle";

    /// <summary>Le client que la clé désigne, posé en claim pour la trace.</summary>
    public string Client { get; set; } = "mcp";
}

/// <summary>
/// Authentification par clé API partagée pour les appels machine : le endpoint /mcp
/// (config Mcp:Cle) et la poussée de flux externe (config FluxExternes:ClePoussee).
/// L'identité de la personne n'est pas portée par la clé : elle vient du paramètre
/// agirComme des outils. Clé absente de la config = tout est refusé (fail closed).
///
/// <para><b>Deux clés plutôt qu'une</b> (vault : D-2026-09-20 Flux Externe Poussé) : le
/// programme qui pousse des événements vit hors de la maison, et la clé du MCP ouvrirait
/// tout le reste. Le schéma est partagé, le secret ne l'est pas.</para>
/// </summary>
public sealed class AuthentificationCleApiHandler(
    IOptionsMonitor<OptionsCleApi> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration)
    : AuthenticationHandler<OptionsCleApi>(options, logger, encoder)
{
    public const string NomScheme = "CleApi";

    /// <summary>Le schéma de la poussée de flux externe — sa propre clé.</summary>
    public const string NomSchemePoussee = "ClePoussee";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var entete = Request.Headers.Authorization.ToString();
        const string prefixe = "Bearer ";
        if (entete.StartsWith(prefixe, StringComparison.OrdinalIgnoreCase) == false)
        {
            return Task.FromResult(AuthenticateResult.Fail("En-tête « Authorization: Bearer <clé> » requis."));
        }

        var fournie = entete[prefixe.Length..].Trim();
        if (ClesEgales(fournie, configuration[Options.CheminDeLaCle]) == false)
        {
            return Task.FromResult(AuthenticateResult.Fail("Clé API invalide."));
        }

        var identite = new ClaimsIdentity([new Claim("houseos:client", Options.Client)], Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identite), Scheme.Name)));
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
