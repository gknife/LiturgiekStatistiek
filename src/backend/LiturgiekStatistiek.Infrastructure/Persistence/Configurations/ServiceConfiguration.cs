using LiturgiekStatistiek.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiturgiekStatistiek.Infrastructure.Persistence.Configurations;

public class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Date).IsRequired();
        builder.Property(s => s.TimeOfDay).IsRequired();
        builder.Property(s => s.SermonText).HasMaxLength(500);
        builder.Property(s => s.SermonTheme).HasMaxLength(500);
        builder.Property(s => s.BroadcastUrl).HasMaxLength(1000);
        builder.Property(s => s.Notes).HasMaxLength(2000);
        builder.Property(s => s.ReadSermonBy).HasMaxLength(200);

        builder.HasOne(s => s.Congregation)
            .WithMany(c => c.Services)
            .HasForeignKey(s => s.CongregationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Preacher)
            .WithMany(p => p.Services)
            .HasForeignKey(s => s.PreacherId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(s => s.Date);
        builder.HasIndex(s => s.CongregationId);
        builder.HasIndex(s => s.Status);
    }
}
