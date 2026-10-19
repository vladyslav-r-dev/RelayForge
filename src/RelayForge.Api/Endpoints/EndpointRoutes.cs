using RelayForge.Application.DTOs;
using RelayForge.Application.UseCases;

namespace RelayForge.Api.Endpoints;

public static class EndpointRoutes
{
    public static IEndpointRouteBuilder MapEndpointRoutes(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/endpoints", async (
            CreateEndpointRequest request,
            CreateEndpointUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var errors = new Dictionary<string, string[]>();

            if (string.IsNullOrWhiteSpace(request.Url))
            {
                errors["Url"] = ["URL is required."];
            }
            else if (request.Url.Length > 2048)
            {
                errors["Url"] = ["URL cannot exceed 2048 characters."];
            }
            else if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri) ||
                     uri.Scheme != Uri.UriSchemeHttps)
            {
                errors["Url"] = ["URL must be a valid HTTPS address."];
            }

            if (errors.Count > 0)
                return Results.ValidationProblem(errors);

            var id = await useCase.ExecuteAsync(request, cancellationToken);

            return Results.Created($"/endpoints/{id}", new { Id = id });
        });

        endpoints.MapGet("/endpoints/{id:guid}", async (
            Guid id,
            GetEndpointUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var endpoint = await useCase.ExecuteAsync(id, cancellationToken);

            return Results.Ok(endpoint);
        });

        endpoints.MapPatch("/endpoints/{id:guid}/activate", async (
            Guid id,
            ActivateEndpointUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            await useCase.ExecuteAsync(id, cancellationToken);

            return Results.NoContent();
        });

        endpoints.MapPatch("/endpoints/{id:guid}/deactivate", async (
            Guid id,
            DeactivateEndpointUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            await useCase.ExecuteAsync(id, cancellationToken);

            return Results.NoContent();
        });

        return endpoints;
    }
}
