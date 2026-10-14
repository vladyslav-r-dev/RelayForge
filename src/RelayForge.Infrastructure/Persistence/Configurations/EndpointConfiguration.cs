using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RelayForge.Domain.Entities;

namespace RelayForge.Infrastructure.Persistence.Configurations;
public class EndpointConfiguration : IEntityTypeConfiguration<Endpoint>
{
    public void Configure(EntityTypeBuilder<Endpoint> builder)
    {
        builder.ToTable("endpoints");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Url).IsRequired().HasMaxLength(2048);
        builder.Property(e => e.IsActive).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
    }
}