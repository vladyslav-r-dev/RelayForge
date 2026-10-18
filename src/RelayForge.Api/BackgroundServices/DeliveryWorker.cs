using RelayForge.Application.Interfaces;

namespace RelayForge.Api.BackgroundServices
{
    public class DeliveryWorker(ILogger<DeliveryWorker> logger,
    IHttpClientFactory httpClientFactory,
    IServiceScopeFactory serviceScopeFactory) : BackgroundService
    {
        readonly ILogger<DeliveryWorker> _logger = logger;
        readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {

                _logger.LogInformation("Starting delivery worker iteration");

                using var scope = _serviceScopeFactory.CreateScope();

                var deliveryRepository = scope.ServiceProvider
                    .GetRequiredService<IDeliveryRepository>();

                var eventRepository = scope.ServiceProvider
                .GetRequiredService<IWebhookEventRepository>();

                 var endpointRepository = scope.ServiceProvider
                .GetRequiredService<IEndpointRepository>();

                var pendingDeliveries = await deliveryRepository
                .GetPendingAsync(stoppingToken);

                foreach (var delivery in pendingDeliveries)
                {
                    var endpoint = await endpointRepository.GetByIdAsync(delivery.EndpointId, stoppingToken);

                    var webhookEvent = await eventRepository.GetByIdAsync(delivery.EventId, stoppingToken);


                    if (endpoint == null)
                    {
                        _logger.LogWarning(
                            "Endpoint with ID {EndpointId} not found for delivery {DeliveryId}",
                            delivery.EndpointId,
                            delivery.Id);
                        continue;
                    }
                    if (webhookEvent == null)
                    {
                        _logger.LogWarning(
                            "Webhook event with ID {WebhookEventId} not found for delivery {DeliveryId}",
                            delivery.EventId,
                            delivery.Id);
                        continue;
                    }

                    var unitOfWork = scope.ServiceProvider
                    .GetRequiredService<IUnitOfWork>();

                    delivery.RegisterAttempt();
                    await unitOfWork.SaveChangesAsync(stoppingToken);

                    using var client = _httpClientFactory.CreateClient();

                    client.Timeout = TimeSpan.FromSeconds(10);

                    try
                    {
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
                            _logger.LogInformation(
                                "Webhook delivery {DeliveryId} succeeded",
                                delivery.Id);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "Webhook request failed: {StatusCode}",
                                response.StatusCode);
                            delivery.RegisterFailure(1);
                            await unitOfWork.SaveChangesAsync(stoppingToken);
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        _logger.LogError(ex, "Error occurred while sending webhook request");
                        delivery.RegisterFailure(1);
                        await unitOfWork.SaveChangesAsync(stoppingToken);
                    }
                    catch (OperationCanceledException ex)
                    when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogWarning(
                        ex,
                        "Webhook delivery {DeliveryId} timed out",
                        delivery.Id);

                    delivery.RegisterFailure(1);
                    await unitOfWork.SaveChangesAsync(stoppingToken);
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
                    _logger.LogError(
                        ex,
                        "Unexpected error in DeliveryWorker");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }
}