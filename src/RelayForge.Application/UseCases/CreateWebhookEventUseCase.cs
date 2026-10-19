using Microsoft.Extensions.Logging;
using RelayForge.Application.DTOs;
using RelayForge.Application.Interfaces;
using RelayForge.Domain.Entities;

namespace RelayForge.Application.UseCases;

public class CreateWebhookEventUseCase(
    IWebhookEventRepository webhookEventRepository,
    IDeliveryRepository deliveryRepository,
    IEndpointRepository endpointRepository,
    IUnitOfWork unitOfWork,
    ILogger<CreateWebhookEventUseCase> logger)
{
    private readonly IWebhookEventRepository _webhookEventRepository = webhookEventRepository;
    private readonly IDeliveryRepository _deliveryRepository = deliveryRepository;
    private readonly IEndpointRepository _endpointRepository = endpointRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<CreateWebhookEventUseCase> _logger = logger;

    public async Task<Guid> ExecuteAsync(
        CreateWebhookEventRequest request,
        CancellationToken cancellationToken)
    {
        var webhookEvent = new WebhookEvent(request.EventType, request.Data);

        var activeEndpoints = await _endpointRepository
            .GetActiveAsync(cancellationToken);

        var deliveries = new List<Delivery>();

        foreach (var endpoint in activeEndpoints)
        {
            deliveries.Add(new Delivery(webhookEvent.Id, endpoint.Id));
        }

        _webhookEventRepository.Add(webhookEvent);
        _deliveryRepository.Add(deliveries, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Webhook event {EventId} created",
            webhookEvent.Id);

        return webhookEvent.Id;
    }
}
