using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models.Planning;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Planning;

public class StepResourceReqConfiguration : IEntityTypeConfiguration<StepResourceReq>
{
    public void Configure(EntityTypeBuilder<StepResourceReq> b)
    {
        b.HasKey(r => r.ReqId);
        b.Property(r => r.CraftCode).IsRequired().HasMaxLength(50);
        b.HasOne(r => r.Step)
            .WithMany(s => s.ResourceRequirements)
            .HasForeignKey(r => r.StepId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
