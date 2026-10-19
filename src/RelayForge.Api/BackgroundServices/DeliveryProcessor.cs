using System.Net;
using System.Net.Http.Json;
using RelayForge.Application.Interfaces;
using RelayForge.Application.Security;
using RelayForge.Domain.Entities;
using RelayForge.Domain.Enums;

namespace RelayForge.Api.BackgroundServices;

public sealed class DeliveryProcessor(
    IEndpointRepository endpoints,
    IWebhookEventRepository events,
    IUnitOfWork unitOfWork,
    IHttpClientFactory clients,
    TrustedReceivers trustedReceivers,
    ILogger<DeliveryProcessor> logger)
{
    public const string ClientName = "WebhookDelivery";

    public async Task ProcessAsync(Delivery delivery, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (delivery.Status != DeliveryStatus.Pending)
            return;

        if (delivery.AttemptCount >= Delivery.MaxAttempts)
        {
            delivery.MarkAsFailed();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        var endpoint = await endpoints.GetByIdAsync(delivery.EndpointId, cancellationToken);
        var webhookEvent = await events.GetByIdAsync(delivery.EventId, cancellationToken);
        if (endpoint is null || webhookEvent is null || !endpoint.IsActive ||
            !trustedReceivers.IsAllowed(endpoint.Url))
        {
            delivery.MarkAsFailed();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogWarning("Delivery {DeliveryId} has a missing or disabled receiver/event", delivery.Id);
            return;
        }

        delivery.RegisterAttempt();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        bool delivered = false;
        bool retry = true;
        TimeSpan? retryAfter = null;
        try
        {
            using var client = clients.CreateClient(ClientName);
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint.Url)
            {
                Content = JsonContent.Create(new
                {
                    eventId = webhookEvent.Id,
                    eventType = webhookEvent.EventType,
                    data = webhookEvent.Data
                })
            };
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            delivered = response.IsSuccessStatusCode;
            retry = response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500;
            if (retry && response.Headers.RetryAfter is { } header)
                retryAfter = header.Delta ?? (header.Date - DateTimeOffset.UtcNow);

            logger.LogInformation("Delivery {DeliveryId}, attempt {Attempt}: HTTP {StatusCode}",
                delivery.Id, delivery.AttemptCount, (int)response.StatusCode);
        }
        catch (HttpRequestException)
        {
            logger.LogWarning("Network failure for delivery {DeliveryId}", delivery.Id);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Timeout for delivery {DeliveryId}", delivery.Id);
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (delivered)
            delivery.MarkAsDelivered();
        else
        {
            delivery.RegisterFailure(retry ? Delivery.MaxAttempts : 1);
            if (delivery.Status == DeliveryStatus.Pending)
            {
                var delay = delivery.GetRetryDelay();
                if (retryAfter > delay)
                    delay = retryAfter.Value > TimeSpan.FromDays(1) ? TimeSpan.FromDays(1) : retryAfter.Value;
                delivery.ScheduleNextAttempt(delay);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
