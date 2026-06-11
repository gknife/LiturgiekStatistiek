using LiturgiekStatistiek.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiturgiekStatistiek.Infrastructure.Persistence.Configurations;

public class ListDefinitionConfiguration : IEntityTypeConfiguration<ListDefinition>
{
    public void Configure(EntityTypeBuilder<ListDefinition> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Name).HasMaxLength(100).IsRequired();
        builder.Property(d => d.Description).HasMaxLength(500);
        builder.HasIndex(d => d.Name).IsUnique();
    }
}
