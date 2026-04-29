using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models.Planning;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Planning;

public class FcoDocumentConfiguration : IEntityTypeConfiguration<FcoDocument>
{
    public void Configure(EntityTypeBuilder<FcoDocument> b)
    {
        b.HasKey(f => f.FcoDocumentId);
        b.Property(f => f.CompanyCode).IsRequired().HasMaxLength(10);
        b.Property(f => f.FcoNumber).IsRequired().HasMaxLength(50);
        b.Property(f => f.Title).IsRequired().HasMaxLength(200);
        b.Property(f => f.Status).IsRequired().HasMaxLength(30);
        b.Property(f => f.TotalFcoAmount).HasPrecision(18, 2);
        b.Property(f => f.MarkupPct).HasPrecision(5, 4);
        b.Property(f => f.UpdatedContractValue).HasPrecision(18, 2);
        b.Property(f => f.TaxPct).HasPrecision(5, 4);
        b.Property(f => f.CreatedBy).IsRequired().HasMaxLength(100);
        b.HasOne(f => f.LinkedWorkOrder)
            .WithMany()
            .HasForeignKey(f => f.LinkedWorkOrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
