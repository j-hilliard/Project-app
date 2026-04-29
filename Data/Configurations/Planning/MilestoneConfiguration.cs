using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models.Planning;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Planning;

public class MilestoneConfiguration : IEntityTypeConfiguration<Milestone>
{
    public void Configure(EntityTypeBuilder<Milestone> b)
    {
        b.HasKey(m => m.MilestoneId);
        b.Property(m => m.Name).IsRequired().HasMaxLength(200);
        b.Property(m => m.Status).IsRequired().HasMaxLength(30).HasDefaultValue("Pending");
        b.Property(m => m.Color).HasMaxLength(20);
        b.Property(m => m.CreatedBy).IsRequired().HasMaxLength(100);
        b.HasOne(m => m.Project)
            .WithMany(p => p.Milestones)
            .HasForeignKey(m => m.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasOne(m => m.Phase)
            .WithMany(ph => ph.Milestones)
            .HasForeignKey(m => m.PhaseId)
            .OnDelete(DeleteBehavior.NoAction);
        b.HasOne(m => m.Task)
            .WithMany(t => t.Milestones)
            .HasForeignKey(m => m.TaskId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
