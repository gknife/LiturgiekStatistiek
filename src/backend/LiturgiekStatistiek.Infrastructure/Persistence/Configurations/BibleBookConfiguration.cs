using LiturgiekStatistiek.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiturgiekStatistiek.Infrastructure.Persistence.Configurations;

public class BibleBookConfiguration : IEntityTypeConfiguration<BibleBook>
{
    public void Configure(EntityTypeBuilder<BibleBook> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Testament).HasMaxLength(2).IsRequired();
        builder.Property(b => b.Name).HasMaxLength(100).IsRequired();
        builder.Property(b => b.VerseCountsJson).IsRequired();
        builder.HasIndex(b => b.Ordinal).IsUnique();
        builder.HasMany(b => b.TranslationNames)
            .WithOne(t => t.BibleBook)
            .HasForeignKey(t => t.BibleBookId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class BibleBookTranslationNameConfiguration : IEntityTypeConfiguration<BibleBookTranslationName>
{
    public void Configure(EntityTypeBuilder<BibleBookTranslationName> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.TranslationAbbreviation).HasMaxLength(20).IsRequired();
        builder.Property(t => t.Name).HasMaxLength(100).IsRequired();
    }
}
