using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models.Scheduling;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Scheduling;

public class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> b)
    {
        b.HasKey(a => a.AssignmentId);
        b.Property(a => a.CompanyCode).IsRequired().HasMaxLength(10);
        b.Property(a => a.CraftCode).IsRequired().HasMaxLength(50);
        b.Property(a => a.JobSourceType).IsRequired().HasMaxLength(30);
        b.Property(a => a.Shift).IsRequired().HasMaxLength(20);
        b.Property(a => a.Status).IsRequired().HasMaxLength(20);
        b.Property(a => a.CreatedBy).IsRequired().HasMaxLength(100);
        b.HasOne(a => a.Resource)
            .WithMany(r => r.Assignments)
            .HasForeignKey(a => a.ResourceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
