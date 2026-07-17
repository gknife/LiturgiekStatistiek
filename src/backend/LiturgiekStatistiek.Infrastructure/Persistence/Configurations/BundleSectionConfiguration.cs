using LiturgiekStatistiek.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiturgiekStatistiek.Infrastructure.Persistence.Configurations;

public class BundleSectionConfiguration : IEntityTypeConfiguration<BundleSection>
{
    public void Configure(EntityTypeBuilder<BundleSection> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Value).IsRequired().HasMaxLength(50);
        builder.Property(x => x.IsActive).HasDefaultValue(true);

        builder.HasOne(x => x.Bundle)
            .WithMany()
            .HasForeignKey(x => x.BundleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.BundleId, x.Value }).IsUnique();
    }
}
