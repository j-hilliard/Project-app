using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models.Planning;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Planning;

public class StepDependencyConfiguration : IEntityTypeConfiguration<StepDependency>
{
    public void Configure(EntityTypeBuilder<StepDependency> b)
    {
        b.HasKey(d => d.DependencyId);
        b.HasOne(d => d.Step)
            .WithMany(s => s.Dependencies)
            .HasForeignKey(d => d.StepId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasOne(d => d.PredecessorStep)
            .WithMany()
            .HasForeignKey(d => d.PredecessorStepId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
