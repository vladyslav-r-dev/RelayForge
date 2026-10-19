using Microsoft.EntityFrameworkCore;
using RelayForge.Application.Interfaces;
using RelayForge.Domain.Entities;
using RelayForge.Infrastructure.Persistence;

namespace RelayForge.Infrastructure.Repositories;

public class WebhookEventRepository(RelayForgeDbContext context)
    : IWebhookEventRepository
{
    private readonly RelayForgeDbContext _context = context;

    public void Add(WebhookEvent webhookEvent)
    {
        _context.WebhookEvents.Add(webhookEvent);
    }

    public async Task<WebhookEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.WebhookEvents.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }
}
