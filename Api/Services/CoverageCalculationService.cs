using Microsoft.EntityFrameworkCore;
using Stronghold.EnterpriseEstimating.Data;

namespace Stronghold.EnterpriseEstimating.Api.Services;

public record CoverageDto(
    string CraftCode,
    string CraftTitle,
    int DemandCount,
    int AssignedCount,
    int Gap,
    double GapPct
);

public class CoverageCalculationService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public CoverageCalculationService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    /// <summary>
    /// Returns coverage gap analysis per craft for the company.
    /// DemandCount = labor rows per craft in approved staffing plans (one row = one position).
    /// AssignedCount = distinct resources with active assignments per craft.
    /// </summary>
    public async Task<List<CoverageDto>> GetCoverageAsync(string companyCode, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var today = DateTime.UtcNow.Date;

        // Demand: count labor rows per craft from approved, unconverted staffing plans
        var demandByCraft = await db.StaffingLaborRows
            .Join(db.StaffingPlans,
                r => r.StaffingPlanId,
                sp => sp.StaffingPlanId,
                (r, sp) => new { sp.CompanyCode, sp.Status, sp.ConvertedEstimateId, r.CraftCode })
            .Where(x => x.CompanyCode == companyCode &&
                        x.Status == "Approved" &&
                        x.ConvertedEstimateId == null &&
                        x.CraftCode != null)
            .GroupBy(x => x.CraftCode!)
            .Select(g => new { CraftCode = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        // Assigned: distinct resources per craft with active assignments today
        var assignedByCraft = await db.Assignments
            .Where(a => a.CompanyCode == companyCode &&
                        a.Status != "Cancelled" &&
                        a.Start <= today &&
                        a.End >= today)
            .GroupBy(a => a.CraftCode)
            .Select(g => new { CraftCode = g.Key, Count = g.Select(a => a.ResourceId).Distinct().Count() })
            .ToListAsync(ct);

        var demandDict = demandByCraft.ToDictionary(x => x.CraftCode, x => x.Count);
        var assignedDict = assignedByCraft.ToDictionary(x => x.CraftCode, x => x.Count);

        var allCraftCodes = demandDict.Keys.Union(assignedDict.Keys).ToList();

        var crafts = await db.Crafts
            .Where(c => allCraftCodes.Contains(c.CraftCode))
            .ToDictionaryAsync(c => c.CraftCode, c => c.Title, ct);

        var result = new List<CoverageDto>();
        foreach (var craftCode in allCraftCodes.OrderBy(c => c))
        {
            var demand = demandDict.GetValueOrDefault(craftCode, 0);
            var assigned = assignedDict.GetValueOrDefault(craftCode, 0);
            var gap = Math.Max(0, demand - assigned);
            var gapPct = demand > 0 ? Math.Round((double)gap / demand * 100, 1) : 0;
            var title = crafts.GetValueOrDefault(craftCode, craftCode);
            result.Add(new CoverageDto(craftCode, title, demand, assigned, gap, gapPct));
        }

        return result.OrderByDescending(c => c.Gap).ToList();
    }

    /// <summary>
    /// Returns count of crafts with at least one open position.
    /// Used by portal and scheduling dashboard KPIs.
    /// </summary>
    public async Task<int> GetShortageCountAsync(string companyCode, CancellationToken ct = default)
    {
        var coverage = await GetCoverageAsync(companyCode, ct);
        return coverage.Count(c => c.Gap > 0);
    }
}
