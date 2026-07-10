using LiturgiekStatistiek.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiturgiekStatistiek.Infrastructure.Persistence.Configurations;

public class ServiceTemplateConfiguration : IEntityTypeConfiguration<ServiceTemplate>
{
    public void Configure(EntityTypeBuilder<ServiceTemplate> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).HasMaxLength(200).IsRequired();

        builder.HasOne(t => t.Denomination)
            .WithMany()
            .HasForeignKey(t => t.DenominationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Congregation)
            .WithMany()
            .HasForeignKey(t => t.CongregationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Occasion)
            .WithMany()
            .HasForeignKey(t => t.OccasionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.MusicalAccompaniment)
            .WithMany()
            .HasForeignKey(t => t.MusicalAccompanimentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.DefaultBibleTranslation)
            .WithMany()
            .HasForeignKey(t => t.DefaultBibleTranslationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.DefaultSongBundle)
            .WithMany()
            .HasForeignKey(t => t.DefaultSongBundleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.DenominationId);
        builder.HasIndex(t => t.CongregationId);
    }
}
