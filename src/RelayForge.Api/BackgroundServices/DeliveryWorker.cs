
using System.Net;
using System.Net.Http.Json;
using RelayForge.Application.Interfaces;
using RelayForge.Domain.Entities;
using RelayForge.Domain.Enums;

namespace RelayForge.Api.BackgroundServices;

public class DeliveryWorker(
    ILogger<DeliveryWorker> logger,
    IHttpClientFactory httpClientFactory,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    private const int MaxAttempts = 3;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();

                var deliveryRepository = scope.ServiceProvider
                    .GetRequiredService<IDeliveryRepository>();

                var eventRepository = scope.ServiceProvider
                    .GetRequiredService<IWebhookEventRepository>();

                var endpointRepository = scope.ServiceProvider
                    .GetRequiredService<IEndpointRepository>();

                var unitOfWork = scope.ServiceProvider
                    .GetRequiredService<IUnitOfWork>();

                var deliveries = await deliveryRepository
                    .GetPendingAsync(stoppingToken);

                foreach (var delivery in deliveries)
                {
                    var endpoint = await endpointRepository
                        .GetByIdAsync(delivery.EndpointId, stoppingToken);

                    var webhookEvent = await eventRepository
                        .GetByIdAsync(delivery.EventId, stoppingToken);

                    if (endpoint is null || webhookEvent is null)
                    {
                        logger.LogWarning(
                            "Missing event or endpoint for delivery {DeliveryId}",
                            delivery.Id);

                        continue;
                    }

                    delivery.RegisterAttempt();
                    await unitOfWork.SaveChangesAsync(stoppingToken);

                    try
                    {
                        using var client = httpClientFactory.CreateClient();
                        client.Timeout = TimeSpan.FromSeconds(10);

                        using var response = await client.PostAsJsonAsync(
                            endpoint.Url,
                            new
                            {
                                eventId = webhookEvent.Id,
                                eventType = webhookEvent.EventType,
                                data = webhookEvent.Data
                            },
                            stoppingToken);

                        if (response.IsSuccessStatusCode)
                        {
                            delivery.MarkAsDelivered();

                            await unitOfWork.SaveChangesAsync(stoppingToken);

                            logger.LogInformation(
                                "Delivery {DeliveryId} succeeded on attempt {Attempt}",
                                delivery.Id,
                                delivery.AttemptCount);

                            continue;
                        }

                        bool canRetry =
                            response.StatusCode == HttpStatusCode.TooManyRequests ||
                            (int)response.StatusCode >= 500;

                        logger.LogWarning(
                            "Delivery {DeliveryId} received HTTP {StatusCode}",
                            delivery.Id,
                            (int)response.StatusCode);

                        await HandleFailureAsync(
                            delivery,
                            unitOfWork,
                            canRetry ? MaxAttempts : 1,
                            stoppingToken);
                    }
                    catch (HttpRequestException ex)
                    {
                        logger.LogWarning(
                            ex,
                            "Network error for delivery {DeliveryId}",
                            delivery.Id);

                        await HandleFailureAsync(
                            delivery,
                            unitOfWork,
                            MaxAttempts,
                            stoppingToken);
                    }
                    catch (OperationCanceledException ex)
                        when (!stoppingToken.IsCancellationRequested)
                    {
                        logger.LogWarning(
                            ex,
                            "Delivery {DeliveryId} timed out",
                            delivery.Id);

                        await HandleFailureAsync(
                            delivery,
                            unitOfWork,
                            MaxAttempts,
                            stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Unexpected error in DeliveryWorker");
            }

            try
            {
                await Task.Delay(PollingInterval, stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private static async Task HandleFailureAsync(
        Delivery delivery,
        IUnitOfWork unitOfWork,
        int maxAttempts,
        CancellationToken cancellationToken)
    {
        delivery.RegisterFailure(maxAttempts);

        if (delivery.Status == DeliveryStatus.Pending)
        {
            double seconds = 5 * Math.Pow(
                2,
                delivery.AttemptCount - 1);

            delivery.ScheduleNextAttempt(
                TimeSpan.FromSeconds(seconds));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
