using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models.Planning;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Planning;

public class ActualEntryConfiguration : IEntityTypeConfiguration<ActualEntry>
{
    public void Configure(EntityTypeBuilder<ActualEntry> b)
    {
        b.HasKey(a => a.ActualEntryId);
        b.Property(a => a.CompanyCode).IsRequired().HasMaxLength(10);
        b.Property(a => a.ActualType).IsRequired().HasMaxLength(30).HasDefaultValue("Labor");
        b.Property(a => a.Position).HasMaxLength(100);
        b.Property(a => a.CraftCode).HasMaxLength(50);
        b.Property(a => a.EnteredBy).IsRequired().HasMaxLength(100);
        b.Property(a => a.StHours).HasPrecision(10, 2);
        b.Property(a => a.OtHours).HasPrecision(10, 2);
        b.Property(a => a.DtHours).HasPrecision(10, 2);
        b.Property(a => a.CostAmount).HasPrecision(18, 2);
        b.Property(a => a.BillableAmount).HasPrecision(18, 2);
        b.Property(a => a.BilledAmount).HasPrecision(18, 2);
        b.HasOne(a => a.WorkOrder)
            .WithMany()
            .HasForeignKey(a => a.WorkOrderId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(a => a.FcoDocument)
            .WithMany()
            .HasForeignKey(a => a.FcoDocumentId)
            .OnDelete(DeleteBehavior.NoAction);
        b.HasOne(a => a.PlanTask)
            .WithMany()
            .HasForeignKey(a => a.PlanTaskId)
            .OnDelete(DeleteBehavior.NoAction);
        b.HasOne(a => a.StepOutStep)
            .WithMany()
            .HasForeignKey(a => a.StepOutStepId)
            .OnDelete(DeleteBehavior.NoAction);
        b.HasOne(a => a.StepOutSubStep)
            .WithMany()
            .HasForeignKey(a => a.StepOutSubStepId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
