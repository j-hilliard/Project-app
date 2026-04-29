using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models.Scheduling;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Scheduling;

public class ResourceConfiguration : IEntityTypeConfiguration<Resource>
{
    public void Configure(EntityTypeBuilder<Resource> b)
    {
        b.HasKey(r => r.ResourceId);
        b.Property(r => r.CompanyCode).IsRequired().HasMaxLength(10);
        b.Property(r => r.Name).IsRequired().HasMaxLength(200);
        b.Property(r => r.EmployeeId).HasMaxLength(50);
        b.Property(r => r.FirstName).IsRequired().HasMaxLength(100);
        b.Property(r => r.LastName).IsRequired().HasMaxLength(100);
        b.Property(r => r.CraftCode).IsRequired().HasMaxLength(50);
        b.Property(r => r.Region).HasMaxLength(100);
        b.Property(r => r.Branch).HasMaxLength(100);
        b.Property(r => r.EmploymentStatus).IsRequired().HasMaxLength(20).HasDefaultValue("Active");
        b.Property(r => r.ShiftEligibility).IsRequired().HasMaxLength(20).HasDefaultValue("Any");
        b.Property(r => r.Phone).HasMaxLength(30);
        b.Property(r => r.Email).HasMaxLength(200);
        b.HasIndex(r => new { r.CompanyCode, r.EmployeeId });
        b.HasOne(r => r.Craft)
            .WithMany(c => c.Resources)
            .HasForeignKey(r => r.CraftCode)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
