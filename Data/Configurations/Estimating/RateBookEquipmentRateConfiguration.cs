using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Estimating;

public class RateBookEquipmentRateConfiguration : IEntityTypeConfiguration<RateBookEquipmentRate>
{
    public void Configure(EntityTypeBuilder<RateBookEquipmentRate> b)
    {
        b.HasKey(r => r.RateBookEquipmentRateId);
        b.Property(r => r.Hourly).HasPrecision(18, 4);
        b.Property(r => r.Daily).HasPrecision(18, 4);
        b.Property(r => r.Weekly).HasPrecision(18, 4);
        b.Property(r => r.Monthly).HasPrecision(18, 4);
        b.HasOne(r => r.RateBook)
            .WithMany(rb => rb.EquipmentRates)
            .HasForeignKey(r => r.RateBookId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
