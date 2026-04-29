using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Estimating;

public class EstimateSequenceConfiguration : IEntityTypeConfiguration<EstimateSequence>
{
    public void Configure(EntityTypeBuilder<EstimateSequence> b)
    {
        b.HasKey(s => s.EstimateSequenceId);
        b.HasIndex(s => new { s.CompanyCode, s.Year, s.SequenceType }).IsUnique();
    }
}
