using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Estimating;

public class RateBookLaborRateConfiguration : IEntityTypeConfiguration<RateBookLaborRate>
{
    public void Configure(EntityTypeBuilder<RateBookLaborRate> b)
    {
        b.HasKey(r => r.RateBookLaborRateId);
        b.Property(r => r.StRate).HasPrecision(18, 4);
        b.Property(r => r.OtRate).HasPrecision(18, 4);
        b.Property(r => r.DtRate).HasPrecision(18, 4);
        b.HasOne(r => r.RateBook)
            .WithMany(rb => rb.LaborRates)
            .HasForeignKey(r => r.RateBookId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
