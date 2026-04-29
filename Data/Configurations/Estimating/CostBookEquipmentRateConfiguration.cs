using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Estimating;

public class CostBookEquipmentRateConfiguration : IEntityTypeConfiguration<CostBookEquipmentRate>
{
    public void Configure(EntityTypeBuilder<CostBookEquipmentRate> b)
    {
        b.HasKey(r => r.CostBookEquipmentRateId);
        b.Property(r => r.Hourly).HasPrecision(18, 4);
        b.Property(r => r.Daily).HasPrecision(18, 4);
        b.Property(r => r.Weekly).HasPrecision(18, 4);
        b.Property(r => r.Monthly).HasPrecision(18, 4);
        b.HasOne(r => r.CostBook)
            .WithMany(cb => cb.EquipmentRates)
            .HasForeignKey(r => r.CostBookId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
