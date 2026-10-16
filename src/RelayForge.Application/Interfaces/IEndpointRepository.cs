using RelayForge.Domain.Entities;

namespace RelayForge.Application.Interfaces;
public interface IEndpointRepository
{
    void Add(Endpoint endpoint, CancellationToken cancellationToken);
    Task<Endpoint?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IEnumerable<Endpoint>> GetActiveAsync(CancellationToken cancellationToken);
}