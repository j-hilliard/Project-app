using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models.Planning;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Planning;

public class PlanTaskConfiguration : IEntityTypeConfiguration<PlanTask>
{
    public void Configure(EntityTypeBuilder<PlanTask> b)
    {
        b.HasKey(t => t.TaskId);
        b.Property(t => t.Title).IsRequired().HasMaxLength(300);
        b.Property(t => t.TaskType).IsRequired().HasMaxLength(30).HasDefaultValue("Task");
        b.Property(t => t.Status).IsRequired().HasMaxLength(30).HasDefaultValue("Pending");
        b.Property(t => t.PercentComplete).HasPrecision(5, 2);
        b.Property(t => t.CraftCode).HasMaxLength(50);
        b.Property(t => t.AssignedTo).HasMaxLength(200);
        b.Property(t => t.OwnerUserId).HasMaxLength(100);
        b.Property(t => t.CreatedBy).IsRequired().HasMaxLength(100);
        b.HasOne(t => t.Phase)
            .WithMany(ph => ph.Tasks)
            .HasForeignKey(t => t.PhaseId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasOne(t => t.ParentTask)
            .WithMany(t => t.SubTasks)
            .HasForeignKey(t => t.ParentTaskId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
