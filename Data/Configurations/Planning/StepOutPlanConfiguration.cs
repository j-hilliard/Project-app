using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models.Planning;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Planning;

public class StepOutPlanConfiguration : IEntityTypeConfiguration<StepOutPlan>
{
    public void Configure(EntityTypeBuilder<StepOutPlan> b)
    {
        b.HasKey(p => p.PlanId);
        b.Property(p => p.CompanyCode).IsRequired().HasMaxLength(10);
        b.Property(p => p.Name).IsRequired().HasMaxLength(200);
        b.Property(p => p.Status).IsRequired().HasMaxLength(30);
        b.Property(p => p.CreatedBy).IsRequired().HasMaxLength(100);
        b.HasOne(p => p.WorkOrder)
            .WithMany(wo => wo.StepOutPlans)
            .HasForeignKey(p => p.WorkOrderId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
