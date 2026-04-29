using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Core;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> b)
    {
        b.HasKey(c => c.CompanyCode);
        b.Property(c => c.CompanyCode).IsRequired().HasMaxLength(10);
        b.Property(c => c.Name).IsRequired().HasMaxLength(200);
        b.Property(c => c.ShortName).IsRequired().HasMaxLength(50);
    }
}
