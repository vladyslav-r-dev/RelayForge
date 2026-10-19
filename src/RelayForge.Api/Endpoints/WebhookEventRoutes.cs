using RelayForge.Application.DTOs;
using RelayForge.Application.UseCases;

namespace RelayForge.Api.Endpoints;

public static class WebhookEventRoutes
{
    public static IEndpointRouteBuilder MapWebhookEventRoutes(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/events", async (
            CreateWebhookEventRequest request,
            CreateWebhookEventUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var errors = new Dictionary<string, string[]>();

            if (string.IsNullOrWhiteSpace(request.EventType))
            {
                errors["EventType"] = ["EventType is required."];
            }
            else if (request.EventType.Length > 150)
            {
                errors["EventType"] = ["EventType cannot exceed 150 characters."];
            }

            if (string.IsNullOrWhiteSpace(request.Data))
            {
                errors["Data"] = ["Data is required."];
            }

            if (errors.Count > 0)
                return Results.ValidationProblem(errors);

            var id = await useCase.ExecuteAsync(request, cancellationToken);

            return Results.Created($"/events/{id}", new { Id = id });
        });

        endpoints.MapGet("/events/{id:guid}", async (
            Guid id,
            GetWebhookEventUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var webhookEvent = await useCase.ExecuteAsync(id, cancellationToken);

            return webhookEvent is null
                ? Results.NotFound()
                : Results.Ok(webhookEvent);
        });

        endpoints.MapGet("/events/{id:guid}/deliveries", async (
            Guid id,
            GetEventDeliveriesUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            var deliveries = await useCase.ExecuteAsync(id, cancellationToken);

            return Results.Ok(deliveries);
        });

        return endpoints;
    }
}
