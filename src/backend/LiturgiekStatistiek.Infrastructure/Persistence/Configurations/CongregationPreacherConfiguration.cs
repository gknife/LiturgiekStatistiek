using LiturgiekStatistiek.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiturgiekStatistiek.Infrastructure.Persistence.Configurations;

public class CongregationPreacherConfiguration : IEntityTypeConfiguration<CongregationPreacher>
{
    public void Configure(EntityTypeBuilder<CongregationPreacher> builder)
    {
        builder.HasKey(cp => new { cp.CongregationId, cp.PreacherId });

        builder.HasOne(cp => cp.Congregation)
            .WithMany(c => c.Pastors)
            .HasForeignKey(cp => cp.CongregationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cp => cp.Preacher)
            .WithMany(p => p.Congregations)
            .HasForeignKey(cp => cp.PreacherId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(cp => cp.PreacherId);
    }
}
