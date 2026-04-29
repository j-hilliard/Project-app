using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Estimating;

public class CrewTemplateConfiguration : IEntityTypeConfiguration<CrewTemplate>
{
    public void Configure(EntityTypeBuilder<CrewTemplate> b)
    {
        b.HasKey(ct => ct.CrewTemplateId);
        b.Property(ct => ct.CompanyCode).IsRequired().HasMaxLength(10);
        b.Property(ct => ct.Name).IsRequired().HasMaxLength(200);
    }
}
