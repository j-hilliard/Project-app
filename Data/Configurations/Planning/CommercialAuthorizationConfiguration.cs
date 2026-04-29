using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models.Planning;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Planning;

public class CommercialAuthorizationConfiguration : IEntityTypeConfiguration<CommercialAuthorization>
{
    public void Configure(EntityTypeBuilder<CommercialAuthorization> b)
    {
        b.HasKey(ca => ca.CommercialAuthorizationId);
        b.HasIndex(ca => new { ca.CompanyCode, ca.AuthorizationNumber });
        b.Property(ca => ca.CompanyCode).IsRequired().HasMaxLength(10);
        b.Property(ca => ca.AuthorizationNumber).IsRequired().HasMaxLength(100);
        b.Property(ca => ca.AuthorizationType).IsRequired().HasMaxLength(50);
        b.Property(ca => ca.AuthorizedValue).HasPrecision(18, 2);
        b.Property(ca => ca.AuthorizedBy).HasMaxLength(200);
        b.Property(ca => ca.Status).IsRequired().HasMaxLength(30).HasDefaultValue("Draft");
        b.Property(ca => ca.DocumentReference).HasMaxLength(500);
        b.Property(ca => ca.CreatedBy).IsRequired().HasMaxLength(100);
    }
}
