using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stronghold.EnterpriseEstimating.Api.Contracts.Planning;
using Stronghold.EnterpriseEstimating.Api.Services.Planning;
using Stronghold.EnterpriseEstimating.Data;
using Stronghold.EnterpriseEstimating.Data.Models.Planning;

namespace Stronghold.EnterpriseEstimating.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/planning")]
[Authorize]
public class PlanningController : ControllerBase
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public PlanningController(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    private string CompanyCode => User.FindFirst("company_code")?.Value ?? string.Empty;
    private string Username => User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                            ?? User.FindFirst("username")?.Value ?? string.Empty;

    // ── Step-Out Plans ─────────────────────────────────────────────────────
    [HttpGet("step-out-plans")]
    public async Task<IActionResult> ListPlans([FromQuery] string? status, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var q = db.StepOutPlans.Where(p => p.CompanyCode == CompanyCode);
        if (status != null) q = q.Where(p => p.Status == status);
        var list = await q.OrderByDescending(p => p.PlanId).ToListAsync(ct);
        return Ok(list);
    }

    [HttpPost("step-out-plans")]
    public async Task<IActionResult> CreatePlan([FromBody] CreateStepOutPlanRequest req, CancellationToken ct)
    {
        var plan = new StepOutPlan
        {
            Name = req.Name,
            Client = req.Client,
            Site = req.Site,
            PlannedStart = req.PlannedStart,
            PlannedEnd = req.PlannedEnd,
            Notes = req.Notes,
            WorkOrderId = req.WorkOrderId,
            Status = req.Status,
            CompanyCode = CompanyCode,
            CreatedBy = Username,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        db.StepOutPlans.Add(plan);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetPlan), new { id = plan.PlanId }, plan);
    }

    [HttpGet("step-out-plans/{id:int}")]
    public async Task<IActionResult> GetPlan(int id, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var plan = await db.StepOutPlans
            .Include(p => p.Steps.OrderBy(s => s.SortOrder))
                .ThenInclude(s => s.Dependencies)
            .Include(p => p.Steps)
                .ThenInclude(s => s.ResourceRequirements)
            .Include(p => p.WorkPackages)
            .FirstOrDefaultAsync(p => p.PlanId == id && p.CompanyCode == CompanyCode, ct);
        return plan == null ? NotFound() : Ok(plan);
    }

    [HttpPut("step-out-plans/{id:int}")]
    public async Task<IActionResult> UpdatePlan(int id, [FromBody] UpdateStepOutPlanRequest req, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var plan = await db.StepOutPlans.FirstOrDefaultAsync(p => p.PlanId == id && p.CompanyCode == CompanyCode, ct);
        if (plan == null) return NotFound();
        plan.Name = req.Name;
        plan.Status = req.Status;
        plan.Client = req.Client;
        plan.Site = req.Site;
        plan.PlannedStart = req.PlannedStart;
        plan.PlannedEnd = req.PlannedEnd;
        plan.Notes = req.Notes;
        plan.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(plan);
    }

    [HttpDelete("step-out-plans/{id:int}")]
    public async Task<IActionResult> DeletePlan(int id, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var plan = await db.StepOutPlans.FirstOrDefaultAsync(p => p.PlanId == id && p.CompanyCode == CompanyCode, ct);
        if (plan == null) return NotFound();
        db.StepOutPlans.Remove(plan);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── Steps ─────────────────────────────────────────────────────────────
    [HttpPost("step-out-plans/{planId:int}/steps")]
    public async Task<IActionResult> AddStep(int planId, [FromBody] CreateStepOutStepRequest req, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var plan = await db.StepOutPlans.FirstOrDefaultAsync(p => p.PlanId == planId && p.CompanyCode == CompanyCode, ct);
        if (plan == null) return NotFound();
        var step = new StepOutStep
        {
            PlanId = planId,
            StepCode = req.StepCode,
            SortOrder = req.SortOrder,
            Title = req.Title,
            Description = req.Description,
            CraftCode = req.CraftCode,
            RequiredPeople = req.RequiredPeople,
            DurationMinutes = req.DurationMinutes,
            IsParallel = req.IsParallel,
            PermitRequired = req.PermitRequired,
            MaterialToolRequired = req.MaterialToolRequired,
            Area = req.Area,
            Status = req.Status,
            PlannedStart = req.PlannedStart,
            PlannedEnd = req.PlannedEnd,
            Notes = req.Notes,
        };
        db.StepOutSteps.Add(step);
        await db.SaveChangesAsync(ct);
        return Ok(step);
    }

    [HttpPut("step-out-plans/{planId:int}/steps/{stepId:int}")]
    public async Task<IActionResult> UpdateStep(int planId, int stepId, [FromBody] UpdateStepOutStepRequest req, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var step = await db.StepOutSteps.FirstOrDefaultAsync(s => s.StepId == stepId && s.PlanId == planId, ct);
        if (step == null) return NotFound();
        step.StepCode = req.StepCode;
        step.SortOrder = req.SortOrder;
        step.Title = req.Title;
        step.Description = req.Description;
        step.DurationMinutes = req.DurationMinutes;
        step.RequiredPeople = req.RequiredPeople;
        step.CraftCode = req.CraftCode;
        step.IsParallel = req.IsParallel;
        step.PermitRequired = req.PermitRequired;
        step.MaterialToolRequired = req.MaterialToolRequired;
        step.Area = req.Area;
        step.Status = req.Status;
        step.PlannedStart = req.PlannedStart;
        step.PlannedEnd = req.PlannedEnd;
        step.Notes = req.Notes;
        await db.SaveChangesAsync(ct);
        return Ok(step);
    }

    [HttpDelete("step-out-plans/{planId:int}/steps/{stepId:int}")]
    public async Task<IActionResult> DeleteStep(int planId, int stepId, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var step = await db.StepOutSteps.FirstOrDefaultAsync(s => s.StepId == stepId && s.PlanId == planId, ct);
        if (step == null) return NotFound();
        db.StepOutSteps.Remove(step);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── Generate Work Packages from Plan ──────────────────────────────────
    [HttpPost("step-out-plans/{planId:int}/generate-work-packages")]
    public async Task<IActionResult> GenerateWorkPackages(int planId, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var plan = await db.StepOutPlans
            .Include(p => p.Steps)
            .FirstOrDefaultAsync(p => p.PlanId == planId && p.CompanyCode == CompanyCode, ct);
        if (plan == null) return NotFound();

        var generated = new List<WorkPackage>();
        foreach (var step in plan.Steps.Where(s => !string.IsNullOrEmpty(s.CraftCode)))
        {
            var wp = new WorkPackage
            {
                CompanyCode = CompanyCode,
                PlanId = planId,
                SourceType = "StepOutPlan",
                SourceId = planId,
                Title = $"{plan.Name} — {step.Title}",
                CraftCode = step.CraftCode,
                RequiredPeople = step.RequiredPeople,
                PlannedStart = step.PlannedStart,
                PlannedEnd = step.PlannedEnd,
                Status = "Draft",
                ReadyForScheduling = false,
                CreatedBy = Username,
            };
            db.WorkPackages.Add(wp);
            generated.Add(wp);
        }

        await db.SaveChangesAsync(ct);
        return Ok(new { generated = generated.Count, workPackages = generated });
    }

    // ── Work Packages ─────────────────────────────────────────────────────
    [HttpGet("work-packages")]
    public async Task<IActionResult> ListWorkPackages([FromQuery] bool? readyForScheduling, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var q = db.WorkPackages.Where(wp => wp.CompanyCode == CompanyCode);
        if (readyForScheduling.HasValue) q = q.Where(wp => wp.ReadyForScheduling == readyForScheduling.Value);
        return Ok(await q.OrderByDescending(wp => wp.PackageId).ToListAsync(ct));
    }

    [HttpGet("work-packages/{id:int}")]
    public async Task<IActionResult> GetWorkPackage(int id, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var wp = await db.WorkPackages
            .Include(w => w.Plan)
            .FirstOrDefaultAsync(w => w.PackageId == id && w.CompanyCode == CompanyCode, ct);
        if (wp == null) return NotFound();

        string? workOrderNumber = null, workOrderTitle = null, workOrderStatus = null;
        string? projectNumber = null, projectName = null;
        int? projectId = null;

        if (wp.WorkOrderId.HasValue)
        {
            var wo = await db.WorkOrders
                .Include(w => w.Project)
                .FirstOrDefaultAsync(w => w.WorkOrderId == wp.WorkOrderId.Value && w.CompanyCode == CompanyCode, ct);
            if (wo != null)
            {
                workOrderNumber = wo.WorkOrderNumber;
                workOrderTitle = wo.Title;
                workOrderStatus = wo.Status;
                projectId = wo.ProjectId;
                projectNumber = wo.Project?.ProjectNumber;
                projectName = wo.Project?.Name;
            }
        }

        return Ok(new
        {
            wp.PackageId,
            wp.CompanyCode,
            wp.PlanId,
            planName = wp.Plan?.Name,
            wp.WorkOrderId,
            workOrderNumber,
            workOrderTitle,
            workOrderStatus,
            projectId,
            projectNumber,
            projectName,
            wp.SourceType,
            wp.SourceId,
            wp.Title,
            wp.CraftCode,
            wp.RequiredPeople,
            wp.PlannedStart,
            wp.PlannedEnd,
            wp.Status,
            wp.ReadyForScheduling,
            wp.Area,
            wp.Location,
            wp.PermitRequired,
            wp.PermitNumber,
            wp.PermitStatus,
            wp.JsaRequired,
            wp.JsaStatus,
            wp.Notes,
            wp.CreatedBy,
            wp.CreatedAt,
            wp.UpdatedAt,
        });
    }

    [HttpPut("work-packages/{id:int}")]
    public async Task<IActionResult> UpdateWorkPackage(int id, [FromBody] UpdateWorkPackageRequest req, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var wp = await db.WorkPackages.FirstOrDefaultAsync(w => w.PackageId == id && w.CompanyCode == CompanyCode, ct);
        if (wp == null) return NotFound();
        wp.Title = req.Title;
        wp.CraftCode = req.CraftCode;
        wp.RequiredPeople = req.RequiredPeople;
        wp.PlannedStart = req.PlannedStart;
        wp.PlannedEnd = req.PlannedEnd;
        wp.Status = req.Status;
        wp.ReadyForScheduling = req.ReadyForScheduling;
        wp.Area = req.Area;
        wp.Location = req.Location;
        wp.PermitRequired = req.PermitRequired;
        wp.PermitNumber = req.PermitNumber;
        wp.PermitStatus = req.PermitStatus;
        wp.JsaRequired = req.JsaRequired;
        wp.JsaStatus = req.JsaStatus;
        wp.Notes = req.Notes;
        wp.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(wp);
    }

    // ── FCO Documents ─────────────────────────────────────────────────────
    [HttpGet("fco")]
    public async Task<IActionResult> ListFco([FromQuery] string? status, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var q = db.FcoDocuments.Where(f => f.CompanyCode == CompanyCode);
        if (status != null) q = q.Where(f => f.Status == status);
        return Ok(await q.OrderByDescending(f => f.FcoDocumentId).ToListAsync(ct));
    }

    [HttpPost("fco")]
    public async Task<IActionResult> CreateFco([FromBody] CreateFcoDocumentRequest req, CancellationToken ct)
    {
        var fco = new FcoDocument
        {
            FcoNumber = req.FcoNumber,
            Title = req.Title,
            ScopeDescription = req.ScopeDescription,
            Reason = req.Reason,
            RequestedBy = req.RequestedBy,
            PreparedBy = req.PreparedBy,
            ScheduleImpactDays = req.ScheduleImpactDays,
            UpdatedContractValue = req.UpdatedContractValue,
            Date = req.Date ?? DateTime.UtcNow,
            LinkedWorkOrderId = req.LinkedWorkOrderId,
            Status = "Draft",
            CompanyCode = CompanyCode,
            CreatedBy = Username,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        db.FcoDocuments.Add(fco);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetFco), new { id = fco.FcoDocumentId }, fco);
    }

    [HttpGet("fco/{id:int}")]
    public async Task<IActionResult> GetFco(int id, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var fco = await db.FcoDocuments.FirstOrDefaultAsync(f => f.FcoDocumentId == id && f.CompanyCode == CompanyCode, ct);
        return fco == null ? NotFound() : Ok(fco);
    }

    [HttpPut("fco/{id:int}")]
    public async Task<IActionResult> UpdateFco(int id, [FromBody] UpdateFcoDocumentRequest req, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var fco = await db.FcoDocuments.FirstOrDefaultAsync(f => f.FcoDocumentId == id && f.CompanyCode == CompanyCode, ct);
        if (fco == null) return NotFound();
        fco.Title = req.Title;
        fco.ScopeDescription = req.ScopeDescription;
        fco.Reason = req.Reason;
        fco.ScheduleImpactDays = req.ScheduleImpactDays;
        fco.RevisedCompletionDate = req.RevisedCompletionDate;
        fco.LaborBreakdownJson = req.LaborBreakdownJson;
        fco.MaterialBreakdownJson = req.MaterialBreakdownJson;
        fco.EquipmentBreakdownJson = req.EquipmentBreakdownJson;
        fco.MarkupPct = req.MarkupPct;
        fco.TaxPct = req.TaxPct;
        fco.TotalFcoAmount = req.TotalFcoAmount;
        fco.UpdatedContractValue = req.UpdatedContractValue;
        fco.Status = req.Status;
        fco.ApprovalNotes = req.ApprovalNotes;
        fco.ClientApprovalName = req.ClientApprovalName;
        fco.ClientApprovalDate = req.ClientApprovalDate;
        fco.ContractorApprovalName = req.ContractorApprovalName;
        fco.ContractorApprovalDate = req.ContractorApprovalDate;
        fco.RevisionHistory = req.RevisionHistory;
        fco.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(fco);
    }

    // ── FCO Document Generation ───────────────────────────────────────────
    [HttpPost("fco/{id:int}/generate-document")]
    public async Task<IActionResult> GenerateFcoDocument(int id, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var fco = await db.FcoDocuments.FirstOrDefaultAsync(f => f.FcoDocumentId == id && f.CompanyCode == CompanyCode, ct);
        if (fco == null) return NotFound();
        var html = FcoDocumentService.BuildHtml(fco);
        return Content(html, "text/html");
    }
}
