using LiturgiekStatistiek.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiturgiekStatistiek.Infrastructure.Persistence.Configurations;

public class PreacherConfiguration : IEntityTypeConfiguration<Preacher>
{
    public void Configure(EntityTypeBuilder<Preacher> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.FullName).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Title).HasMaxLength(20);
        builder.Property(p => p.City).HasMaxLength(200);
    }
}
