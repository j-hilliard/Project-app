using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stronghold.EnterpriseEstimating.Data;
using Stronghold.EnterpriseEstimating.Data.Models.Planning;

namespace Stronghold.EnterpriseEstimating.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/projects/{projectId:int}/phases")]
[Authorize]
public class ProjectPhaseController : ControllerBase
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public ProjectPhaseController(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    private string CompanyCode => User.FindFirst("company_code")?.Value ?? string.Empty;

    [HttpGet]
    public async Task<IActionResult> List(int projectId, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var projectExists = await db.Projects.AnyAsync(p => p.ProjectId == projectId && p.CompanyCode == CompanyCode, ct);
        if (!projectExists) return NotFound();

        var phases = await db.ProjectPhases
            .Where(ph => ph.ProjectId == projectId)
            .Include(ph => ph.Tasks.OrderBy(t => t.SortOrder))
            .Include(ph => ph.Milestones)
            .OrderBy(ph => ph.SortOrder)
            .ToListAsync(ct);
        return Ok(phases);
    }

    [HttpGet("{phaseId:int}")]
    public async Task<IActionResult> Get(int projectId, int phaseId, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var phase = await db.ProjectPhases
            .Include(ph => ph.Tasks.OrderBy(t => t.SortOrder))
                .ThenInclude(t => t.SubTasks)
            .Include(ph => ph.Milestones)
            .FirstOrDefaultAsync(ph => ph.PhaseId == phaseId && ph.ProjectId == projectId, ct);

        if (phase == null) return NotFound();

        var projectBelongs = await db.Projects.AnyAsync(p => p.ProjectId == projectId && p.CompanyCode == CompanyCode, ct);
        return projectBelongs ? Ok(phase) : Forbid();
    }

    [HttpPost]
    public async Task<IActionResult> Create(int projectId, [FromBody] ProjectPhase phase, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var project = await db.Projects.FirstOrDefaultAsync(p => p.ProjectId == projectId && p.CompanyCode == CompanyCode, ct);
        if (project == null) return NotFound();

        phase.ProjectId = projectId;
        db.ProjectPhases.Add(phase);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { projectId, phaseId = phase.PhaseId }, phase);
    }

    [HttpPut("{phaseId:int}")]
    public async Task<IActionResult> Update(int projectId, int phaseId, [FromBody] ProjectPhase update, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var projectBelongs = await db.Projects.AnyAsync(p => p.ProjectId == projectId && p.CompanyCode == CompanyCode, ct);
        if (!projectBelongs) return NotFound();

        var phase = await db.ProjectPhases.FirstOrDefaultAsync(ph => ph.PhaseId == phaseId && ph.ProjectId == projectId, ct);
        if (phase == null) return NotFound();

        phase.Name = update.Name;
        phase.Description = update.Description;
        phase.PlannedStart = update.PlannedStart;
        phase.PlannedEnd = update.PlannedEnd;
        phase.ForecastEnd = update.ForecastEnd;
        phase.Status = update.Status;
        phase.Color = update.Color;
        phase.SortOrder = update.SortOrder;
        await db.SaveChangesAsync(ct);
        return Ok(phase);
    }

    [HttpPatch("sort-order")]
    public async Task<IActionResult> UpdateSortOrder(int projectId, [FromBody] List<SortOrderItem> items, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var projectBelongs = await db.Projects.AnyAsync(p => p.ProjectId == projectId && p.CompanyCode == CompanyCode, ct);
        if (!projectBelongs) return NotFound();

        var phases = await db.ProjectPhases.Where(ph => ph.ProjectId == projectId).ToListAsync(ct);
        foreach (var item in items)
        {
            var phase = phases.FirstOrDefault(ph => ph.PhaseId == item.Id);
            if (phase != null) phase.SortOrder = item.SortOrder;
        }
        await db.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpDelete("{phaseId:int}")]
    public async Task<IActionResult> Delete(int projectId, int phaseId, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var projectBelongs = await db.Projects.AnyAsync(p => p.ProjectId == projectId && p.CompanyCode == CompanyCode, ct);
        if (!projectBelongs) return NotFound();

        var phase = await db.ProjectPhases.FirstOrDefaultAsync(ph => ph.PhaseId == phaseId && ph.ProjectId == projectId, ct);
        if (phase == null) return NotFound();

        db.ProjectPhases.Remove(phase);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}

public record SortOrderItem(int Id, int SortOrder);
