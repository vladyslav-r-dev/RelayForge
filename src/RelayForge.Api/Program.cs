using RelayForge.Api.DependencyInjection;
using RelayForge.Api.Endpoints;
using RelayForge.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRelayForgeServices(builder.Configuration);

var app = builder.Build();
await app.ApplyRelayForgeMigrationsAsync();

app.UseRelayForgePipeline();
app.MapHealthRoutes();
app.MapEndpointRoutes();
app.MapWebhookEventRoutes();

app.Run();
