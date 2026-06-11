using LiturgiekStatistiek.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiturgiekStatistiek.Infrastructure.Persistence.Configurations;

public class ServiceBundleConfiguration : IEntityTypeConfiguration<ServiceBundle>
{
    public void Configure(EntityTypeBuilder<ServiceBundle> builder)
    {
        builder.HasKey(sb => new { sb.ServiceId, sb.BundleId });

        builder.HasOne(sb => sb.Service)
            .WithMany(s => s.Bundles)
            .HasForeignKey(sb => sb.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
