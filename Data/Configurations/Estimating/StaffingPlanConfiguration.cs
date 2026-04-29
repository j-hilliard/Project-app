using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Estimating;

public class StaffingPlanConfiguration : IEntityTypeConfiguration<StaffingPlan>
{
    public void Configure(EntityTypeBuilder<StaffingPlan> b)
    {
        b.HasKey(sp => sp.StaffingPlanId);
        b.HasIndex(sp => new { sp.CompanyCode, sp.StaffingPlanNumber }).IsUnique();
        b.Property(sp => sp.CompanyCode).IsRequired().HasMaxLength(10);
        b.Property(sp => sp.StaffingPlanNumber).IsRequired().HasMaxLength(50);
        b.Property(sp => sp.Name).IsRequired().HasMaxLength(200);
        b.Property(sp => sp.Client).IsRequired().HasMaxLength(200);
        b.Property(sp => sp.RoughLaborTotal).HasPrecision(18, 2);
        b.HasOne(sp => sp.ConvertedEstimate)
            .WithMany()
            .HasForeignKey(sp => sp.ConvertedEstimateId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
