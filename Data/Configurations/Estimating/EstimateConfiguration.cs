using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Estimating;

public class EstimateConfiguration : IEntityTypeConfiguration<Estimate>
{
    public void Configure(EntityTypeBuilder<Estimate> b)
    {
        b.HasKey(e => e.EstimateId);
        b.HasIndex(e => new { e.CompanyCode, e.EstimateNumber }).IsUnique();
        b.Property(e => e.CompanyCode).IsRequired().HasMaxLength(10);
        b.Property(e => e.EstimateNumber).IsRequired().HasMaxLength(50);
        b.Property(e => e.Name).IsRequired().HasMaxLength(200);
        b.Property(e => e.Client).IsRequired().HasMaxLength(200);
        b.Property(e => e.Status).IsRequired().HasMaxLength(30);
        b.Property(e => e.Shift).IsRequired().HasMaxLength(10);
        b.Property(e => e.OtMethod).IsRequired().HasMaxLength(30);
        b.Property(e => e.ConfidencePct).HasPrecision(5, 2);
        b.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
        b.HasOne(e => e.StaffingPlan)
            .WithMany(sp => sp.Estimates)
            .HasForeignKey(e => e.StaffingPlanId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
