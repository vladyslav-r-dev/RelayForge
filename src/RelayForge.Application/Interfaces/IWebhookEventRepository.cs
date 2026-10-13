using RelayForge.Domain.Entities;

namespace RelayForge.Application.Interfaces;

public interface IWebhookEventRepository
{
    Task AddAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken);
}