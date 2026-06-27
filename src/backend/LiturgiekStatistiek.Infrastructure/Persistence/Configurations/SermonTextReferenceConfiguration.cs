using LiturgiekStatistiek.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiturgiekStatistiek.Infrastructure.Persistence.Configurations;

public class SermonTextReferenceConfiguration : IEntityTypeConfiguration<SermonTextReference>
{
    public void Configure(EntityTypeBuilder<SermonTextReference> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.BookName).HasMaxLength(100);
        builder.HasOne(r => r.Service)
            .WithMany(s => s.SermonTextReferences)
            .HasForeignKey(r => r.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(r => r.BibleBook)
            .WithMany()
            .HasForeignKey(r => r.BibleBookId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(r => r.BibleBookId);
    }
}
