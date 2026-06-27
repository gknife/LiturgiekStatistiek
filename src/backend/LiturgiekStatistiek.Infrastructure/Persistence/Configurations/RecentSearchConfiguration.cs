using LiturgiekStatistiek.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiturgiekStatistiek.Infrastructure.Persistence.Configurations;

public class RecentSearchConfiguration : IEntityTypeConfiguration<RecentSearch>
{
    public void Configure(EntityTypeBuilder<RecentSearch> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.UserId).HasMaxLength(200).IsRequired();
        builder.Property(s => s.QueryText).HasMaxLength(1000).IsRequired();
        builder.HasIndex(s => new { s.UserId, s.CreatedAt });
    }
}
