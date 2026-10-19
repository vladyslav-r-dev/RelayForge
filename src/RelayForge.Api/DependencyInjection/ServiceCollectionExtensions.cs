using Microsoft.EntityFrameworkCore;
using RelayForge.Api.BackgroundServices;
using RelayForge.Api.Exceptions;
using RelayForge.Application.Interfaces;
using RelayForge.Application.Security;
using RelayForge.Application.UseCases;
using RelayForge.Infrastructure;
using RelayForge.Infrastructure.Persistence;
using RelayForge.Infrastructure.Repositories;

namespace RelayForge.Api.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRelayForgeServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOpenApi();
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        if (string.IsNullOrWhiteSpace(configuration["Security:ApiKey"]))
            throw new InvalidOperationException("Security:ApiKey must be configured.");
        if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("RelayForge")))
            throw new InvalidOperationException("ConnectionStrings:RelayForge must be configured.");

        services.AddSingleton(new TrustedReceivers(
            configuration.GetSection("Delivery:AllowedReceiverUrls").Get<string[]>() ?? []));
        services.AddHttpClient(DeliveryProcessor.ClientName, client =>
            client.Timeout = TimeSpan.FromSeconds(10))
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                UseCookies = false,
                UseProxy = false
            })
            .RemoveAllLoggers();

        services.AddDbContext<RelayForgeDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("RelayForge")));

        services.AddScoped<IEndpointRepository, EndpointRepository>();
        services.AddScoped<IWebhookEventRepository, WebhookEventRepository>();
        services.AddScoped<IDeliveryRepository, DeliveryRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<CreateEndpointUseCase>();
        services.AddScoped<GetEndpointUseCase>();
        services.AddScoped<ActivateEndpointUseCase>();
        services.AddScoped<DeactivateEndpointUseCase>();
        services.AddScoped<CreateWebhookEventUseCase>();
        services.AddScoped<GetWebhookEventUseCase>();
        services.AddScoped<GetEventDeliveriesUseCase>();
        services.AddHostedService<DeliveryWorker>();
        services.AddScoped<DeliveryProcessor>();

        return services;
    }
}
