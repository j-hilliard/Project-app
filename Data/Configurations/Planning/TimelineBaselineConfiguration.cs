using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models.Planning;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Planning;

public class TimelineBaselineConfiguration : IEntityTypeConfiguration<TimelineBaseline>
{
    public void Configure(EntityTypeBuilder<TimelineBaseline> b)
    {
        b.HasKey(tb => tb.BaselineId);
        b.Property(tb => tb.EntityType).IsRequired().HasMaxLength(20).HasDefaultValue("Project");
        b.Property(tb => tb.BaselineReason).HasMaxLength(500);
        b.Property(tb => tb.BaselineLabel).HasMaxLength(100);
        b.Property(tb => tb.LockedBy).IsRequired().HasMaxLength(100);
        b.HasOne(tb => tb.Project)
            .WithMany()
            .HasForeignKey(tb => tb.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
