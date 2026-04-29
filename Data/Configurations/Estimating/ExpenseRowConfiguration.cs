using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Estimating;

public class ExpenseRowConfiguration : IEntityTypeConfiguration<ExpenseRow>
{
    public void Configure(EntityTypeBuilder<ExpenseRow> b)
    {
        b.HasKey(r => r.ExpenseRowId);
        b.Property(r => r.Rate).HasPrecision(18, 4);
        b.Property(r => r.Subtotal).HasPrecision(18, 2);
        b.HasOne(r => r.Estimate)
            .WithMany(e => e.ExpenseRows)
            .HasForeignKey(r => r.EstimateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
