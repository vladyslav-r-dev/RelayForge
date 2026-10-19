using Microsoft.EntityFrameworkCore;
using RelayForge.Application.Interfaces;
using RelayForge.Domain.Entities;
using RelayForge.Infrastructure.Persistence;

namespace RelayForge.Infrastructure.Repositories;

public class EndpointRepository(RelayForgeDbContext context) : IEndpointRepository
{
    private readonly RelayForgeDbContext _context = context;

    public void Add(Endpoint endpoint, CancellationToken cancellationToken)
    {
        _context.Endpoints.Add(endpoint);
    }

    public Task<Endpoint?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _context.Endpoints.FindAsync(new object[] { id }, cancellationToken).AsTask();
    }

    public async Task<IEnumerable<Endpoint>> GetActiveAsync(
    CancellationToken cancellationToken)
    {
        return await _context.Endpoints
            .Where(e => e.IsActive)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
