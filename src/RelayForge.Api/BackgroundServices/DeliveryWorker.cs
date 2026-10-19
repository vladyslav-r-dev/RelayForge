using RelayForge.Application.Interfaces;

namespace RelayForge.Api.BackgroundServices;

public sealed class DeliveryWorker(
    ILogger<DeliveryWorker> logger,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IDeliveryRepository>();
                var processor = scope.ServiceProvider.GetRequiredService<DeliveryProcessor>();
                var deliveries = await repository.GetPendingAsync(stoppingToken);
                foreach (var delivery in deliveries)
                    await processor.ProcessAsync(delivery, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError("Delivery batch aborted ({ErrorType}); retrying with a new scope", exception.GetType().Name);
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
