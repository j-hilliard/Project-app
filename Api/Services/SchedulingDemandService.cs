using Microsoft.EntityFrameworkCore;
using Stronghold.EnterpriseEstimating.Data;

namespace Stronghold.EnterpriseEstimating.Api.Services;

public record JobDemandDto(
    string SourceType,
    int SourceId,
    string Name,
    string? Client,
    string? Site,
    DateTime? StartDate,
    DateTime? EndDate,
    string Status
);

public class SchedulingDemandService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public SchedulingDemandService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IReadOnlyList<JobDemandDto>> GetDemandAsync(string companyCode, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var estimates = await db.Estimates
            .Where(e => e.CompanyCode == companyCode &&
                        (e.Status == "Awarded" || e.Status == "Pending"))
            .Select(e => new JobDemandDto(
                "Estimate",
                e.EstimateId,
                e.Name,
                e.Client,
                e.Site,
                e.StartDate,
                e.EndDate,
                e.Status))
            .ToListAsync(ct);

        // Approved staffing plans that have NOT been converted to an estimate
        var staffingPlans = await db.StaffingPlans
            .Where(sp => sp.CompanyCode == companyCode &&
                         sp.ConvertedEstimateId == null &&
                         sp.Status == "Approved")
            .Select(sp => new JobDemandDto(
                "StaffingPlan",
                sp.StaffingPlanId,
                sp.Name,
                sp.Client,
                null,
                sp.StartDate,
                sp.EndDate,
                sp.Status))
            .ToListAsync(ct);

        var workPackages = await db.WorkPackages
            .Where(wp => wp.CompanyCode == companyCode && wp.ReadyForScheduling)
            .Select(wp => new JobDemandDto(
                "WorkPackage",
                wp.PackageId,
                wp.Title,
                null,
                null,
                wp.PlannedStart,
                wp.PlannedEnd,
                wp.Status))
            .ToListAsync(ct);

        return estimates
            .Concat(staffingPlans)
            .Concat(workPackages)
            .OrderBy(j => j.StartDate)
            .ToList();
    }
}
