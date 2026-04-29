using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Estimating;

public class EstimateRevisionConfiguration : IEntityTypeConfiguration<EstimateRevision>
{
    public void Configure(EntityTypeBuilder<EstimateRevision> b)
    {
        b.HasKey(r => r.EstimateRevisionId);
        b.HasIndex(r => new { r.EstimateId, r.RevisionNumber }).IsUnique();
        b.Property(r => r.LaborTotal).HasPrecision(18, 2);
        b.Property(r => r.EquipTotal).HasPrecision(18, 2);
        b.Property(r => r.GrandTotal).HasPrecision(18, 2);
        b.HasOne(r => r.Estimate)
            .WithMany(e => e.Revisions)
            .HasForeignKey(r => r.EstimateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
