using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models.Planning;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Planning;

public class WorkPackageConfiguration : IEntityTypeConfiguration<WorkPackage>
{
    public void Configure(EntityTypeBuilder<WorkPackage> b)
    {
        b.HasKey(wp => wp.PackageId);
        b.Property(wp => wp.CompanyCode).IsRequired().HasMaxLength(10);
        b.Property(wp => wp.Title).IsRequired().HasMaxLength(200);
        b.Property(wp => wp.Status).IsRequired().HasMaxLength(30);
        b.Property(wp => wp.CreatedBy).IsRequired().HasMaxLength(100);
        b.HasOne(wp => wp.Plan)
            .WithMany(p => p.WorkPackages)
            .HasForeignKey(wp => wp.PlanId)
            .OnDelete(DeleteBehavior.SetNull);
        b.HasOne(wp => wp.WorkOrder)
            .WithMany(wo => wo.WorkPackages)
            .HasForeignKey(wp => wp.WorkOrderId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
