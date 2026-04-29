using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models.Scheduling;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Scheduling;

public class CertificationConfiguration : IEntityTypeConfiguration<Certification>
{
    public void Configure(EntityTypeBuilder<Certification> b)
    {
        b.HasKey(c => c.CertId);
        b.Property(c => c.Type).IsRequired().HasMaxLength(50);
        b.HasOne(c => c.Resource)
            .WithMany(r => r.Certifications)
            .HasForeignKey(c => c.ResourceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
