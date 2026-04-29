using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Estimating;

public class RateBookExpenseItemConfiguration : IEntityTypeConfiguration<RateBookExpenseItem>
{
    public void Configure(EntityTypeBuilder<RateBookExpenseItem> b)
    {
        b.HasKey(r => r.RateBookExpenseItemId);
        b.Property(r => r.Rate).HasPrecision(18, 4);
        b.HasOne(r => r.RateBook)
            .WithMany(rb => rb.ExpenseItems)
            .HasForeignKey(r => r.RateBookId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
