using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models.Scheduling;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Scheduling;

public class AvailabilityBlockConfiguration : IEntityTypeConfiguration<AvailabilityBlock>
{
    public void Configure(EntityTypeBuilder<AvailabilityBlock> b)
    {
        b.HasKey(ab => ab.BlockId);
        b.HasOne(ab => ab.Resource)
            .WithMany(r => r.AvailabilityBlocks)
            .HasForeignKey(ab => ab.ResourceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
