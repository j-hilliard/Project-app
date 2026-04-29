using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models.Planning;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Planning;

public class EstimateTaskLinkConfiguration : IEntityTypeConfiguration<EstimateTaskLink>
{
    public void Configure(EntityTypeBuilder<EstimateTaskLink> b)
    {
        b.HasKey(l => l.LinkId);
        b.HasIndex(l => new { l.TaskId, l.EstimateId }).IsUnique();
        b.Property(l => l.LinkType).IsRequired().HasMaxLength(30).HasDefaultValue("Primary");
        b.HasOne(l => l.Task)
            .WithMany()
            .HasForeignKey(l => l.TaskId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
