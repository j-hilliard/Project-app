using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models.Planning;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Planning;

public class StepOutSubStepConfiguration : IEntityTypeConfiguration<StepOutSubStep>
{
    public void Configure(EntityTypeBuilder<StepOutSubStep> b)
    {
        b.HasKey(ss => ss.SubStepId);
        b.Property(ss => ss.SubStepCode).IsRequired().HasMaxLength(20);
        b.Property(ss => ss.SortOrder).HasPrecision(10, 4);
        b.Property(ss => ss.Title).IsRequired().HasMaxLength(300);
        b.Property(ss => ss.Status).IsRequired().HasMaxLength(30).HasDefaultValue("Pending");
        b.Property(ss => ss.DurationHours).HasPrecision(8, 2);
        b.Property(ss => ss.ActualDurationHours).HasPrecision(8, 2);
        b.Property(ss => ss.CraftCode).HasMaxLength(50);
        b.HasOne(ss => ss.Step)
            .WithMany(s => s.SubStepLeafs)
            .HasForeignKey(ss => ss.StepId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
