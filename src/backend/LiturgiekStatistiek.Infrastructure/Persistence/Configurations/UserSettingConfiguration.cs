using LiturgiekStatistiek.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LiturgiekStatistiek.Infrastructure.Persistence.Configurations;

public class UserSettingConfiguration : IEntityTypeConfiguration<UserSetting>
{
    public void Configure(EntityTypeBuilder<UserSetting> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.UserId).HasMaxLength(200).IsRequired();
        builder.Property(s => s.SettingsJson).IsRequired();
        builder.HasIndex(s => s.UserId).IsUnique();
    }
}
