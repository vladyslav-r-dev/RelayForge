using RelayForge.Domain.Entities;
using RelayForge.Domain.Enums;
using RelayForge.Application.Interfaces;
using RelayForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace RelayForge.Infrastructure.Repositories
{
public class DeliveryRepository(RelayForgeDbContext context) : IDeliveryRepository
{
    private readonly RelayForgeDbContext _context = context;

    public void Add(IEnumerable<Delivery> deliveries, CancellationToken cancellationToken)
    {
        _context.AddRange(deliveries);
    }

    public async Task<IEnumerable<Delivery>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken)
    {
        return await _context.Deliveries
            .Where(d => d.EventId == eventId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Delivery>> GetPendingAsync(CancellationToken cancellationToken)
    {
        return await _context.Deliveries
            .Where(d =>
            d.Status == DeliveryStatus.Pending &&
            (d.NextAttemptAt == null ||
            d.NextAttemptAt <= DateTimeOffset.UtcNow))
            .OrderBy(d => d.CreatedAt)
            .Take(10)
            .ToListAsync(cancellationToken);
    }
}
}