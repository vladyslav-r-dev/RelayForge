using RelayForge.Application.Interfaces;
using RelayForge.Application.Responses;

namespace RelayForge.Application.UseCases;

public class GetWebhookEventUseCase(IWebhookEventRepository webhookEventRepository)
{
    private readonly IWebhookEventRepository _webhookEventRepository = webhookEventRepository;

    public async Task<WebhookEventResponse?> ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        var webhookEvent = await _webhookEventRepository.GetByIdAsync(id, cancellationToken);

        if (webhookEvent is null)
            return null;

        return new WebhookEventResponse
        {
            Id = webhookEvent.Id,
            EventType = webhookEvent.EventType,
            CreatedAt = webhookEvent.CreatedAt,
            Data = webhookEvent.Data
        };
    }
}
