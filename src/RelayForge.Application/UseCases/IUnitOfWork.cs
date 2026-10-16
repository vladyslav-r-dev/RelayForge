public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}