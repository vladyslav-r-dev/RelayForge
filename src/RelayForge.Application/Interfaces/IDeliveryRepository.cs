using RelayForge.Domain.Entities;

namespace RelayForge.Application.Interfaces;

public interface IDeliveryRepository
{
    void Add(IEnumerable<Delivery> deliveries, CancellationToken cancellationToken);
    Task<IEnumerable<Delivery>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken);
    Task<IEnumerable<Delivery>> GetPendingAsync(CancellationToken cancellationToken);
}