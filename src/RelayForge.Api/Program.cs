
using Microsoft.EntityFrameworkCore;
using RelayForge.Api.BackgroundServices;
using RelayForge.Api.Exceptions;
using RelayForge.Api.Middleware;
using RelayForge.Application.DTOs;
using RelayForge.Application.Interfaces;
using RelayForge.Application.UseCases;
using RelayForge.Infrastructure;
using RelayForge.Infrastructure.Persistence;
using RelayForge.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHttpClient();

builder.Services.AddDbContext<RelayForgeDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("RelayForge")));

builder.Services.AddScoped<IEndpointRepository, EndpointRepository>();
builder.Services.AddScoped<IWebhookEventRepository, WebhookEventRepository>();
builder.Services.AddScoped<IDeliveryRepository, DeliveryRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddScoped<CreateEndpointUseCase>();
builder.Services.AddScoped<GetEndpointUseCase>();
builder.Services.AddScoped<ActivateEndpointUseCase>();
builder.Services.AddScoped<DeactivateEndpointUseCase>();
builder.Services.AddScoped<CreateWebhookEventUseCase>();
builder.Services.AddScoped<GetWebhookEventUseCase>();
builder.Services.AddScoped<GetEventDeliveriesUseCase>();
builder.Services.AddHostedService<DeliveryWorker>();

var app = builder.Build();

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

app.UseHttpsRedirection();

app.MapPost("/endpoints", async (
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
             (uri.Scheme != Uri.UriSchemeHttp &&
              uri.Scheme != Uri.UriSchemeHttps))
    {
        errors["Url"] = ["URL must be a valid HTTP or HTTPS address."];
    }

    if (errors.Count > 0)
        return Results.ValidationProblem(errors);

    var id = await useCase.ExecuteAsync(request, cancellationToken);

    return Results.Created($"/endpoints/{id}", new { Id = id });
});

app.MapGet("/endpoints/{id:guid}", async (
    Guid id,
    GetEndpointUseCase useCase,
    CancellationToken cancellationToken) =>
{
    var endpoint = await useCase.ExecuteAsync(id, cancellationToken);

    return endpoint is null
        ? Results.NotFound()
        : Results.Ok(endpoint);
});

app.MapPatch("/endpoints/{id:guid}/activate", async (
    Guid id,
    ActivateEndpointUseCase useCase,
    CancellationToken cancellationToken) =>
{
    await useCase.ExecuteAsync(id, cancellationToken);

    return Results.NoContent();
});

app.MapPatch("/endpoints/{id:guid}/deactivate", async (
    Guid id,
    DeactivateEndpointUseCase useCase,
    CancellationToken cancellationToken) =>
{
    await useCase.ExecuteAsync(id, cancellationToken);

    return Results.NoContent();
});

app.MapPost("/events", async (
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

app.MapGet("/events/{id:guid}", async (
    Guid id,
    GetWebhookEventUseCase useCase,
    CancellationToken cancellationToken) =>
{
    var webhookEvent = await useCase.ExecuteAsync(id, cancellationToken);

    return webhookEvent is null
        ? Results.NotFound()
        : Results.Ok(webhookEvent);
});

app.MapGet("/events/{id:guid}/deliveries", async (
    Guid id,
    GetEventDeliveriesUseCase useCase,
    CancellationToken cancellationToken) =>
{
    var deliveries = await useCase.ExecuteAsync(id, cancellationToken);

    return deliveries is null
        ? Results.NotFound()
        : Results.Ok(deliveries);
});

app.Run();
