using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.AspNetCore;

namespace HouseOs.Api.Features.Mcp;

public static class McpEndpoints
{
    /// <summary>Policy d'autorisation du endpoint /mcp (scheme CleApi, remplace la FallbackPolicy).</summary>
    public const string PolicyCleApi = "McpCle";

    public static IServiceCollection AjouterMcp(this IServiceCollection services)
    {
        services.AddMcpServer()
            .WithHttpTransport(options => options.SessionMode = HttpServerSessionMode.Stateless)
            // Un filtre unique plutôt qu'un enrobage par outil : les ~18 outils MCP
            // écrivent dans la même base que l'UI, et jusqu'ici une panne d'outil
            // remontait à l'agent en McpException sans laisser la moindre trace ici.
            .WithRequestFilters(filtres => filtres.AddCallToolFilter(suivant => async (contexte, annulation) =>
            {
                var journal = contexte.Services?.GetService<ILoggerFactory>()?.CreateLogger("HouseOs.Mcp")
                    ?? (ILogger)NullLogger.Instance;
                var outil = contexte.Params?.Name;
                // Les noms d'arguments seulement : de quoi rejouer l'appel sans
                // déverser le contenu (notes, montants) dans le journal.
                var arguments = contexte.Params?.Arguments?.Keys.ToArray() ?? [];
                var chrono = Stopwatch.StartNew();
                try
                {
                    var resultat = await suivant(contexte, annulation);
                    journal.LogInformation(
                        "Outil MCP {Outil}({Arguments}) → {Issue} en {DureeMs} ms.",
                        outil, arguments, resultat.IsError == true ? "erreur" : "ok",
                        chrono.ElapsedMilliseconds);
                    return resultat;
                }
                catch (Exception ex)
                {
                    journal.LogWarning(
                        ex, "Outil MCP {Outil}({Arguments}) a échoué après {DureeMs} ms.",
                        outil, arguments, chrono.ElapsedMilliseconds);
                    throw;
                }
            }))
            .WithTools([typeof(OutilsTaches), typeof(OutilsMaison), typeof(OutilsIcal)]);
        return services;
    }

    public static WebApplication MapMcpHouseOs(this WebApplication app)
    {
        if (string.IsNullOrEmpty(app.Configuration["Mcp:Cle"]))
        {
            app.Logger.LogWarning("Mcp:Cle non configurée — le endpoint /mcp refusera toutes les requêtes.");
        }
        app.MapMcp("/mcp").RequireAuthorization(PolicyCleApi);
        return app;
    }
}
