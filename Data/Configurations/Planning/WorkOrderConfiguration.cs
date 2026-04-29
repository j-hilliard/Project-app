using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stronghold.EnterpriseEstimating.Data.Models.Planning;

namespace Stronghold.EnterpriseEstimating.Data.Configurations.Planning;

public class WorkOrderConfiguration : IEntityTypeConfiguration<WorkOrder>
{
    public void Configure(EntityTypeBuilder<WorkOrder> b)
    {
        b.HasKey(wo => wo.WorkOrderId);
        b.HasIndex(wo => new { wo.CompanyCode, wo.WorkOrderNumber }).IsUnique();
        b.Property(wo => wo.CompanyCode).IsRequired().HasMaxLength(10);
        b.Property(wo => wo.WorkOrderNumber).IsRequired().HasMaxLength(50);
        b.Property(wo => wo.Title).IsRequired().HasMaxLength(200);
        b.Property(wo => wo.AuthorizedValue).HasPrecision(18, 2).HasDefaultValue(0m);
        b.Property(wo => wo.Status).IsRequired().HasMaxLength(30).HasDefaultValue("Draft");
        b.Property(wo => wo.ReleasedBy).HasMaxLength(200);
        b.Property(wo => wo.CreatedBy).IsRequired().HasMaxLength(100);
        b.HasOne(wo => wo.Project)
            .WithMany(p => p.WorkOrders)
            .HasForeignKey(wo => wo.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(wo => wo.CommercialAuthorization)
            .WithMany(ca => ca.WorkOrders)
            .HasForeignKey(wo => wo.CommercialAuthorizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
