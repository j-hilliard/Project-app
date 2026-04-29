using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Estimating;

public class EstimateSummaryConfiguration : IEntityTypeConfiguration<EstimateSummary>
{
    public void Configure(EntityTypeBuilder<EstimateSummary> b)
    {
        b.HasKey(s => s.EstimateSummaryId);
        b.HasIndex(s => s.EstimateId).IsUnique();
        b.Property(s => s.BillSubtotal).HasPrecision(18, 2);
        b.Property(s => s.DiscountValue).HasPrecision(18, 4);
        b.Property(s => s.DiscountAmount).HasPrecision(18, 2);
        b.Property(s => s.TaxRate).HasPrecision(5, 4);
        b.Property(s => s.TaxAmount).HasPrecision(18, 2);
        b.Property(s => s.GrandTotal).HasPrecision(18, 2);
        b.Property(s => s.InternalCostTotal).HasPrecision(18, 2);
        b.Property(s => s.GrossProfit).HasPrecision(18, 2);
        b.Property(s => s.GrossMarginPct).HasPrecision(18, 4);
        b.HasOne(s => s.Estimate)
            .WithOne(e => e.Summary)
            .HasForeignKey<EstimateSummary>(s => s.EstimateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
