using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    public async Task<IActionResult> CreatePlan([FromBody] StepOutPlan plan, CancellationToken ct)
    {
        plan.CompanyCode = CompanyCode;
        plan.CreatedBy = Username;
        plan.CreatedAt = DateTimeOffset.UtcNow;
        plan.UpdatedAt = DateTimeOffset.UtcNow;
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
    public async Task<IActionResult> UpdatePlan(int id, [FromBody] StepOutPlan update, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var plan = await db.StepOutPlans.FirstOrDefaultAsync(p => p.PlanId == id && p.CompanyCode == CompanyCode, ct);
        if (plan == null) return NotFound();
        plan.Name = update.Name;
        plan.Status = update.Status;
        plan.Client = update.Client;
        plan.Site = update.Site;
        plan.PlannedStart = update.PlannedStart;
        plan.PlannedEnd = update.PlannedEnd;
        plan.Notes = update.Notes;
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
    public async Task<IActionResult> AddStep(int planId, [FromBody] StepOutStep step, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var plan = await db.StepOutPlans.FirstOrDefaultAsync(p => p.PlanId == planId && p.CompanyCode == CompanyCode, ct);
        if (plan == null) return NotFound();
        step.PlanId = planId;
        db.StepOutSteps.Add(step);
        await db.SaveChangesAsync(ct);
        return Ok(step);
    }

    [HttpPut("step-out-plans/{planId:int}/steps/{stepId:int}")]
    public async Task<IActionResult> UpdateStep(int planId, int stepId, [FromBody] StepOutStep update, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var step = await db.StepOutSteps.FirstOrDefaultAsync(s => s.StepId == stepId && s.PlanId == planId, ct);
        if (step == null) return NotFound();
        step.StepCode = update.StepCode;
        step.SortOrder = update.SortOrder;
        step.Title = update.Title;
        step.Description = update.Description;
        step.DurationMinutes = update.DurationMinutes;
        step.RequiredPeople = update.RequiredPeople;
        step.CraftCode = update.CraftCode;
        step.IsParallel = update.IsParallel;
        step.PermitRequired = update.PermitRequired;
        step.MaterialToolRequired = update.MaterialToolRequired;
        step.Area = update.Area;
        step.Status = update.Status;
        step.PlannedStart = update.PlannedStart;
        step.PlannedEnd = update.PlannedEnd;
        step.Notes = update.Notes;
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

        // Resolve WO and Project nav info (labels only — no financial fields exposed)
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
    public async Task<IActionResult> UpdateWorkPackage(int id, [FromBody] WorkPackage update, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var wp = await db.WorkPackages.FirstOrDefaultAsync(w => w.PackageId == id && w.CompanyCode == CompanyCode, ct);
        if (wp == null) return NotFound();
        wp.Title = update.Title;
        wp.CraftCode = update.CraftCode;
        wp.RequiredPeople = update.RequiredPeople;
        wp.PlannedStart = update.PlannedStart;
        wp.PlannedEnd = update.PlannedEnd;
        wp.Status = update.Status;
        wp.ReadyForScheduling = update.ReadyForScheduling;
        wp.Area = update.Area;
        wp.Location = update.Location;
        wp.PermitRequired = update.PermitRequired;
        wp.PermitNumber = update.PermitNumber;
        wp.PermitStatus = update.PermitStatus;
        wp.JsaRequired = update.JsaRequired;
        wp.JsaStatus = update.JsaStatus;
        wp.Notes = update.Notes;
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
    public async Task<IActionResult> CreateFco([FromBody] FcoDocument fco, CancellationToken ct)
    {
        fco.CompanyCode = CompanyCode;
        fco.CreatedBy = Username;
        fco.CreatedAt = DateTimeOffset.UtcNow;
        fco.UpdatedAt = DateTimeOffset.UtcNow;
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
    public async Task<IActionResult> UpdateFco(int id, [FromBody] FcoDocument update, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var fco = await db.FcoDocuments.FirstOrDefaultAsync(f => f.FcoDocumentId == id && f.CompanyCode == CompanyCode, ct);
        if (fco == null) return NotFound();
        fco.Title = update.Title;
        fco.ScopeDescription = update.ScopeDescription;
        fco.Reason = update.Reason;
        fco.ScheduleImpactDays = update.ScheduleImpactDays;
        fco.RevisedCompletionDate = update.RevisedCompletionDate;
        fco.LaborBreakdownJson = update.LaborBreakdownJson;
        fco.MaterialBreakdownJson = update.MaterialBreakdownJson;
        fco.EquipmentBreakdownJson = update.EquipmentBreakdownJson;
        fco.MarkupPct = update.MarkupPct;
        fco.TaxPct = update.TaxPct;
        fco.TotalFcoAmount = update.TotalFcoAmount;
        fco.UpdatedContractValue = update.UpdatedContractValue;
        fco.Status = update.Status;
        fco.ApprovalNotes = update.ApprovalNotes;
        fco.ClientApprovalName = update.ClientApprovalName;
        fco.ClientApprovalDate = update.ClientApprovalDate;
        fco.ContractorApprovalName = update.ContractorApprovalName;
        fco.ContractorApprovalDate = update.ContractorApprovalDate;
        fco.RevisionHistory = update.RevisionHistory;
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

        var html = BuildFcoHtml(fco);
        return Content(html, "text/html");
    }

    private static string BuildFcoHtml(FcoDocument fco)
    {
        var total = fco.TotalFcoAmount.ToString("C");
        var updatedContract = fco.UpdatedContractValue.HasValue ? fco.UpdatedContractValue.Value.ToString("C") : "&#8212;";
        var status = fco.Status.ToUpperInvariant();
        var taxDisplay = fco.TaxPct.HasValue ? fco.TaxPct.Value.ToString("P1") : "&#8212;";
        var revCompletion = fco.RevisedCompletionDate?.ToString("MM/dd/yyyy") ?? "&#8212;";
        var estRef = fco.LinkedEstimateId?.ToString() ?? "&#8212;";
        var clientApprovalDate = fco.ClientApprovalDate?.ToString("MM/dd/yyyy") ?? "________________________________";
        var contractorApprovalDate = fco.ContractorApprovalDate?.ToString("MM/dd/yyyy") ?? "________________________________";
        var dateDisplay = fco.Date.ToString("MMMM d, yyyy");
        var generatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm");
        var revisionSection = string.IsNullOrEmpty(fco.RevisionHistory) ? "" :
            $"<div class=\"section-title\">Revision History</div><table><tr><td>{fco.RevisionHistory}</td></tr></table>";

        return $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
            <meta charset="UTF-8"/>
            <style>
              body { font-family: Arial, sans-serif; font-size: 11pt; margin: 40px; color: #111; }
              h1 { font-size: 18pt; text-align: center; margin: 0 0 4px; }
              .subtitle { text-align: center; color: #555; margin: 0 0 24px; font-size: 10pt; }
              .status-badge { display: inline-block; padding: 3px 12px; border-radius: 4px;
                font-size: 9pt; font-weight: bold; letter-spacing: .05em;
                background: #fef9c3; color: #854d0e; border: 1px solid #fcd34d; }
              table { width: 100%; border-collapse: collapse; margin-bottom: 16px; }
              td, th { border: 1px solid #ccc; padding: 6px 10px; font-size: 10pt; vertical-align: top; }
              th { background: #f3f4f6; font-weight: 600; width: 30%; }
              .section-title { background: #1e3a5f; color: #fff; font-weight: bold;
                font-size: 10pt; padding: 5px 10px; letter-spacing: .06em; text-transform: uppercase; }
              .sig-box { border: 1px solid #ccc; height: 60px; margin-top: 4px; }
              .footer { margin-top: 32px; font-size: 9pt; color: #777; text-align: center; border-top: 1px solid #ccc; padding-top: 8px; }
            </style>
            </head>
            <body>
            <h1>FIELD CHANGE ORDER</h1>
            <div class="subtitle">FCO #{{fco.FcoNumber}} &nbsp;|&nbsp; {{dateDisplay}} &nbsp;|&nbsp; <span class="status-badge">{{status}}</span></div>

            <div class="section-title">Project Information</div>
            <table>
              <tr><th>Project Name</th><td>{{fco.ProjectName ?? "&#8212;"}}</td><th>Project Address</th><td>{{fco.ProjectAddress ?? "&#8212;"}}</td></tr>
              <tr><th>Original Contract/Estimate</th><td>#{{estRef}}</td><th>FCO Number</th><td>{{fco.FcoNumber}}</td></tr>
            </table>

            <div class="section-title">Parties</div>
            <table>
              <tr><th>Client</th><td>{{fco.ClientName ?? "&#8212;"}}</td><th>Client Contact</th><td>{{fco.ClientContact ?? "&#8212;"}}</td></tr>
              <tr><th>Contractor</th><td>{{fco.ContractorName ?? "&#8212;"}}</td><th>Contractor Contact</th><td>{{fco.ContractorContact ?? "&#8212;"}}</td></tr>
              <tr><th>Requested By</th><td>{{fco.RequestedBy ?? "&#8212;"}}</td><th>Prepared By</th><td>{{fco.PreparedBy ?? "&#8212;"}}</td></tr>
            </table>

            <div class="section-title">Scope of Change</div>
            <table>
              <tr><th>Description</th><td colspan="3">{{fco.ScopeDescription ?? "&#8212;"}}</td></tr>
              <tr><th>Reason / Basis</th><td colspan="3">{{fco.Reason ?? "&#8212;"}}</td></tr>
              <tr><th>Schedule Impact</th><td>{{fco.ScheduleImpactDays}} days added</td><th>Revised Completion</th><td>{{revCompletion}}</td></tr>
            </table>

            <div class="section-title">Cost Summary</div>
            <table>
              <tr><th>Labor Breakdown</th><td>{{fco.LaborBreakdownJson ?? "See attached"}}</td></tr>
              <tr><th>Material Breakdown</th><td>{{fco.MaterialBreakdownJson ?? "See attached"}}</td></tr>
              <tr><th>Equipment Breakdown</th><td>{{fco.EquipmentBreakdownJson ?? "See attached"}}</td></tr>
              <tr><th>Markup / Overhead / Profit</th><td>{{fco.MarkupPct:P1}}</td></tr>
              <tr><th>Tax</th><td>{{taxDisplay}}</td></tr>
              <tr><th>Total FCO Amount</th><td><strong>{{total}}</strong></td></tr>
              <tr><th>Updated Contract / Estimate Value</th><td><strong>{{updatedContract}}</strong></td></tr>
            </table>

            <div class="section-title">Approvals</div>
            <table>
              <tr>
                <td style="width:50%">
                  <strong>Client Approval</strong><br/>
                  Name: {{fco.ClientApprovalName ?? "________________________________"}}<br/>
                  Date: {{clientApprovalDate}}<br/>
                  <div class="sig-box"></div>
                </td>
                <td style="width:50%">
                  <strong>Contractor Approval</strong><br/>
                  Name: {{fco.ContractorApprovalName ?? "________________________________"}}<br/>
                  Date: {{contractorApprovalDate}}<br/>
                  <div class="sig-box"></div>
                </td>
              </tr>
            </table>

            {{revisionSection}}

            <div class="footer">
              Generated by Stronghold Platform &mdash; {{generatedAt}} UTC
            </div>
            </body>
            </html>
            """;
    }
}
