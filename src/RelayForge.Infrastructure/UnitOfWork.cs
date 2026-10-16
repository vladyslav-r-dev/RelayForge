using RelayForge.Application.UseCases;
using RelayForge.Infrastructure.Persistence; 

namespace RelayForge.Infrastructure
{
public class UnitOfWork(RelayForgeDbContext context) : IUnitOfWork
{
    private readonly RelayForgeDbContext _context = context;

        public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
}