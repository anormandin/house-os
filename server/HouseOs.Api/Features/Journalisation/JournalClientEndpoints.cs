using System.Security.Claims;
using HouseOs.Api.Infrastructure.Journalisation;
using Microsoft.Extensions.Logging;

namespace HouseOs.Api.Features.Journalisation;

/// <summary>Un évènement observé dans le navigateur, tel que la page l'envoie.</summary>
public record EvenementClient(
    string? Horodatage,
    string? Niveau,
    string? Message,
    string? Categorie,
    string? Url,
    string? TraceId,
    Dictionary<string, string>? Proprietes);

/// <param name="Origine">
/// L'origine de la page (<c>location.origin</c>). Fait de session, pas d'évènement :
/// c'est elle qui dit si l'onglet parle au proxy Vite, à Kestrel en direct ou à la
/// prod — trois chemins réseau différents pour un même symptôme.
/// </param>
public record LotJournalClient(string? SessionId, string? Origine, EvenementClient[]? Evenements);

/// <summary>
/// Réception de la piste de session du navigateur. Le web était jusqu'ici totalement
/// muet : ni console, ni error boundary, et l'échec de connexion au hub avalé par un
/// <c>catch(() =&gt; {})</c>. Sans ce point d'entrée, « rien ne s'est passé à l'écran »
/// reste un récit invérifiable.
///
/// Les évènements ne sont jamais persistés en base : ils sont ré-émis dans le pipeline
/// Serilog et vivent dans Seq, avec le même <c>TraceId</c> que la requête serveur
/// correspondante.
/// </summary>
public static class JournalClientEndpoints
{
    /// <summary>Politique de rate limiting du journal client (fenêtre fixe par IP).</summary>
    public const string PolitiqueLimite = "journal-client";

    internal const int MaxEvenementsParLot = 50;
    internal const int MaxLongueurMessage = 1000;
    internal const int MaxProprietes = 20;
    internal const int MaxLongueurValeur = 500;
    internal const long TailleMaxCorps = 64 * 1024;

    /// <summary>
    /// Liste blanche : l'entrée n'est pas authentifiée sur l'écran de connexion, on ne
    /// laisse donc pas le navigateur choisir un niveau arbitraire.
    /// </summary>
    private static readonly Dictionary<string, LogLevel> NiveauxPermis = new(StringComparer.OrdinalIgnoreCase)
    {
        ["debug"] = LogLevel.Debug,
        ["info"] = LogLevel.Information,
        ["warn"] = LogLevel.Warning,
        ["error"] = LogLevel.Error,
    };

    public static IEndpointRouteBuilder MapJournalClient(this IEndpointRouteBuilder app)
    {
        var fabrique = app.ServiceProvider.GetRequiredService<ILoggerFactory>();

        app.MapPost("/api/journal-client", async (ClaimsPrincipal principal, HttpContext http) =>
        {
            // Corps borné AVANT lecture : ce endpoint est le seul ouvert sans cookie,
            // il ne doit pas pouvoir servir à remplir le disque. La liaison automatique
            // aurait déjà tout lu — d'où la désérialisation à la main.
            if (http.Request.ContentLength is > TailleMaxCorps)
            {
                return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
            }

            LotJournalClient? lot;
            try
            {
                lot = await http.Request.ReadFromJsonAsync<LotJournalClient>();
            }
            catch (System.Text.Json.JsonException)
            {
                return Results.BadRequest();
            }
            if (lot is null)
            {
                return Results.BadRequest();
            }

            var evenements = lot.Evenements ?? [];
            if (evenements.Length > MaxEvenementsParLot)
            {
                evenements = evenements[..MaxEvenementsParLot];
            }

            // Le nom de l'auteur vient du cookie, jamais du corps : un lot anonyme
            // (écran de connexion) reste anonyme, il ne peut pas s'attribuer une session.
            var utilisateur = principal.Identity?.IsAuthenticated == true
                ? principal.FindFirstValue(ClaimTypes.Name)
                : null;
            var session = Tronquer(lot.SessionId, 64);
            var origine = Tronquer(lot.Origine, 200);

            foreach (var evenement in evenements)
            {
                var journal = fabrique.CreateLogger($"HouseOs.Client.{Categorie(evenement.Categorie)}");
                var niveau = NiveauxPermis.TryGetValue(evenement.Niveau ?? "", out var n)
                    ? n
                    : LogLevel.Information;

                // Le message du navigateur est une VALEUR structurée, jamais un gabarit :
                // sans ça, un message contenant des accolades permettrait de forger des
                // propriétés dans Seq.
                using var portee = journal.BeginScope(Proprietes(evenement));
                journal.Log(
                    niveau,
                    "Client — {MessageClient} (session {SessionClient}, {UtilisateurClient} @ {OrigineClient}{UrlClient}).",
                    Tronquer(evenement.Message, MaxLongueurMessage) ?? "(vide)",
                    session,
                    utilisateur,
                    origine,
                    Tronquer(evenement.Url, 500));
            }

            return Results.Accepted();
        })
        .AllowAnonymous()
        .RequireRateLimiting(PolitiqueLimite);

        return app;
    }

    /// <summary>
    /// Les propriétés du navigateur, bornées en nombre et en longueur. Le TraceId,
    /// lui, écrase l'enrichisseur serveur : c'est le lien vers la requête fautive.
    /// </summary>
    private static Dictionary<string, object?> Proprietes(EvenementClient evenement)
    {
        var portee = new Dictionary<string, object?> { ["Source"] = "Client" };
        if (string.IsNullOrWhiteSpace(evenement.TraceId) == false)
        {
            portee[EnrichisseurTrace.ProprieteTrace] = Tronquer(evenement.TraceId, 64);
        }
        if (string.IsNullOrWhiteSpace(evenement.Horodatage) == false)
        {
            portee["HorodatageClient"] = Tronquer(evenement.Horodatage, 40);
        }
        foreach (var (cle, valeur) in (evenement.Proprietes ?? []).Take(MaxProprietes))
        {
            portee["Client" + Tronquer(cle, 40)] = Tronquer(valeur, MaxLongueurValeur);
        }
        return portee;
    }

    /// <summary>Catégorie du navigateur, réduite à un identifiant sûr pour Serilog.</summary>
    private static string Categorie(string? brute)
    {
        var nettoyee = new string((brute ?? "").Where(char.IsLetterOrDigit).Take(30).ToArray());
        return nettoyee.Length == 0 ? "Divers" : nettoyee;
    }

    private static string? Tronquer(string? valeur, int max) =>
        valeur is null ? null : valeur.Length <= max ? valeur : valeur[..max];
}
