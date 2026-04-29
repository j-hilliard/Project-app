using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models.Planning;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Planning;

public class StepOutStepConfiguration : IEntityTypeConfiguration<StepOutStep>
{
    public void Configure(EntityTypeBuilder<StepOutStep> b)
    {
        b.HasKey(s => s.StepId);
        b.Property(s => s.StepCode).IsRequired().HasMaxLength(20);
        b.Property(s => s.SortOrder).HasPrecision(10, 4);
        b.Property(s => s.Title).IsRequired().HasMaxLength(300);
        b.Property(s => s.Status).IsRequired().HasMaxLength(30);
        b.Property(s => s.DurationHours).HasPrecision(8, 2);
        b.HasOne(s => s.Plan)
            .WithMany(p => p.Steps)
            .HasForeignKey(s => s.PlanId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasOne(s => s.ParentStep)
            .WithMany(s => s.SubSteps)
            .HasForeignKey(s => s.ParentStepId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
