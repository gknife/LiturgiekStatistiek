using LiturgiekStatistiek.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiturgiekStatistiek.Infrastructure.Persistence.Configurations;

public class SongConfiguration : IEntityTypeConfiguration<Song>
{
    public void Configure(EntityTypeBuilder<Song> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Number).IsRequired();
        builder.Property(s => s.Title).HasMaxLength(300);

        builder.HasIndex(s => new { s.BundleId, s.Number }).IsUnique();
    }
}
