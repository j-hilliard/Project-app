using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stronghold.EnterpriseEstimating.Api.Services;
using Stronghold.EnterpriseEstimating.Data;
using Stronghold.EnterpriseEstimating.Data.Models.Scheduling;

namespace Stronghold.EnterpriseEstimating.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/scheduling")]
[Authorize]
public class SchedulingController : ControllerBase
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly SchedulingDemandService _demandService;
    private readonly AssignmentConflictService _conflictService;
    private readonly CoverageCalculationService _coverageService;
    private readonly SuggestedMatchService _matchService;

    public SchedulingController(
        IDbContextFactory<AppDbContext> dbFactory,
        SchedulingDemandService demandService,
        AssignmentConflictService conflictService,
        CoverageCalculationService coverageService,
        SuggestedMatchService matchService)
    {
        _dbFactory = dbFactory;
        _demandService = demandService;
        _conflictService = conflictService;
        _coverageService = coverageService;
        _matchService = matchService;
    }

    private string CompanyCode => User.FindFirst("company_code")?.Value ?? string.Empty;
    private string Username => User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                            ?? User.FindFirst("username")?.Value ?? string.Empty;

    // ── Dashboard ─────────────────────────────────────────────────────────
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        var company = CompanyCode;
        var demand = await _demandService.GetDemandAsync(company, ct);
        var today = DateTime.UtcNow.Date;
        var soonThreshold = today.AddDays(7);

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var endingSoonCount = await db.Assignments
            .Where(a => a.CompanyCode == company &&
                        a.Status != "Cancelled" &&
                        a.End >= today &&
                        a.End <= soonThreshold)
            .Select(a => a.ResourceId)
            .Distinct()
            .CountAsync(ct);

        var craftShortagesCount = await _coverageService.GetShortageCountAsync(company, ct);
        var availableSoonCount = await _matchService.GetAvailableSoonCountAsync(company, ct);

        return Ok(new
        {
            activeJobCount = demand.Count,
            craftShortagesCount,
            endingSoonCount,
            availableSoonCount,
            suggestedMatchCount = availableSoonCount,
        });
    }

    // ── Demand (jobs board) ───────────────────────────────────────────────
    [HttpGet("jobs")]
    public async Task<IActionResult> Jobs(CancellationToken ct)
    {
        var demand = await _demandService.GetDemandAsync(CompanyCode, ct);
        return Ok(demand);
    }

    // ── Resources ─────────────────────────────────────────────────────────
    [HttpGet("resources")]
    public async Task<IActionResult> ListResources(
        [FromQuery] string? craft,
        [FromQuery] bool? active,
        CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var q = db.Resources.Where(r => r.CompanyCode == CompanyCode);
        if (craft != null) q = q.Where(r => r.CraftCode == craft);
        if (active.HasValue) q = q.Where(r => r.IsActive == active.Value);
        var list = await q.Include(r => r.Certifications).OrderBy(r => r.Name).ToListAsync(ct);
        return Ok(list);
    }

    [HttpPost("resources")]
    public async Task<IActionResult> CreateResource([FromBody] Resource resource, CancellationToken ct)
    {
        resource.CompanyCode = CompanyCode;
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        db.Resources.Add(resource);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetResource), new { id = resource.ResourceId }, resource);
    }

    [HttpGet("resources/{id:int}")]
    public async Task<IActionResult> GetResource(int id, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var r = await db.Resources
            .Include(r => r.Certifications)
            .Include(r => r.AvailabilityBlocks)
            .FirstOrDefaultAsync(r => r.ResourceId == id && r.CompanyCode == CompanyCode, ct);
        return r == null ? NotFound() : Ok(r);
    }

    [HttpPut("resources/{id:int}")]
    public async Task<IActionResult> UpdateResource(int id, [FromBody] Resource update, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var existing = await db.Resources.FirstOrDefaultAsync(r => r.ResourceId == id && r.CompanyCode == CompanyCode, ct);
        if (existing == null) return NotFound();
        existing.Name = update.Name;
        existing.CraftCode = update.CraftCode;
        existing.Branch = update.Branch;
        existing.IsActive = update.IsActive;
        await db.SaveChangesAsync(ct);
        return Ok(existing);
    }

    [HttpDelete("resources/{id:int}")]
    public async Task<IActionResult> DeleteResource(int id, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var existing = await db.Resources.FirstOrDefaultAsync(r => r.ResourceId == id && r.CompanyCode == CompanyCode, ct);
        if (existing == null) return NotFound();
        db.Resources.Remove(existing);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── Certifications ────────────────────────────────────────────────────
    [HttpGet("resources/{resourceId:int}/certifications")]
    public async Task<IActionResult> ListCertifications(int resourceId, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var resource = await db.Resources.FirstOrDefaultAsync(r => r.ResourceId == resourceId && r.CompanyCode == CompanyCode, ct);
        if (resource == null) return NotFound();
        var certs = await db.Certifications.Where(c => c.ResourceId == resourceId).ToListAsync(ct);
        return Ok(certs);
    }

    [HttpPost("certifications")]
    public async Task<IActionResult> CreateCertification([FromBody] Certification cert, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var resource = await db.Resources.FirstOrDefaultAsync(r => r.ResourceId == cert.ResourceId && r.CompanyCode == CompanyCode, ct);
        if (resource == null) return NotFound();
        db.Certifications.Add(cert);
        await db.SaveChangesAsync(ct);
        return Ok(cert);
    }

    [HttpDelete("certifications/{id:int}")]
    public async Task<IActionResult> DeleteCertification(int id, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var cert = await db.Certifications
            .Include(c => c.Resource)
            .FirstOrDefaultAsync(c => c.CertId == id && c.Resource.CompanyCode == CompanyCode, ct);
        if (cert == null) return NotFound();
        db.Certifications.Remove(cert);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── Assignments ───────────────────────────────────────────────────────
    [HttpGet("assignments")]
    public async Task<IActionResult> ListAssignments(
        [FromQuery] int? resourceId,
        [FromQuery] string? jobSourceType,
        [FromQuery] int? jobSourceId,
        CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var q = db.Assignments
            .Include(a => a.Resource)
            .Where(a => a.CompanyCode == CompanyCode);
        if (resourceId.HasValue) q = q.Where(a => a.ResourceId == resourceId.Value);
        if (jobSourceType != null) q = q.Where(a => a.JobSourceType == jobSourceType);
        if (jobSourceId.HasValue) q = q.Where(a => a.JobSourceId == jobSourceId.Value);
        return Ok(await q.OrderBy(a => a.Start).ToListAsync(ct));
    }

    [HttpPost("assignments")]
    public async Task<IActionResult> CreateAssignment([FromBody] Assignment assignment, CancellationToken ct)
    {
        assignment.CompanyCode = CompanyCode;
        assignment.CreatedBy = Username;

        var conflicts = await _conflictService.DetectConflictsAsync(
            assignment.ResourceId, assignment.Start, assignment.End,
            assignment.CraftCode, CompanyCode, null, ct);

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        db.Assignments.Add(assignment);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetAssignment), new { id = assignment.AssignmentId }, new
        {
            assignment,
            conflicts,
            hasConflicts = conflicts.Count > 0,
        });
    }

    [HttpGet("assignments/{id:int}")]
    public async Task<IActionResult> GetAssignment(int id, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var a = await db.Assignments
            .Include(a => a.Resource)
            .FirstOrDefaultAsync(a => a.AssignmentId == id && a.CompanyCode == CompanyCode, ct);
        return a == null ? NotFound() : Ok(a);
    }

    [HttpPut("assignments/{id:int}")]
    public async Task<IActionResult> UpdateAssignment(int id, [FromBody] Assignment update, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var existing = await db.Assignments.FirstOrDefaultAsync(a => a.AssignmentId == id && a.CompanyCode == CompanyCode, ct);
        if (existing == null) return NotFound();
        existing.ResourceId = update.ResourceId;
        existing.JobSourceType = update.JobSourceType;
        existing.JobSourceId = update.JobSourceId;
        existing.JobName = update.JobName;
        existing.CraftCode = update.CraftCode;
        existing.Start = update.Start;
        existing.End = update.End;
        existing.Shift = update.Shift;
        existing.Status = update.Status;
        await db.SaveChangesAsync(ct);

        var conflicts = await _conflictService.DetectConflictsAsync(
            existing.ResourceId, existing.Start, existing.End,
            existing.CraftCode, CompanyCode, id, ct);

        return Ok(new { assignment = existing, conflicts, hasConflicts = conflicts.Count > 0 });
    }

    [HttpDelete("assignments/{id:int}")]
    public async Task<IActionResult> DeleteAssignment(int id, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var a = await db.Assignments.FirstOrDefaultAsync(a => a.AssignmentId == id && a.CompanyCode == CompanyCode, ct);
        if (a == null) return NotFound();
        db.Assignments.Remove(a);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── Coverage ──────────────────────────────────────────────────────────
    [HttpGet("coverage")]
    public async Task<IActionResult> Coverage(CancellationToken ct)
    {
        var coverage = await _coverageService.GetCoverageAsync(CompanyCode, ct);
        return Ok(coverage);
    }

    // ── Ending Soon ───────────────────────────────────────────────────────
    [HttpGet("ending-soon")]
    public async Task<IActionResult> EndingSoon([FromQuery] int days = 7, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var company = CompanyCode;
        var today = DateTime.UtcNow.Date;
        var threshold = today.AddDays(days);

        var list = await db.Assignments
            .Include(a => a.Resource)
            .Where(a => a.CompanyCode == company &&
                        a.Status != "Cancelled" &&
                        a.End >= today &&
                        a.End <= threshold)
            .OrderBy(a => a.End)
            .ToListAsync(ct);

        return Ok(list);
    }

    // ── Available Soon (suggested matches) ───────────────────────────────
    [HttpGet("available-soon")]
    public async Task<IActionResult> AvailableSoon([FromQuery] int days = 14, CancellationToken ct = default)
    {
        var matches = await _matchService.GetSuggestedMatchesAsync(CompanyCode, ct);
        return Ok(matches);
    }

    [HttpGet("suggested-matches")]
    public async Task<IActionResult> SuggestedMatches(CancellationToken ct)
    {
        var matches = await _matchService.GetSuggestedMatchesAsync(CompanyCode, ct);
        return Ok(matches);
    }

    // ── Availability Blocks ───────────────────────────────────────────────
    [HttpPost("availability-blocks")]
    public async Task<IActionResult> CreateAvailabilityBlock([FromBody] AvailabilityBlock block, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        db.AvailabilityBlocks.Add(block);
        await db.SaveChangesAsync(ct);
        return Ok(block);
    }

    [HttpDelete("availability-blocks/{id:int}")]
    public async Task<IActionResult> DeleteAvailabilityBlock(int id, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var block = await db.AvailabilityBlocks.FindAsync(new object[] { id }, ct);
        if (block == null) return NotFound();
        db.AvailabilityBlocks.Remove(block);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
