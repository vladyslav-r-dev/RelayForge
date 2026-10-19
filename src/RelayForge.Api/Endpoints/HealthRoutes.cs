using Microsoft.EntityFrameworkCore;
using RelayForge.Infrastructure.Persistence;

namespace RelayForge.Api.Endpoints;

public static class HealthRoutes
{
    public static IEndpointRouteBuilder MapHealthRoutes(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health", async (RelayForgeDbContext db, CancellationToken cancellationToken) =>
            await db.Database.CanConnectAsync(cancellationToken)
                ? Results.Ok(new { status = "healthy" })
                : Results.StatusCode(StatusCodes.Status503ServiceUnavailable));

        return endpoints;
    }
}
