using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models.Planning;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Planning;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> b)
    {
        b.HasKey(p => p.ProjectId);
        b.HasIndex(p => new { p.CompanyCode, p.ProjectNumber }).IsUnique();
        b.Property(p => p.CompanyCode).IsRequired().HasMaxLength(10);
        b.Property(p => p.ProjectNumber).IsRequired().HasMaxLength(50);
        b.Property(p => p.Name).IsRequired().HasMaxLength(200);
        b.Property(p => p.Client).HasMaxLength(200);
        b.Property(p => p.ClientCode).HasMaxLength(50);
        b.Property(p => p.Site).HasMaxLength(200);
        b.Property(p => p.City).HasMaxLength(100);
        b.Property(p => p.State).HasMaxLength(50);
        b.Property(p => p.JobLetter).HasMaxLength(10);
        b.Property(p => p.Status).IsRequired().HasMaxLength(30).HasDefaultValue("Initiating");
        b.Property(p => p.AtRiskThresholdDays).HasDefaultValue(5);
        b.Property(p => p.OwnerUserId).HasMaxLength(100);
        b.Property(p => p.CreatedBy).IsRequired().HasMaxLength(100);
        b.HasOne(p => p.CommercialAuthorization)
            .WithMany(ca => ca.Projects)
            .HasForeignKey(p => p.CommercialAuthorizationId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
