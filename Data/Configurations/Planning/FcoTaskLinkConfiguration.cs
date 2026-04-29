using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models.Planning;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Planning;

public class FcoTaskLinkConfiguration : IEntityTypeConfiguration<FcoTaskLink>
{
    public void Configure(EntityTypeBuilder<FcoTaskLink> b)
    {
        b.HasKey(l => l.LinkId);
        b.HasIndex(l => new { l.TaskId, l.FcoDocumentId }).IsUnique();
        b.Property(l => l.LinkType).IsRequired().HasMaxLength(30).HasDefaultValue("ScopeAddition");
        b.HasOne(l => l.Task)
            .WithMany()
            .HasForeignKey(l => l.TaskId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasOne(l => l.FcoDocument)
            .WithMany()
            .HasForeignKey(l => l.FcoDocumentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
