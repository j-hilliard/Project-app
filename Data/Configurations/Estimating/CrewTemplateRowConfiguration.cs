using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Estimating;

public class CrewTemplateRowConfiguration : IEntityTypeConfiguration<CrewTemplateRow>
{
    public void Configure(EntityTypeBuilder<CrewTemplateRow> b)
    {
        b.HasKey(r => r.CrewTemplateRowId);
        b.HasOne(r => r.CrewTemplate)
            .WithMany(ct => ct.Rows)
            .HasForeignKey(r => r.CrewTemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
