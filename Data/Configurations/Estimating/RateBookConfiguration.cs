using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Estimating;

public class RateBookConfiguration : IEntityTypeConfiguration<RateBook>
{
    public void Configure(EntityTypeBuilder<RateBook> b)
    {
        b.HasKey(rb => rb.RateBookId);
        b.Property(rb => rb.CompanyCode).IsRequired().HasMaxLength(10);
        b.Property(rb => rb.Name).IsRequired().HasMaxLength(200);
    }
}
