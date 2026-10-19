using RelayForge.Domain.Entities;

namespace RelayForge.Application.Interfaces;

public interface IWebhookEventRepository
{
    void Add(WebhookEvent webhookEvent);
    Task<WebhookEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
