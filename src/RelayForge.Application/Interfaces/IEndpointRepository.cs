using RelayForge.Domain.Entities;

namespace RelayForge.Application.Interfaces;
public interface IEndpointRepository
{
    Task AddAsync(Endpoint endpoint, CancellationToken cancellationToken);
    Task<Endpoint?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task UpdateAsync(Endpoint endpoint, CancellationToken cancellationToken);
}