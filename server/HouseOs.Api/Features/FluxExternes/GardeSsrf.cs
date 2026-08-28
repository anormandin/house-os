using System.Net;
using System.Net.Sockets;

namespace HouseOs.Api.Features.FluxExternes;

/// <summary>
/// Garde SSRF du téléchargement ICS : les calendriers légitimes (Recollect, écoles,
/// villes) sont publics — toute URL dont l'hôte résout vers le réseau interne
/// (loopback, link-local, RFC1918, CGNAT/Tailscale, multicast) est refusée avec un
/// message clair. Les redirections sont suivies à la main (bornées) pour re-vérifier
/// chaque saut — l'auto-redirect du handler primaire est désactivé (Program.cs).
/// Risque résiduel accepté : un DNS qui répond différemment entre la vérification et
/// la connexion (rebinding) passe — hors de portée pour un serveur de maison.
/// </summary>
public class GardeSsrfHandler : DelegatingHandler
{
    public const int MaxRedirections = 3;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage requete, CancellationToken ct)
    {
        var uri = requete.RequestUri
            ?? throw new HttpRequestException("URL absente de la requête.");
        for (var saut = 0; ; saut++)
        {
            await VerifierUri(uri, ct);
            if (saut > 0)
            {
                // Une HttpRequestMessage ne se renvoie pas : nouvelle requête GET par saut.
                requete.Dispose();
                requete = new HttpRequestMessage(HttpMethod.Get, uri);
            }
            var reponse = await base.SendAsync(requete, ct);
            var location = (int)reponse.StatusCode is >= 300 and < 400
                ? reponse.Headers.Location
                : null;
            if (location is null)
            {
                return reponse;
            }
            reponse.Dispose();
            if (saut >= MaxRedirections)
            {
                throw new HttpRequestException($"Trop de redirections (max {MaxRedirections}).");
            }
            uri = location.IsAbsoluteUri ? location : new Uri(uri, location);
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                throw new HttpRequestException("Redirection vers un schéma non http(s).");
            }
        }
    }

    /// <summary>Refuse l'URL si son hôte (littéral ou résolu par DNS) est interne.</summary>
    public static async Task VerifierUri(Uri uri, CancellationToken ct)
    {
        var hote = uri.IdnHost;
        IPAddress[] adresses;
        if (IPAddress.TryParse(hote, out var litterale))
        {
            adresses = [litterale];
        }
        else
        {
            try
            {
                adresses = await Dns.GetHostAddressesAsync(hote, ct);
            }
            catch (SocketException)
            {
                throw new HttpRequestException($"Hôte introuvable : {hote}.");
            }
        }
        if (adresses.Any(EstAdresseInterne))
        {
            throw new HttpRequestException(
                $"URL refusée : « {hote} » pointe vers le réseau interne — " +
                "seuls les calendriers publics sont acceptés.");
        }
    }

    /// <summary>Loopback, privé (RFC1918), link-local, CGNAT (Tailscale), multicast,
    /// réservé — tout ce qu'un flux de calendrier public n'a aucune raison d'être.</summary>
    public static bool EstAdresseInterne(IPAddress adresse)
    {
        if (adresse.IsIPv4MappedToIPv6)
        {
            adresse = adresse.MapToIPv4();
        }
        if (IPAddress.IsLoopback(adresse))
        {
            return true;
        }
        if (adresse.AddressFamily == AddressFamily.InterNetwork)
        {
            var o = adresse.GetAddressBytes();
            return o[0] == 0                                    // 0.0.0.0/8
                || o[0] == 10                                   // 10.0.0.0/8
                || (o[0] == 100 && (o[1] & 0b1100_0000) == 64)  // 100.64.0.0/10 (CGNAT, Tailscale)
                || (o[0] == 169 && o[1] == 254)                 // 169.254.0.0/16 (link-local)
                || (o[0] == 172 && (o[1] & 0b1111_0000) == 16)  // 172.16.0.0/12
                || (o[0] == 192 && o[1] == 168)                 // 192.168.0.0/16
                || o[0] >= 224;                                 // multicast, réservé, broadcast
        }
        return adresse.IsIPv6LinkLocal
            || adresse.IsIPv6Multicast
            || adresse.IsIPv6UniqueLocal
            || adresse.Equals(IPAddress.IPv6Any);
    }
}
