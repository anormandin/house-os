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
