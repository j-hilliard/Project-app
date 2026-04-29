using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Core;

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> b)
    {
        b.HasKey(ur => new { ur.UserId, ur.RoleId });
        b.HasOne(ur => ur.User).WithMany(u => u.UserRoles).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(ur => ur.Role).WithMany(r => r.UserRoles).OnDelete(DeleteBehavior.Restrict);
    }
}
