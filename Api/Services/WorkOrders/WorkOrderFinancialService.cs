using Microsoft.EntityFrameworkCore;
using Stronghold.EnterpriseEstimating.Data;
using Stronghold.EnterpriseEstimating.Data.Models.Planning;

namespace Stronghold.EnterpriseEstimating.Api.Services.WorkOrders;

public static class WorkOrderFinancialService
{
    public static async Task<WorkOrderFinancialsResult> CalculateAsync(
        AppDbContext db, WorkOrder wo, string companyCode, CancellationToken ct)
    {
        var approvedFcoTotal = await db.FcoDocuments
            .Where(f => f.LinkedWorkOrderId == wo.WorkOrderId && f.CompanyCode == companyCode && f.Status == "Approved")
            .SumAsync(f => f.TotalFcoAmount, ct);

        var actuals = await db.ActualEntries
            .Where(a => a.WorkOrderId == wo.WorkOrderId && a.CompanyCode == companyCode && a.IsConfirmed)
            .ToListAsync(ct);

        var actualCostToDate = actuals.Sum(a => a.CostAmount);
        var billableToDate = actuals.Sum(a => a.BillableAmount);
        var billedToDate = actuals.Sum(a => a.BilledAmount);

        var revisedAuthorized = wo.AuthorizedValue + approvedFcoTotal;
        var unbilledEntitlement = billableToDate - billedToDate;
        var remainingAuthorized = revisedAuthorized - actualCostToDate;
        var margin = billableToDate - actualCostToDate;
        var marginPct = billableToDate > 0 ? Math.Round(margin / billableToDate * 100, 1) : 0m;

        return new WorkOrderFinancialsResult(
            wo.WorkOrderId,
            wo.AuthorizedValue,
            approvedFcoTotal,
            revisedAuthorized,
            actualCostToDate,
            billableToDate,
            billedToDate,
            unbilledEntitlement,
            remainingAuthorized,
            margin,
            marginPct);
    }
}

public record WorkOrderFinancialsResult(
    int WorkOrderId,
    decimal AuthorizedValue,
    decimal ApprovedFcoTotal,
    decimal RevisedAuthorized,
    decimal ActualCostToDate,
    decimal BillableToDate,
    decimal BilledToDate,
    decimal UnbilledEntitlement,
    decimal RemainingAuthorized,
    decimal Margin,
    decimal MarginPct);
