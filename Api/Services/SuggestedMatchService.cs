using Microsoft.EntityFrameworkCore;
using Stronghold.EnterpriseEstimating.Data;

namespace Stronghold.EnterpriseEstimating.Api.Services;

public record SuggestedMatchDto(
    int ResourceId,
    string ResourceName,
    string CraftCode,
    string CraftTitle,
    string? Branch,
    string Reason,     // "RollingOff" | "Unassigned"
    DateTime? AvailableDate,
    int MatchScore
);

public class SuggestedMatchService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public SuggestedMatchService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    /// <summary>
    /// Returns resources that are a good fit for open demand gaps:
    /// - Resources whose last assignment ends within 14 days (rolling off)
    /// - Resources with no current assignment (unassigned)
    /// Ordered by match score descending.
    /// </summary>
    public async Task<List<SuggestedMatchDto>> GetSuggestedMatchesAsync(
        string companyCode,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var today = DateTime.UtcNow.Date;
        var rollOffThreshold = today.AddDays(14);

        var resources = await db.Resources
            .Include(r => r.Assignments.Where(a => a.CompanyCode == companyCode && a.Status != "Cancelled"))
            .Include(r => r.Craft)
            .Where(r => r.CompanyCode == companyCode && r.IsActive)
            .ToListAsync(ct);

        var crafts = await db.Crafts.ToDictionaryAsync(c => c.CraftCode, c => c.Title, ct);

        var matches = new List<SuggestedMatchDto>();

        foreach (var resource in resources)
        {
            var activeAssignments = resource.Assignments
                .Where(a => a.End >= today)
                .OrderByDescending(a => a.End)
                .ToList();

            if (!activeAssignments.Any())
            {
                // Unassigned: high value
                matches.Add(new SuggestedMatchDto(
                    resource.ResourceId,
                    resource.Name,
                    resource.CraftCode,
                    crafts.GetValueOrDefault(resource.CraftCode, resource.CraftCode),
                    resource.Branch,
                    "Unassigned",
                    today,
                    100
                ));
            }
            else
            {
                var lastEnd = activeAssignments.First().End;
                if (lastEnd <= rollOffThreshold)
                {
                    // Rolling off soon
                    var daysUntilFree = (lastEnd.Date - today).Days;
                    var score = 80 + Math.Max(0, 14 - daysUntilFree); // sooner = higher score
                    matches.Add(new SuggestedMatchDto(
                        resource.ResourceId,
                        resource.Name,
                        resource.CraftCode,
                        crafts.GetValueOrDefault(resource.CraftCode, resource.CraftCode),
                        resource.Branch,
                        "RollingOff",
                        lastEnd.Date,
                        score
                    ));
                }
            }
        }

        return matches.OrderByDescending(m => m.MatchScore).ToList();
    }

    /// <summary>
    /// Returns count of resources available or rolling off — used by dashboard KPIs.
    /// </summary>
    public async Task<int> GetAvailableSoonCountAsync(string companyCode, CancellationToken ct = default)
    {
        var matches = await GetSuggestedMatchesAsync(companyCode, ct);
        return matches.Count;
    }
}
