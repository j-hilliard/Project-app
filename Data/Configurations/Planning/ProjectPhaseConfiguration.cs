using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models.Planning;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Planning;

public class ProjectPhaseConfiguration : IEntityTypeConfiguration<ProjectPhase>
{
    public void Configure(EntityTypeBuilder<ProjectPhase> b)
    {
        b.HasKey(ph => ph.PhaseId);
        b.Property(ph => ph.Name).IsRequired().HasMaxLength(200);
        b.Property(ph => ph.Status).IsRequired().HasMaxLength(30).HasDefaultValue("Planning");
        b.Property(ph => ph.Color).HasMaxLength(20);
        b.HasOne(ph => ph.Project)
            .WithMany(p => p.Phases)
            .HasForeignKey(ph => ph.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
