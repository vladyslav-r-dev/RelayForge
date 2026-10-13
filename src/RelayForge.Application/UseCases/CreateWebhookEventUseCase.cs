using RelayForge.Application.Interfaces;
using RelayForge.Application.DTOs;
using RelayForge.Domain.Entities;

namespace RelayForge.Application.UseCases;

public class CreateWebhookEventUseCase(IWebhookEventRepository webhookEventRepository)
{
    private readonly IWebhookEventRepository _webhookEventRepository = webhookEventRepository;

    public async Task<Guid> ExecuteAsync(
    CreateWebhookEventRequest request,
    CancellationToken cancellationToken)
    {
        var webhookEvent = new WebhookEvent(request.EventType, request.Data);

        await _webhookEventRepository.AddAsync(webhookEvent, cancellationToken);

        return webhookEvent.Id;
    }
}