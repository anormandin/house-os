using HouseOs.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HouseOs.Api.Features.Sante;

public static class SanteEndpoint
{
    public static IEndpointRouteBuilder MapSante(this IEndpointRouteBuilder app)
    {
        // Sonde réelle : « ok » sans toucher la DB masquerait une base morte au
        // healthcheck compose. Timeout court — une sonde qui pend est pire qu'une
        // sonde rouge.
        app.MapGet("/api/sante", async (HouseOsDbContext db, CancellationToken ct) =>
        {
            using var delai = CancellationTokenSource.CreateLinkedTokenSource(ct);
            delai.CancelAfter(TimeSpan.FromSeconds(3));
            try
            {
                await db.Database.ExecuteSqlAsync($"SELECT 1", delai.Token);
                return Results.Ok(new { statut = "ok", db = "ok" });
            }
            catch (Exception) when (ct.IsCancellationRequested == false)
            {
                return Results.Json(new { statut = "degrade", db = "inaccessible" },
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        }).AllowAnonymous();
        return app;
    }
}
