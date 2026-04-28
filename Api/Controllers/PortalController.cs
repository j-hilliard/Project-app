using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stronghold.EnterpriseEstimating.Api.Services;
using Stronghold.EnterpriseEstimating.Data;

namespace Stronghold.EnterpriseEstimating.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/portal")]
[Authorize]
public class PortalController : ControllerBase
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly CoverageCalculationService _coverageService;

    public PortalController(IDbContextFactory<AppDbContext> dbFactory, CoverageCalculationService coverageService)
    {
        _dbFactory = dbFactory;
        _coverageService = coverageService;
    }

    private string CompanyCode => User.FindFirst("company_code")?.Value ?? string.Empty;

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var company = CompanyCode;
        var today = DateTime.UtcNow.Date;
        var soonThreshold = today.AddDays(7);

        var openEstimateCount = await db.Estimates
            .Where(e => e.CompanyCode == company && (e.Status == "Awarded" || e.Status == "Pending" || e.Status == "Draft"))
            .CountAsync(ct);

        var recentEstimates = await db.Estimates
            .Where(e => e.CompanyCode == company)
            .OrderByDescending(e => e.EstimateId)
            .Take(5)
            .Select(e => new
            {
                e.EstimateId,
                e.EstimateNumber,
                e.Name,
                e.Client,
                e.Status,
            })
            .ToListAsync(ct);

        var pendingFcoCount = await db.FcoDocuments
            .Where(f => f.CompanyCode == company && f.Status == "Submitted")
            .CountAsync(ct);

        var craftShortagesCount = await _coverageService.GetShortageCountAsync(company, ct);

        var jobsEndingSoonCount = await db.Assignments
            .Where(a => a.CompanyCode == company &&
                        a.Status != "Cancelled" &&
                        a.End >= today &&
                        a.End <= soonThreshold)
            .Select(a => a.ResourceId)
            .Distinct()
            .CountAsync(ct);

        var peopleFreeSoonCount = await db.Assignments
            .Where(a => a.CompanyCode == company && a.Status != "Cancelled" && a.End >= today)
            .GroupBy(a => a.ResourceId)
            .Where(g => g.Max(a => a.End) <= soonThreshold)
            .CountAsync(ct);

        var alertCount = craftShortagesCount + (pendingFcoCount > 0 ? 1 : 0);

        return Ok(new
        {
            openEstimateCount,
            pendingFcoCount,
            craftShortagesCount,
            jobsEndingSoonCount,
            peopleFreeSoonCount,
            alertCount,
            recentEstimates,
        });
    }
}
