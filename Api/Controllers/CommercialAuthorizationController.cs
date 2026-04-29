using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stronghold.EnterpriseEstimating.Data;
using Stronghold.EnterpriseEstimating.Data.Models.Planning;

namespace Stronghold.EnterpriseEstimating.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/commercial-authorizations")]
[Authorize]
public class CommercialAuthorizationController : ControllerBase
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public CommercialAuthorizationController(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    private string CompanyCode => User.FindFirst("company_code")?.Value ?? string.Empty;
    private string Username => User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                            ?? User.FindFirst("username")?.Value ?? string.Empty;

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int? estimateId,
        [FromQuery] string? status,
        CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var q = db.CommercialAuthorizations.Where(ca => ca.CompanyCode == CompanyCode);
        if (estimateId.HasValue) q = q.Where(ca => ca.EstimateId == estimateId.Value);
        if (status != null) q = q.Where(ca => ca.Status == status);
        return Ok(await q.OrderByDescending(ca => ca.CommercialAuthorizationId).ToListAsync(ct));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var ca = await db.CommercialAuthorizations
            .Include(c => c.Projects)
            .Include(c => c.WorkOrders)
            .FirstOrDefaultAsync(c => c.CommercialAuthorizationId == id && c.CompanyCode == CompanyCode, ct);
        return ca == null ? NotFound() : Ok(ca);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CommercialAuthorization ca, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // Gate: estimate must exist and belong to this company
        var estimate = await db.Estimates
            .FirstOrDefaultAsync(e => e.EstimateId == ca.EstimateId && e.CompanyCode == CompanyCode, ct);
        if (estimate == null) return BadRequest("Estimate not found.");

        ca.CompanyCode = CompanyCode;
        ca.CreatedBy = Username;
        ca.CreatedAt = DateTimeOffset.UtcNow;
        ca.UpdatedAt = DateTimeOffset.UtcNow;
        db.CommercialAuthorizations.Add(ca);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = ca.CommercialAuthorizationId }, ca);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CommercialAuthorization update, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var ca = await db.CommercialAuthorizations
            .FirstOrDefaultAsync(c => c.CommercialAuthorizationId == id && c.CompanyCode == CompanyCode, ct);
        if (ca == null) return NotFound();

        ca.AuthorizationType = update.AuthorizationType;
        ca.AuthorizationNumber = update.AuthorizationNumber;
        ca.AuthorizedValue = update.AuthorizedValue;
        ca.AuthorizedBy = update.AuthorizedBy;
        ca.ReceivedDate = update.ReceivedDate;
        ca.EffectiveDate = update.EffectiveDate;
        ca.ExpirationDate = update.ExpirationDate;
        ca.Notes = update.Notes;
        ca.DocumentReference = update.DocumentReference;
        ca.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(ca);
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> SetStatus(int id, [FromBody] StatusUpdateRequest req, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var ca = await db.CommercialAuthorizations
            .FirstOrDefaultAsync(c => c.CommercialAuthorizationId == id && c.CompanyCode == CompanyCode, ct);
        if (ca == null) return NotFound();

        // Gate: only one Active CommAuth per Estimate
        if (req.Status == "Active")
        {
            var alreadyActive = await db.CommercialAuthorizations
                .AnyAsync(c => c.EstimateId == ca.EstimateId
                            && c.Status == "Active"
                            && c.CommercialAuthorizationId != id, ct);
            if (alreadyActive)
                return Conflict("Another CommercialAuthorization for this estimate is already Active.");
        }

        ca.Status = req.Status;
        ca.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(ca);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var ca = await db.CommercialAuthorizations
            .Include(c => c.Projects)
            .FirstOrDefaultAsync(c => c.CommercialAuthorizationId == id && c.CompanyCode == CompanyCode, ct);
        if (ca == null) return NotFound();
        if (ca.Status != "Draft") return BadRequest("Only Draft authorizations can be deleted.");
        if (ca.Projects.Any()) return Conflict("Authorization has linked projects and cannot be deleted.");

        db.CommercialAuthorizations.Remove(ca);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}

public record StatusUpdateRequest(string Status);
