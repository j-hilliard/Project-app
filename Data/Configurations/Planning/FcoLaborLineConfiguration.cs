using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models.Planning;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Planning;

public class FcoLaborLineConfiguration : IEntityTypeConfiguration<FcoLaborLine>
{
    public void Configure(EntityTypeBuilder<FcoLaborLine> b)
    {
        b.HasKey(l => l.FcoLaborLineId);
        b.Property(l => l.Position).IsRequired().HasMaxLength(100);
        b.Property(l => l.LaborType).IsRequired().HasMaxLength(20).HasDefaultValue("Direct");
        b.Property(l => l.CraftCode).HasMaxLength(50);
        b.Property(l => l.NavCode).HasMaxLength(50);
        b.Property(l => l.StHours).HasPrecision(10, 2);
        b.Property(l => l.OtHours).HasPrecision(10, 2);
        b.Property(l => l.DtHours).HasPrecision(10, 2);
        b.Property(l => l.BillStRate).HasPrecision(18, 4);
        b.Property(l => l.BillOtRate).HasPrecision(18, 4);
        b.Property(l => l.BillDtRate).HasPrecision(18, 4);
        b.Property(l => l.Subtotal).HasPrecision(18, 2);
        b.HasOne(l => l.FcoDocument)
            .WithMany()
            .HasForeignKey(l => l.FcoDocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
