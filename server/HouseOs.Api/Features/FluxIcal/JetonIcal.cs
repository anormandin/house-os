namespace HouseOs.Api.Features.FluxIcal;

/// <summary>
/// Le jeton est la seule authentification du flux (les apps calendrier ne savent
/// pas envoyer de cookie) : 192 bits de CSPRNG, régénérable par rotation.
/// </summary>
public static class JetonIcal
{
    public static string Generer() =>
        Convert.ToHexStringLower(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24));

    /// <summary>Chemin + URL publique (Funnel) si configurée — null sinon, l'UI n'affiche alors que l'interne.</summary>
    public static object ReponseFlux(string jeton, IConfiguration config)
    {
        var chemin = $"/ical/{jeton}.ics";
        var basePublique = config["Ical:UrlPubliqueBase"];
        return new
        {
            chemin,
            urlPublique = string.IsNullOrEmpty(basePublique)
                ? null
                : $"{basePublique.TrimEnd('/')}{chemin}",
        };
    }
}
