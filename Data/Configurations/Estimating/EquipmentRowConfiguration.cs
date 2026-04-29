using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Estimating;

public class EquipmentRowConfiguration : IEntityTypeConfiguration<EquipmentRow>
{
    public void Configure(EntityTypeBuilder<EquipmentRow> b)
    {
        b.HasKey(r => r.EquipmentRowId);
        b.Property(r => r.Rate).HasPrecision(18, 4);
        b.Property(r => r.Subtotal).HasPrecision(18, 2);
        b.HasOne(r => r.Estimate)
            .WithMany(e => e.EquipmentRows)
            .HasForeignKey(r => r.EstimateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
