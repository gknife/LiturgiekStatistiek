using LiturgiekStatistiek.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiturgiekStatistiek.Infrastructure.Persistence.Configurations;

public class ContentPageConfiguration : IEntityTypeConfiguration<ContentPage>
{
    public void Configure(EntityTypeBuilder<ContentPage> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Slug).HasMaxLength(100).IsRequired();
        builder.Property(p => p.TitleNl).HasMaxLength(200).IsRequired();
        builder.HasIndex(p => p.Slug).IsUnique();
    }
}
