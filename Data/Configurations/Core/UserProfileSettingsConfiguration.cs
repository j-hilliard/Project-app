using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Core;

public class UserProfileSettingsConfiguration : IEntityTypeConfiguration<UserProfileSettings>
{
    public void Configure(EntityTypeBuilder<UserProfileSettings> b)
    {
        b.ToTable("Profile");
        b.HasKey(p => p.ProfileId);
        b.HasIndex(p => p.UserId).IsUnique();
        b.HasOne(p => p.User)
            .WithOne(u => u.Profile)
            .HasForeignKey<UserProfileSettings>(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
