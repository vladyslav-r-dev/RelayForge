using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RelayForge.Domain.Entities;

namespace RelayForge.Infrastructure.Persistence.Configurations;

public class WebhookEventConfiguration : IEntityTypeConfiguration<WebhookEvent>
{
    public void Configure(EntityTypeBuilder<WebhookEvent> builder)
    {
        builder.ToTable("webhook_events");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.EventType).IsRequired().HasMaxLength(150);
        builder.Property(e => e.Data).IsRequired().HasColumnType("text");
        builder.Property(e => e.CreatedAt).IsRequired();
    }
}
