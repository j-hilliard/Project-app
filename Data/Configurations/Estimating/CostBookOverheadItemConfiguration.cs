using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Estimating;

public class CostBookOverheadItemConfiguration : IEntityTypeConfiguration<CostBookOverheadItem>
{
    public void Configure(EntityTypeBuilder<CostBookOverheadItem> b)
    {
        b.HasKey(r => r.CostBookOverheadItemId);
        b.Property(r => r.Value).HasPrecision(10, 4);
        b.HasOne(r => r.CostBook)
            .WithMany(cb => cb.OverheadItems)
            .HasForeignKey(r => r.CostBookId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
