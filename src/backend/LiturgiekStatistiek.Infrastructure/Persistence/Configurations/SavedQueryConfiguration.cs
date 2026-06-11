using LiturgiekStatistiek.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiturgiekStatistiek.Infrastructure.Persistence.Configurations;

public class SavedQueryConfiguration : IEntityTypeConfiguration<SavedQuery>
{
    public void Configure(EntityTypeBuilder<SavedQuery> builder)
    {
        builder.HasKey(q => q.Id);
        builder.Property(q => q.UserId).HasMaxLength(200).IsRequired();
        builder.Property(q => q.Name).HasMaxLength(200).IsRequired();
    }
}
