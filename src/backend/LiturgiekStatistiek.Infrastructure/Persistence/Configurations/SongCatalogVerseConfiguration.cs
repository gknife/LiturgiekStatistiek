using LiturgiekStatistiek.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiturgiekStatistiek.Infrastructure.Persistence.Configurations;

public class SongCatalogVerseConfiguration : IEntityTypeConfiguration<SongCatalogVerse>
{
    public void Configure(EntityTypeBuilder<SongCatalogVerse> builder)
    {
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Number).IsRequired();
        builder.Property(v => v.Title).HasMaxLength(300);

        builder.HasOne(v => v.Song)
            .WithMany(s => s.Verses)
            .HasForeignKey(v => v.SongId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(v => new { v.SongId, v.Number }).IsUnique();
    }
}
