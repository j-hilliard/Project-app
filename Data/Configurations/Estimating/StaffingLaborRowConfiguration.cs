using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Estimating;

public class StaffingLaborRowConfiguration : IEntityTypeConfiguration<StaffingLaborRow>
{
    public void Configure(EntityTypeBuilder<StaffingLaborRow> b)
    {
        b.HasKey(r => r.StaffingLaborRowId);
        b.Property(r => r.StRate).HasPrecision(18, 4);
        b.Property(r => r.OtRate).HasPrecision(18, 4);
        b.Property(r => r.DtRate).HasPrecision(18, 4);
        b.Property(r => r.StHours).HasPrecision(10, 2);
        b.Property(r => r.OtHours).HasPrecision(10, 2);
        b.Property(r => r.DtHours).HasPrecision(10, 2);
        b.Property(r => r.Subtotal).HasPrecision(18, 2);
        b.HasOne(r => r.StaffingPlan)
            .WithMany(sp => sp.LaborRows)
            .HasForeignKey(r => r.StaffingPlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
