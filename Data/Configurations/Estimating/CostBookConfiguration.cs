using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Estimating;

public class CostBookConfiguration : IEntityTypeConfiguration<CostBook>
{
    public void Configure(EntityTypeBuilder<CostBook> b)
    {
        b.HasKey(cb => cb.CostBookId);
        b.Property(cb => cb.CompanyCode).IsRequired().HasMaxLength(10);
        b.Property(cb => cb.Name).IsRequired().HasMaxLength(200);
    }
}
