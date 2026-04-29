using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stronghold.EnterpriseEstimating.Data;
using Stronghold.EnterpriseEstimating.Data.Models.Planning;

namespace Stronghold.EnterpriseEstimating.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/milestones")]
[Authorize]
public class MilestoneController : ControllerBase
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public MilestoneController(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    private string CompanyCode => User.FindFirst("company_code")?.Value ?? string.Empty;
    private string Username => User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                            ?? User.FindFirst("username")?.Value ?? string.Empty;

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int? projectId,
        [FromQuery] string? status,
        [FromQuery] bool? criticalOnly,
        CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var q = db.Milestones
            .Where(m => db.Projects.Any(p => p.ProjectId == m.ProjectId && p.CompanyCode == CompanyCode));
        if (projectId.HasValue) q = q.Where(m => m.ProjectId == projectId.Value);
        if (status != null) q = q.Where(m => m.Status == status);
        if (criticalOnly == true) q = q.Where(m => m.IsCritical);
        return Ok(await q.OrderBy(m => m.PlannedDate).ToListAsync(ct));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var milestone = await db.Milestones.FirstOrDefaultAsync(m => m.MilestoneId == id, ct);
        if (milestone == null) return NotFound();
        var owned = await db.Projects.AnyAsync(p => p.ProjectId == milestone.ProjectId && p.CompanyCode == CompanyCode, ct);
        return owned ? Ok(milestone) : Forbid();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Milestone milestone, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var owned = await db.Projects.AnyAsync(p => p.ProjectId == milestone.ProjectId && p.CompanyCode == CompanyCode, ct);
        if (!owned) return BadRequest("Project not found.");

        milestone.CreatedBy = Username;
        milestone.CreatedAt = DateTimeOffset.UtcNow;
        db.Milestones.Add(milestone);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = milestone.MilestoneId }, milestone);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Milestone update, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var milestone = await db.Milestones.FirstOrDefaultAsync(m => m.MilestoneId == id, ct);
        if (milestone == null) return NotFound();
        var owned = await db.Projects.AnyAsync(p => p.ProjectId == milestone.ProjectId && p.CompanyCode == CompanyCode, ct);
        if (!owned) return Forbid();

        milestone.Name = update.Name;
        milestone.Description = update.Description;
        milestone.PlannedDate = update.PlannedDate;
        milestone.IsDeadline = update.IsDeadline;
        milestone.IsCritical = update.IsCritical;
        milestone.Color = update.Color;
        milestone.PhaseId = update.PhaseId;
        milestone.TaskId = update.TaskId;
        await db.SaveChangesAsync(ct);
        return Ok(milestone);
    }

    [HttpPatch("{id:int}/achieve")]
    public async Task<IActionResult> Achieve(int id, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var milestone = await db.Milestones.FirstOrDefaultAsync(m => m.MilestoneId == id, ct);
        if (milestone == null) return NotFound();
        var owned = await db.Projects.AnyAsync(p => p.ProjectId == milestone.ProjectId && p.CompanyCode == CompanyCode, ct);
        if (!owned) return Forbid();

        milestone.Status = "Achieved";
        milestone.ActualDate = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(milestone);
    }

    [HttpPatch("{id:int}/miss")]
    public async Task<IActionResult> Miss(int id, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var milestone = await db.Milestones.FirstOrDefaultAsync(m => m.MilestoneId == id, ct);
        if (milestone == null) return NotFound();
        var owned = await db.Projects.AnyAsync(p => p.ProjectId == milestone.ProjectId && p.CompanyCode == CompanyCode, ct);
        if (!owned) return Forbid();

        milestone.Status = "Missed";
        await db.SaveChangesAsync(ct);
        return Ok(milestone);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var milestone = await db.Milestones.FirstOrDefaultAsync(m => m.MilestoneId == id, ct);
        if (milestone == null) return NotFound();
        var owned = await db.Projects.AnyAsync(p => p.ProjectId == milestone.ProjectId && p.CompanyCode == CompanyCode, ct);
        if (!owned) return Forbid();

        db.Milestones.Remove(milestone);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
