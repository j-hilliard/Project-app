using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Estimating;

public class LaborRowConfiguration : IEntityTypeConfiguration<LaborRow>
{
    public void Configure(EntityTypeBuilder<LaborRow> b)
    {
        b.HasKey(r => r.LaborRowId);
        b.Property(r => r.BillStRate).HasPrecision(18, 4);
        b.Property(r => r.BillOtRate).HasPrecision(18, 4);
        b.Property(r => r.BillDtRate).HasPrecision(18, 4);
        b.Property(r => r.StHours).HasPrecision(10, 2);
        b.Property(r => r.OtHours).HasPrecision(10, 2);
        b.Property(r => r.DtHours).HasPrecision(10, 2);
        b.Property(r => r.Subtotal).HasPrecision(18, 2);
        b.HasOne(r => r.Estimate)
            .WithMany(e => e.LaborRows)
            .HasForeignKey(r => r.EstimateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
