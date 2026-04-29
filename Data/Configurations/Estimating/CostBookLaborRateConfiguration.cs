using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Estimating;

public class CostBookLaborRateConfiguration : IEntityTypeConfiguration<CostBookLaborRate>
{
    public void Configure(EntityTypeBuilder<CostBookLaborRate> b)
    {
        b.HasKey(r => r.CostBookLaborRateId);
        b.Property(r => r.StRate).HasPrecision(18, 4);
        b.Property(r => r.OtRate).HasPrecision(18, 4);
        b.Property(r => r.DtRate).HasPrecision(18, 4);
        b.HasOne(r => r.CostBook)
            .WithMany(cb => cb.LaborRates)
            .HasForeignKey(r => r.CostBookId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
