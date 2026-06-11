using LiturgiekStatistiek.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiturgiekStatistiek.Infrastructure.Persistence.Configurations;

public class ListItemConfiguration : IEntityTypeConfiguration<ListItem>
{
    public void Configure(EntityTypeBuilder<ListItem> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Value).HasMaxLength(300).IsRequired();
        builder.Property(i => i.Abbreviation).HasMaxLength(50);

        builder.HasOne(i => i.ListDefinition)
            .WithMany(d => d.Items)
            .HasForeignKey(i => i.ListDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => i.ListDefinitionId);
    }
}
