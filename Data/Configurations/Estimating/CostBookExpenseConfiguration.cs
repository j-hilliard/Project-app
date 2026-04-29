using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Estimating;

public class CostBookExpenseConfiguration : IEntityTypeConfiguration<CostBookExpense>
{
    public void Configure(EntityTypeBuilder<CostBookExpense> b)
    {
        b.HasKey(r => r.CostBookExpenseId);
        b.Property(r => r.Rate).HasPrecision(18, 4);
        b.HasOne(r => r.CostBook)
            .WithMany(cb => cb.Expenses)
            .HasForeignKey(r => r.CostBookId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
