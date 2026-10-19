using Microsoft.EntityFrameworkCore;
using RelayForge.Api.Middleware;
using RelayForge.Infrastructure.Persistence;

namespace RelayForge.Api.Extensions;

public static class WebApplicationExtensions
{
    public static async Task ApplyRelayForgeMigrationsAsync(this WebApplication app)
    {
        if (app.Configuration.GetValue<bool>("Database:ApplyMigrations"))
        {
            using var scope = app.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<RelayForgeDbContext>().Database.MigrateAsync();
        }
    }

    public static WebApplication UseRelayForgePipeline(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseMiddleware<ApiKeyMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();

            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/openapi/v1.json", "RelayForge API v1");
            });
        }

        return app;
    }
}
