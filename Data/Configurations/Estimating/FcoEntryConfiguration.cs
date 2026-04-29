using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Estimating;

public class FcoEntryConfiguration : IEntityTypeConfiguration<FcoEntry>
{
    public void Configure(EntityTypeBuilder<FcoEntry> b)
    {
        b.HasKey(f => f.FcoEntryId);
        b.Property(f => f.DollarAdjustment).HasPrecision(18, 2);
        b.HasOne(f => f.Estimate)
            .WithMany(e => e.FcoEntries)
            .HasForeignKey(f => f.EstimateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
