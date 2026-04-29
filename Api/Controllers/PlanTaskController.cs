using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stronghold.EnterpriseEstimating.Data;
using Stronghold.EnterpriseEstimating.Data.Models.Planning;

namespace Stronghold.EnterpriseEstimating.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/plan-tasks")]
[Authorize]
public class PlanTaskController : ControllerBase
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public PlanTaskController(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    private string CompanyCode => User.FindFirst("company_code")?.Value ?? string.Empty;
    private string Username => User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                            ?? User.FindFirst("username")?.Value ?? string.Empty;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int? phaseId, [FromQuery] string? status, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var q = db.PlanTasks.AsQueryable();
        if (phaseId.HasValue) q = q.Where(t => t.PhaseId == phaseId.Value);
        if (status != null) q = q.Where(t => t.Status == status);

        // Scope to this company via phase → project
        q = q.Where(t => db.ProjectPhases
            .Join(db.Projects.Where(p => p.CompanyCode == CompanyCode),
                  ph => ph.ProjectId, p => p.ProjectId, (ph, p) => ph.PhaseId)
            .Contains(t.PhaseId));

        return Ok(await q
            .Include(t => t.SubTasks.OrderBy(st => st.SortOrder))
            .OrderBy(t => t.SortOrder)
            .ToListAsync(ct));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var task = await db.PlanTasks
            .Include(t => t.SubTasks.OrderBy(st => st.SortOrder))
            .Include(t => t.SuccessorDependencies)
            .Include(t => t.PredecessorDependencies)
            .Include(t => t.Milestones)
            .FirstOrDefaultAsync(t => t.TaskId == id, ct);
        if (task == null) return NotFound();

        var phaseOwned = await db.ProjectPhases
            .AnyAsync(ph => ph.PhaseId == task.PhaseId
                         && db.Projects.Any(p => p.ProjectId == ph.ProjectId && p.CompanyCode == CompanyCode), ct);
        return phaseOwned ? Ok(task) : Forbid();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PlanTask task, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var phaseOwned = await db.ProjectPhases
            .AnyAsync(ph => ph.PhaseId == task.PhaseId
                         && db.Projects.Any(p => p.ProjectId == ph.ProjectId && p.CompanyCode == CompanyCode), ct);
        if (!phaseOwned) return BadRequest("Phase not found or not owned by this company.");

        task.CreatedBy = Username;
        task.CreatedAt = DateTimeOffset.UtcNow;
        task.UpdatedAt = DateTimeOffset.UtcNow;
        db.PlanTasks.Add(task);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = task.TaskId }, task);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] PlanTask update, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var task = await db.PlanTasks.FirstOrDefaultAsync(t => t.TaskId == id, ct);
        if (task == null) return NotFound();

        task.Title = update.Title;
        task.Description = update.Description;
        task.TaskType = update.TaskType;
        task.PlannedStart = update.PlannedStart;
        task.PlannedEnd = update.PlannedEnd;
        task.DurationDays = update.DurationDays;
        task.ForecastEnd = update.ForecastEnd;
        task.CraftCode = update.CraftCode;
        task.AssignedTo = update.AssignedTo;
        task.OwnerUserId = update.OwnerUserId;
        task.SortOrder = update.SortOrder;
        task.IsMilestone = update.IsMilestone;
        task.ParentTaskId = update.ParentTaskId;
        task.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(task);
    }

    [HttpPatch("{id:int}/progress")]
    public async Task<IActionResult> UpdateProgress(int id, [FromBody] ProgressUpdateRequest req, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var task = await db.PlanTasks.FirstOrDefaultAsync(t => t.TaskId == id, ct);
        if (task == null) return NotFound();

        task.PercentComplete = req.PercentComplete;
        if (req.ForecastEnd.HasValue) task.ForecastEnd = req.ForecastEnd;
        if (req.Status != null) task.Status = req.Status;
        if (task.Status == "InProgress" && task.ActualStart == null) task.ActualStart = DateTime.UtcNow;
        if (task.Status == "Complete" && task.ActualEnd == null) task.ActualEnd = DateTime.UtcNow;
        task.UpdatedAt = DateTimeOffset.UtcNow;

        // Snapshot
        db.TaskProgressSnapshots.Add(new TaskProgressSnapshot
        {
            TaskId = id,
            SnapshotDate = DateTime.UtcNow,
            PercentComplete = req.PercentComplete,
            ForecastEnd = req.ForecastEnd,
            Status = task.Status,
            ReportedBy = Username,
            Notes = req.Notes,
            CreatedAt = DateTimeOffset.UtcNow,
        });

        await db.SaveChangesAsync(ct);
        return Ok(task);
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> SetStatus(int id, [FromBody] TaskStatusRequest req, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var task = await db.PlanTasks.FirstOrDefaultAsync(t => t.TaskId == id, ct);
        if (task == null) return NotFound();

        // Soft gate: warn if moving to InProgress with no estimate link, unless caller confirms bypass
        if (req.Status == "InProgress" && !req.BypassEstimateLinkWarning)
        {
            var hasLink = await db.EstimateTaskLinks
                .AnyAsync(l => l.TaskId == id && !l.IsArchived, ct);
            if (!hasLink)
                return UnprocessableEntity(new
                {
                    code = "NoEstimateLink",
                    warning = true,
                    message = "This task has no active estimate link. Add an estimate link before activating, or set bypassEstimateLinkWarning=true to proceed anyway."
                });
        }

        if (req.Status == "InProgress" && task.ActualStart == null) task.ActualStart = DateTime.UtcNow;
        if (req.Status == "Complete" && task.ActualEnd == null) task.ActualEnd = DateTime.UtcNow;

        task.Status = req.Status;
        task.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(task);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var task = await db.PlanTasks.FirstOrDefaultAsync(t => t.TaskId == id, ct);
        if (task == null) return NotFound();
        db.PlanTasks.Remove(task);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── Dependencies ───────────────────────────────────────────────────────
    [HttpPost("{id:int}/dependencies")]
    public async Task<IActionResult> AddDependency(int id, [FromBody] TaskDependency dep, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        dep.SuccessorTaskId = id;
        db.TaskDependencies.Add(dep);
        await db.SaveChangesAsync(ct);
        return Ok(dep);
    }

    [HttpDelete("{id:int}/dependencies/{depId:int}")]
    public async Task<IActionResult> RemoveDependency(int id, int depId, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var dep = await db.TaskDependencies
            .FirstOrDefaultAsync(d => d.DependencyId == depId && d.SuccessorTaskId == id, ct);
        if (dep == null) return NotFound();
        db.TaskDependencies.Remove(dep);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}

public record ProgressUpdateRequest(decimal PercentComplete, string? Status, DateTime? ForecastEnd, string? Notes);
public record TaskStatusRequest(string Status, bool BypassEstimateLinkWarning = false);
