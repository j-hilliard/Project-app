using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models.Planning;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Planning;

public class TaskProgressSnapshotConfiguration : IEntityTypeConfiguration<TaskProgressSnapshot>
{
    public void Configure(EntityTypeBuilder<TaskProgressSnapshot> b)
    {
        b.HasKey(s => s.SnapshotId);
        b.Property(s => s.Status).IsRequired().HasMaxLength(30);
        b.Property(s => s.ReportedBy).IsRequired().HasMaxLength(100);
        b.Property(s => s.PercentComplete).HasPrecision(5, 2);
        b.HasOne(s => s.Task)
            .WithMany()
            .HasForeignKey(s => s.TaskId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
