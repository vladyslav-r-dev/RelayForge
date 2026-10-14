using Microsoft.EntityFrameworkCore;
using RelayForge.Domain.Entities;

namespace RelayForge.Infrastructure.Persistence;
public class RelayForgeDbContext(DbContextOptions<RelayForgeDbContext> options) : DbContext(options)
{
    public DbSet<Endpoint> Endpoints { get; set; }
    public DbSet<WebhookEvent> WebhookEvents { get; set; }
    public DbSet<Delivery> Deliveries  { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
    base.OnModelCreating(modelBuilder);

    modelBuilder.ApplyConfigurationsFromAssembly(
        typeof(RelayForgeDbContext).Assembly);
    }
}