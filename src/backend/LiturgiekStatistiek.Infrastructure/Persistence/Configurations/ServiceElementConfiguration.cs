using LiturgiekStatistiek.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiturgiekStatistiek.Infrastructure.Persistence.Configurations;

public class ServiceElementConfiguration : IEntityTypeConfiguration<ServiceElement>
{
    public void Configure(EntityTypeBuilder<ServiceElement> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Position).IsRequired();
        builder.Property(e => e.ElementType).IsRequired();
        builder.Property(e => e.ScriptureReference).HasMaxLength(500);
        builder.Property(e => e.Notes).HasMaxLength(1000);

        builder.HasOne(e => e.Service)
            .WithMany(s => s.Elements)
            .HasForeignKey(e => e.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.ServiceId, e.Position });
    }
}
