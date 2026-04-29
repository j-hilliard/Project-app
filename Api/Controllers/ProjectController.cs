using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stronghold.EnterpriseEstimating.Data;
using Stronghold.EnterpriseEstimating.Data.Models.Planning;

namespace Stronghold.EnterpriseEstimating.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/projects")]
[Authorize]
public class ProjectController : ControllerBase
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public ProjectController(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    private string CompanyCode => User.FindFirst("company_code")?.Value ?? string.Empty;
    private string Username => User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                            ?? User.FindFirst("username")?.Value ?? string.Empty;

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? status,
        [FromQuery] int? estimateId,
        CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var q = db.Projects.Where(p => p.CompanyCode == CompanyCode);
        if (status != null) q = q.Where(p => p.Status == status);
        if (estimateId.HasValue) q = q.Where(p => p.EstimateId == estimateId.Value);
        var list = await q
            .Include(p => p.Phases)
            .Include(p => p.WorkOrders)
            .OrderByDescending(p => p.ProjectId)
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var project = await db.Projects
            .Include(p => p.Phases.OrderBy(ph => ph.SortOrder))
            .Include(p => p.WorkOrders)
            .Include(p => p.Milestones.OrderBy(m => m.PlannedDate))
            .FirstOrDefaultAsync(p => p.ProjectId == id && p.CompanyCode == CompanyCode, ct);
        return project == null ? NotFound() : Ok(project);
    }

    [HttpGet("{id:int}/full")]
    public async Task<IActionResult> GetFull(int id, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var project = await db.Projects
            .Include(p => p.Phases.OrderBy(ph => ph.SortOrder))
                .ThenInclude(ph => ph.Tasks.OrderBy(t => t.SortOrder))
                    .ThenInclude(t => t.SubTasks.OrderBy(st => st.SortOrder))
            .Include(p => p.Phases)
                .ThenInclude(ph => ph.Milestones)
            .Include(p => p.WorkOrders)
            .Include(p => p.Milestones.OrderBy(m => m.PlannedDate))
            .FirstOrDefaultAsync(p => p.ProjectId == id && p.CompanyCode == CompanyCode, ct);
        return project == null ? NotFound() : Ok(project);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Project project, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // Gate: CommAuth must be Active
        if (project.CommercialAuthorizationId.HasValue)
        {
            var caActive = await db.CommercialAuthorizations
                .AnyAsync(ca => ca.CommercialAuthorizationId == project.CommercialAuthorizationId
                             && ca.Status == "Active"
                             && ca.CompanyCode == CompanyCode, ct);
            if (!caActive)
                return BadRequest("CommercialAuthorization must be Active to create a Project.");
        }

        project.CompanyCode = CompanyCode;
        project.CreatedBy = Username;
        project.CreatedAt = DateTimeOffset.UtcNow;
        project.UpdatedAt = DateTimeOffset.UtcNow;
        db.Projects.Add(project);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = project.ProjectId }, project);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Project update, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var project = await db.Projects
            .FirstOrDefaultAsync(p => p.ProjectId == id && p.CompanyCode == CompanyCode, ct);
        if (project == null) return NotFound();

        project.Name = update.Name;
        project.Client = update.Client;
        project.ClientCode = update.ClientCode;
        project.Site = update.Site;
        project.City = update.City;
        project.State = update.State;
        project.JobLetter = update.JobLetter;
        project.PlannedStart = update.PlannedStart;
        project.PlannedEnd = update.PlannedEnd;
        project.ForecastEnd = update.ForecastEnd;
        project.AtRiskThresholdDays = update.AtRiskThresholdDays;
        project.OwnerUserId = update.OwnerUserId;
        project.LessonsLearnedNotes = update.LessonsLearnedNotes;
        project.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(project);
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> SetStatus(int id, [FromBody] StatusUpdateRequest req, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var project = await db.Projects
            .FirstOrDefaultAsync(p => p.ProjectId == id && p.CompanyCode == CompanyCode, ct);
        if (project == null) return NotFound();

        if (req.Status == "Active" && project.ActualStart == null)
            project.ActualStart = DateTime.UtcNow;
        if ((req.Status == "Closed" || req.Status == "Closing") && project.ActualEnd == null)
            project.ActualEnd = DateTime.UtcNow;

        project.Status = req.Status;
        project.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(project);
    }

    [HttpPost("{id:int}/baseline")]
    public async Task<IActionResult> LockBaseline(int id, [FromBody] BaselineRequest req, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var project = await db.Projects
            .Include(p => p.Phases)
                .ThenInclude(ph => ph.Tasks)
            .FirstOrDefaultAsync(p => p.ProjectId == id && p.CompanyCode == CompanyCode, ct);
        if (project == null) return NotFound();

        var snapshots = new List<TimelineBaseline>
        {
            new()
            {
                ProjectId = id,
                EntityType = "Project",
                EntityId = id,
                SnapshotDate = DateTime.UtcNow,
                PlannedStart = project.PlannedStart,
                PlannedEnd = project.PlannedEnd,
                BaselineReason = req.Reason,
                BaselineLabel = req.Label,
                LockedBy = Username,
                LockedAt = DateTimeOffset.UtcNow,
            }
        };

        foreach (var phase in project.Phases)
        {
            snapshots.Add(new TimelineBaseline
            {
                ProjectId = id,
                EntityType = "Phase",
                EntityId = phase.PhaseId,
                SnapshotDate = DateTime.UtcNow,
                PlannedStart = phase.PlannedStart,
                PlannedEnd = phase.PlannedEnd,
                BaselineReason = req.Reason,
                BaselineLabel = req.Label,
                LockedBy = Username,
                LockedAt = DateTimeOffset.UtcNow,
            });

            foreach (var task in phase.Tasks)
            {
                snapshots.Add(new TimelineBaseline
                {
                    ProjectId = id,
                    EntityType = "Task",
                    EntityId = task.TaskId,
                    SnapshotDate = DateTime.UtcNow,
                    PlannedStart = task.PlannedStart,
                    PlannedEnd = task.PlannedEnd,
                    DurationDays = task.DurationDays,
                    BaselineReason = req.Reason,
                    BaselineLabel = req.Label,
                    LockedBy = Username,
                    LockedAt = DateTimeOffset.UtcNow,
                });
            }
        }

        db.TimelineBaselines.AddRange(snapshots);
        await db.SaveChangesAsync(ct);
        return Ok(new { snapshotsCreated = snapshots.Count });
    }
}

public record BaselineRequest(string? Reason, string? Label);
