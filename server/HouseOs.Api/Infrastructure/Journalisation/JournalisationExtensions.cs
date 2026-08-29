using System.Security.Claims;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace HouseOs.Api.Infrastructure.Journalisation;

/// <summary>
/// Câblage de la journalisation structurée : Serilog vers la console (dev,
/// <c>docker logs</c>) et vers Seq (recherche et corrélation).
///
/// Les niveaux vivent dans la section <c>Serilog</c> d'appsettings — se régler sans
/// recompiler compte, parce que l'incident qu'on chasse n'est pas reproductible à
/// volonté. Le <see cref="LoggingLevelSwitch"/> est exposé en DI pour qu'un futur
/// interrupteur puisse monter la verbosité à chaud ; il n'est jamais piloté par Seq
/// (voir le commentaire du sink).
/// </summary>
public static class JournalisationExtensions
{
    /// <summary>Au-delà, une requête est anormalement lente et mérite un Warning.</summary>
    private const int SeuilRequeteLenteMsDefaut = 1000;

    /// <summary>Fichiers servis à la pelle : jamais au-dessus de Verbose en succès.</summary>
    private static readonly string[] ExtensionsStatiques =
        [".js", ".css", ".map", ".png", ".jpg", ".jpeg", ".svg", ".ico", ".woff", ".woff2", ".webmanifest"];

    public static WebApplicationBuilder AjouterJournalisation(this WebApplicationBuilder builder)
    {
        var config = builder.Configuration;
        var interrupteur = new LoggingLevelSwitch(
            Enum.TryParse<LogEventLevel>(config["Serilog:MinimumLevel:Default"], out var niveau)
                ? niveau
                : LogEventLevel.Information);
        builder.Services.AddSingleton(interrupteur);

        builder.Services.AddSerilog((fournisseur, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(config)
                .ReadFrom.Services(fournisseur)
                .MinimumLevel.ControlledBy(interrupteur)
                .Enrich.FromLogContext()
                .Enrich.With<EnrichisseurTrace>()
                .Enrich.WithProperty("Application", "HouseOs");

            var urlSeq = config["Journalisation:Seq:Url"];
            if (string.IsNullOrWhiteSpace(urlSeq) == false)
            {
                // Volontairement SANS controlLevelSwitch : ce paramètre laisse Seq
                // imposer son propre niveau minimum au sink. Mesuré le 2026-08-29 —
                // les durées de phase en Debug (diffusion synchro, tier grossier)
                // sortaient bien en console mais n'atteignaient jamais Seq, alors
                // que ce sont exactement elles qu'on collecte. Le niveau se règle
                // dans la section Serilog d'appsettings, point.
                configuration.WriteTo.Seq(
                    urlSeq,
                    apiKey: Vide(config["Journalisation:Seq:CleApi"]),
                    // Tampon sur disque : Seq peut redémarrer sans qu'on perde
                    // justement les évènements de l'incident qu'on attendait.
                    bufferBaseFilename: Vide(config["Journalisation:Seq:CheminTampon"]));
            }
        });

        // ProblemDetails : toute erreur renvoyée au client porte l'identifiant de trace.
        builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = contexte =>
            contexte.ProblemDetails.Extensions["traceId"] = Trace.Identifiant(contexte.HttpContext));
        builder.Services.AddExceptionHandler<GestionnaireExceptions>();

        return builder;
    }

    /// <summary>
    /// À brancher juste après <c>UseForwardedHeaders</c> : l'IP et le schéma journalisés
    /// doivent être ceux du client, pas ceux du proxy.
    /// </summary>
    public static WebApplication UtiliserJournalisation(this WebApplication app)
    {
        var seuilLent = app.Configuration.GetValue(
            "Journalisation:SeuilRequeteLenteMs", SeuilRequeteLenteMsDefaut);

        // OnStarting plutôt qu'une écriture directe : UseExceptionHandler vide les
        // en-têtes quand il reprend la main, et l'identifiant doit survivre au 500 —
        // c'est le cas où on en a le plus besoin.
        app.Use(async (contexte, suivant) =>
        {
            contexte.Response.OnStarting(() =>
            {
                contexte.Response.Headers[Trace.EnTete] = Trace.Identifiant(contexte);
                return Task.CompletedTask;
            });
            await suivant();
        });

        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate =
                "{Methode} {Chemin} → {StatusCode} en {Elapsed:0.0} ms";
            options.GetLevel = (contexte, ecoule, exception) =>
                NiveauRequete(contexte.Request.Path, contexte.Response.StatusCode, ecoule, exception, seuilLent);
            options.EnrichDiagnosticContext = (diagnostic, contexte) =>
            {
                diagnostic.Set("Methode", contexte.Request.Method);
                diagnostic.Set("Chemin", contexte.Request.Path.Value);
                diagnostic.Set("Utilisateur", contexte.User.Identity?.IsAuthenticated == true
                    ? contexte.User.FindFirstValue(ClaimTypes.Name)
                    : null);
                diagnostic.Set("AdresseClient", contexte.Connection.RemoteIpAddress?.ToString());
                diagnostic.Set("AgentUtilisateur", contexte.Request.Headers.UserAgent.ToString());
                diagnostic.Set("Schema", contexte.Request.Scheme);
                // L'hôte tel que le client l'a écrit : distingue un accès direct, le
                // proxy Vite en dev et NPM en prod — trois chemins où le même geste
                // peut échouer différemment.
                diagnostic.Set("Hote", contexte.Request.Host.Value);
                // Le cœur de l'enquête 503 : distingue « le serveur n'a pas répondu »
                // de « le client est parti avant la réponse ».
                diagnostic.Set("Abandonnee", contexte.RequestAborted.IsCancellationRequested);
            };
        });

        app.UseExceptionHandler();

        return app;
    }

    /// <summary>
    /// Journal d'une tranche verticale, à capturer une fois dans le <c>MapX</c> et à
    /// laisser fermer les lambdas d'endpoint. Catégorie <c>HouseOs.&lt;Tranche&gt;</c> :
    /// dans Seq, isoler une tranche devient un filtre, et non une lecture au jugé.
    /// </summary>
    public static Microsoft.Extensions.Logging.ILogger JournalPour(
        this IEndpointRouteBuilder app, string tranche) =>
        app.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger($"HouseOs.{tranche}");

    /// <summary>
    /// Niveau d'une ligne de requête. Extrait pour être testable : c'est cette table
    /// qui décide si le healthcheck toutes les 30 s noie ou non les vrais évènements.
    /// </summary>
    internal static LogEventLevel NiveauRequete(
        PathString chemin, int statut, double ecouleMs, Exception? exception, int seuilLentMs)
    {
        if (exception is not null || statut >= StatusCodes.Status500InternalServerError)
        {
            return LogEventLevel.Error;
        }
        if (statut >= StatusCodes.Status400BadRequest)
        {
            return LogEventLevel.Warning;
        }
        if (EstBruitDeFond(chemin))
        {
            return LogEventLevel.Verbose;
        }
        // Une connexion WebSocket dure aussi longtemps que l'onglet reste ouvert :
        // la juger sur sa durée transformerait chaque session normale en alerte, et
        // noierait les vraies lenteurs qu'on cherche.
        if (statut == StatusCodes.Status101SwitchingProtocols)
        {
            return LogEventLevel.Information;
        }
        return ecouleMs > seuilLentMs ? LogEventLevel.Warning : LogEventLevel.Information;
    }

    private static bool EstBruitDeFond(PathString chemin)
    {
        var valeur = chemin.Value;
        if (string.IsNullOrEmpty(valeur))
        {
            return false;
        }
        // Le healthcheck du compose frappe /api/sante toutes les 30 s ; les assets
        // Vite partent par dizaines à chaque chargement.
        return valeur.StartsWith("/api/sante", StringComparison.OrdinalIgnoreCase)
            || valeur.StartsWith("/assets/", StringComparison.OrdinalIgnoreCase)
            || ExtensionsStatiques.Any(ext => valeur.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
    }

    private static string? Vide(string? valeur) =>
        string.IsNullOrWhiteSpace(valeur) ? null : valeur;
}
