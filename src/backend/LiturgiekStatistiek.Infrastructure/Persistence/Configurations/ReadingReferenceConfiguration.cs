using LiturgiekStatistiek.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiturgiekStatistiek.Infrastructure.Persistence.Configurations;

public class ReadingReferenceConfiguration : IEntityTypeConfiguration<ReadingReference>
{
    public void Configure(EntityTypeBuilder<ReadingReference> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.BookName).HasMaxLength(100);
        builder.HasOne(r => r.ServiceElement)
            .WithMany(e => e.ReadingReferences)
            .HasForeignKey(r => r.ServiceElementId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(r => r.BibleBook)
            .WithMany()
            .HasForeignKey(r => r.BibleBookId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(r => r.BibleBookId);
        builder.HasIndex(r => new { r.ServiceElementId, r.Position });
    }
}
