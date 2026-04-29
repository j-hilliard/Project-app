using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models.Planning;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Planning;

public class TaskDependencyConfiguration : IEntityTypeConfiguration<TaskDependency>
{
    public void Configure(EntityTypeBuilder<TaskDependency> b)
    {
        b.HasKey(d => d.DependencyId);
        b.Property(d => d.DependencyType).IsRequired().HasMaxLength(30).HasDefaultValue("FinishToStart");
        b.HasOne(d => d.SuccessorTask)
            .WithMany(t => t.SuccessorDependencies)
            .HasForeignKey(d => d.SuccessorTaskId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasOne(d => d.PredecessorTask)
            .WithMany(t => t.PredecessorDependencies)
            .HasForeignKey(d => d.PredecessorTaskId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
