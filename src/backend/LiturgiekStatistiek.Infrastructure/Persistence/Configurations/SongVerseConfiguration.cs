using LiturgiekStatistiek.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiturgiekStatistiek.Infrastructure.Persistence.Configurations;

public class SongVerseConfiguration : IEntityTypeConfiguration<SongVerse>
{
    public void Configure(EntityTypeBuilder<SongVerse> builder)
    {
        builder.HasKey(v => v.Id);
        builder.Property(v => v.VerseLabel).HasMaxLength(50).IsRequired();
        builder.Property(v => v.Position).IsRequired();

        builder.HasOne(v => v.ServiceElementSong)
            .WithMany(s => s.Verses)
            .HasForeignKey(v => v.ServiceElementSongId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
