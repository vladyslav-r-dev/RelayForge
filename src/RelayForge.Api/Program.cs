using Microsoft.EntityFrameworkCore;
using RelayForge.Application.DTOs;
using RelayForge.Application.Interfaces;
using RelayForge.Application.UseCases;
using RelayForge.Infrastructure.Persistence;
using RelayForge.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDbContext<RelayForgeDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("RelayForge")));


builder.Services.AddScoped<IEndpointRepository, EndpointRepository>();

builder.Services.AddScoped<CreateEndpointUseCase>();
builder.Services.AddScoped<DeactivateEndpointUseCase>();
builder.Services.AddScoped<ActivateEndpointUseCase>();

var app = builder.Build();

app.MapPost("/endpoints", async (
    CreateEndpointRequest request,
    CreateEndpointUseCase useCase,
    CancellationToken cancellationToken) =>
{
    var id = await useCase.ExecuteAsync(request, cancellationToken);

    return Results.Created($"/endpoints/{id}", new { Id = id });
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
