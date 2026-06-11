using LiturgiekStatistiek.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiturgiekStatistiek.Infrastructure.Persistence.Configurations;

public class ChangeHistoryConfiguration : IEntityTypeConfiguration<ChangeHistory>
{
    public void Configure(EntityTypeBuilder<ChangeHistory> builder)
    {
        builder.HasKey(h => h.Id);
        builder.Property(h => h.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(h => h.ChangedBy).HasMaxLength(200).IsRequired();

        builder.HasIndex(h => new { h.EntityType, h.EntityId });
        builder.HasIndex(h => h.ChangedAt);
    }
}
