namespace HouseOs.Api.Features.Sante;

public static class SanteEndpoint
{
    public static IEndpointRouteBuilder MapSante(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/sante", () => Results.Ok(new { statut = "ok" }));
        return app;
    }
}
