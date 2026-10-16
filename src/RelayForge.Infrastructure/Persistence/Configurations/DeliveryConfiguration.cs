using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RelayForge.Domain.Entities;

namespace RelayForge.Infrastructure.Persistence.Configurations;

public class DeliveryConfiguration : IEntityTypeConfiguration<Delivery>
{
    public void Configure(EntityTypeBuilder<Delivery> builder)
    {
        builder.ToTable("deliveries");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.EventId).IsRequired();
        builder.Property(d => d.EndpointId).IsRequired();
        builder.Property(d => d.Status).IsRequired();
        builder.Property(d => d.AttemptCount).IsRequired();
        builder.Property(d => d.CreatedAt).IsRequired();

        builder.HasOne<WebhookEvent>()
            .WithMany(e => e.Deliveries)
            .HasForeignKey(d => d.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Endpoint>()
            .WithMany()
            .HasForeignKey(d => d.EndpointId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}