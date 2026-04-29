using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models.Scheduling;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Scheduling;

public class CraftConfiguration : IEntityTypeConfiguration<Craft>
{
    public void Configure(EntityTypeBuilder<Craft> b)
    {
        b.HasKey(c => c.CraftCode);
        b.Property(c => c.CraftCode).IsRequired().HasMaxLength(50);
        b.Property(c => c.Title).IsRequired().HasMaxLength(100);
    }
}
