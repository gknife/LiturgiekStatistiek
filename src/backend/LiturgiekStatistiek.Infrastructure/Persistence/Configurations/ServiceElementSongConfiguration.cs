using LiturgiekStatistiek.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiturgiekStatistiek.Infrastructure.Persistence.Configurations;

public class ServiceElementSongConfiguration : IEntityTypeConfiguration<ServiceElementSong>
{
    public void Configure(EntityTypeBuilder<ServiceElementSong> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.SongNumber).IsRequired();
        builder.Property(s => s.Position).IsRequired();
        builder.Property(s => s.Section).IsRequired().HasMaxLength(50).HasDefaultValue("");

        builder.HasOne(s => s.ServiceElement)
            .WithMany(e => e.Songs)
            .HasForeignKey(s => s.ServiceElementId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.BundleId, s.Section, s.SongNumber });
    }
}
