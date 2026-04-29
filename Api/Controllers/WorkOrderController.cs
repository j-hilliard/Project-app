using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stronghold.EnterpriseEstimating.Data;
using Stronghold.EnterpriseEstimating.Data.Models.Planning;

namespace Stronghold.EnterpriseEstimating.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/work-orders")]
[Authorize]
public class WorkOrderController : ControllerBase
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public WorkOrderController(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    private string CompanyCode => User.FindFirst("company_code")?.Value ?? string.Empty;
    private string Username => User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                            ?? User.FindFirst("username")?.Value ?? string.Empty;

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int? projectId,
        [FromQuery] int? estimateId,
        [FromQuery] string? status,
        CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var q = db.WorkOrders.Where(wo => wo.CompanyCode == CompanyCode);
        if (projectId.HasValue) q = q.Where(wo => wo.ProjectId == projectId.Value);
        if (estimateId.HasValue) q = q.Where(wo => wo.EstimateId == estimateId.Value);
        if (status != null) q = q.Where(wo => wo.Status == status);
        return Ok(await q.OrderByDescending(wo => wo.WorkOrderId).ToListAsync(ct));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var wo = await db.WorkOrders
            .Include(w => w.StepOutPlans)
            .Include(w => w.WorkPackages)
            .FirstOrDefaultAsync(w => w.WorkOrderId == id && w.CompanyCode == CompanyCode, ct);
        return wo == null ? NotFound() : Ok(wo);
    }

    [HttpGet("{id:int}/financials")]
    public async Task<IActionResult> GetFinancials(int id, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var wo = await db.WorkOrders
            .FirstOrDefaultAsync(w => w.WorkOrderId == id && w.CompanyCode == CompanyCode, ct);
        if (wo == null) return NotFound();

        var approvedFcoTotal = await db.FcoDocuments
            .Where(f => f.LinkedWorkOrderId == id && f.CompanyCode == CompanyCode && f.Status == "Approved")
            .SumAsync(f => f.TotalFcoAmount, ct);

        var actuals = await db.ActualEntries
            .Where(a => a.WorkOrderId == id && a.CompanyCode == CompanyCode && a.IsConfirmed)
            .ToListAsync(ct);

        var actualCostToDate = actuals.Sum(a => a.CostAmount);
        var billableToDate = actuals.Sum(a => a.BillableAmount);
        var billedToDate = actuals.Sum(a => a.BilledAmount);

        var revisedAuthorized = wo.AuthorizedValue + approvedFcoTotal;
        var unbilledEntitlement = billableToDate - billedToDate;
        var remainingAuthorized = revisedAuthorized - actualCostToDate;
        var margin = billableToDate - actualCostToDate;
        var marginPct = billableToDate > 0 ? Math.Round(margin / billableToDate * 100, 1) : 0m;

        return Ok(new
        {
            workOrderId = id,
            authorizedValue = wo.AuthorizedValue,
            approvedFcoTotal,
            revisedAuthorized,
            actualCostToDate,
            billableToDate,
            billedToDate,
            unbilledEntitlement,
            remainingAuthorized,
            margin,
            marginPct,
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] WorkOrder wo, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var project = await db.Projects
            .FirstOrDefaultAsync(p => p.ProjectId == wo.ProjectId && p.CompanyCode == CompanyCode, ct);
        if (project == null) return BadRequest("Project not found.");

        var ca = await db.CommercialAuthorizations
            .FirstOrDefaultAsync(c => c.CommercialAuthorizationId == wo.CommercialAuthorizationId
                                   && c.CompanyCode == CompanyCode, ct);
        if (ca == null) return BadRequest("CommercialAuthorization not found.");

        wo.CompanyCode = CompanyCode;
        wo.CreatedBy = Username;
        wo.CreatedAt = DateTimeOffset.UtcNow;
        wo.UpdatedAt = DateTimeOffset.UtcNow;
        db.WorkOrders.Add(wo);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = wo.WorkOrderId }, wo);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] WorkOrder update, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var wo = await db.WorkOrders
            .FirstOrDefaultAsync(w => w.WorkOrderId == id && w.CompanyCode == CompanyCode, ct);
        if (wo == null) return NotFound();

        wo.Title = update.Title;
        wo.Description = update.Description;
        wo.Scope = update.Scope;
        wo.AuthorizedValue = update.AuthorizedValue;
        wo.PlannedStart = update.PlannedStart;
        wo.PlannedEnd = update.PlannedEnd;
        wo.ForecastEnd = update.ForecastEnd;
        wo.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(wo);
    }

    [HttpPatch("{id:int}/release")]
    public async Task<IActionResult> Release(int id, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var wo = await db.WorkOrders
            .Include(w => w.CommercialAuthorization)
            .FirstOrDefaultAsync(w => w.WorkOrderId == id && w.CompanyCode == CompanyCode, ct);
        if (wo == null) return NotFound();
        if (wo.Status != "Draft")
            return UnprocessableEntity(new { code = "InvalidStatus", message = $"WorkOrder status is '{wo.Status}' — only Draft can be Released." });

        // Gate 1: CommercialAuthorization must be Active
        if (wo.CommercialAuthorization?.Status != "Active")
            return UnprocessableEntity(new { code = "CommAuthNotActive", message = "CommercialAuthorization must be Active to release a WorkOrder." });

        // Gate 2: Estimate must be Awarded
        var estimateAwarded = await db.Estimates
            .AnyAsync(e => e.EstimateId == wo.EstimateId && e.Status == "Awarded" && e.CompanyCode == CompanyCode, ct);
        if (!estimateAwarded)
            return UnprocessableEntity(new { code = "EstimateNotAwarded", message = "The linked Estimate must be Awarded to release a WorkOrder." });

        // Gate 3: AuthorizedValue must be set
        if (wo.AuthorizedValue <= 0)
            return UnprocessableEntity(new { code = "AuthorizedValueRequired", message = "WorkOrder AuthorizedValue must be greater than zero before release." });

        // Gate 4: Sum of active/released WOs on same CommAuth must not exceed CommAuth.AuthorizedValue
        var commAuthValue = wo.CommercialAuthorization.AuthorizedValue;
        var alreadyAllocated = await db.WorkOrders
            .Where(w => w.CommercialAuthorizationId == wo.CommercialAuthorizationId
                     && w.WorkOrderId != id
                     && w.CompanyCode == CompanyCode
                     && (w.Status == "Released" || w.Status == "InProgress" || w.Status == "Complete"))
            .SumAsync(w => w.AuthorizedValue, ct);
        if (alreadyAllocated + wo.AuthorizedValue > commAuthValue)
            return UnprocessableEntity(new
            {
                code = "AuthorizedValueExceeded",
                message = $"Releasing this WorkOrder (${wo.AuthorizedValue:N2}) would exceed the CommercialAuthorization authorized value of ${commAuthValue:N2}. Already allocated: ${alreadyAllocated:N2}."
            });

        wo.Status = "Released";
        wo.ReleasedBy = Username;
        wo.ReleasedAt = DateTimeOffset.UtcNow;
        wo.ActualStart = DateTime.UtcNow;
        wo.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(wo);
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> SetStatus(int id, [FromBody] StatusUpdateRequest req, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var wo = await db.WorkOrders
            .FirstOrDefaultAsync(w => w.WorkOrderId == id && w.CompanyCode == CompanyCode, ct);
        if (wo == null) return NotFound();

        if ((req.Status == "Complete" || req.Status == "Closed") && wo.ActualEnd == null)
            wo.ActualEnd = DateTime.UtcNow;

        wo.Status = req.Status;
        wo.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(wo);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var wo = await db.WorkOrders
            .FirstOrDefaultAsync(w => w.WorkOrderId == id && w.CompanyCode == CompanyCode, ct);
        if (wo == null) return NotFound();
        if (wo.Status != "Draft") return BadRequest("Only Draft work orders can be deleted.");

        var hasActuals = await db.ActualEntries.AnyAsync(a => a.WorkOrderId == id, ct);
        if (hasActuals) return Conflict("WorkOrder has actual entries and cannot be deleted.");

        db.WorkOrders.Remove(wo);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
