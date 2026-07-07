using LiturgiekStatistiek.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiturgiekStatistiek.Infrastructure.Persistence.Configurations;

public class ServiceTemplateElementConfiguration : IEntityTypeConfiguration<ServiceTemplateElement>
{
    public void Configure(EntityTypeBuilder<ServiceTemplateElement> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Position).IsRequired();
        builder.Property(e => e.ElementType).IsRequired();
        builder.Property(e => e.FixedScriptureReference).HasMaxLength(500);

        builder.HasOne(e => e.ServiceTemplate)
            .WithMany(t => t.Elements)
            .HasForeignKey(e => e.ServiceTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Label)
            .WithMany()
            .HasForeignKey(e => e.LabelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Performer)
            .WithMany()
            .HasForeignKey(e => e.PerformerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.ServiceTemplateId, e.Position });
    }
}
