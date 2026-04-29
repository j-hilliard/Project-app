using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stronghold.EnterpriseEstimating.Api.Services.Dev;
using Stronghold.EnterpriseEstimating.Data;

namespace Stronghold.EnterpriseEstimating.Api.Controllers;

[ApiController]
[ApiVersionNeutral]
[AllowAnonymous]
[Route("api/v{version:apiVersion}/dev")]
[Route("api/v1.0/dev")]
public class DevController : ControllerBase
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IWebHostEnvironment _env;

    public DevController(IDbContextFactory<AppDbContext> dbFactory, IWebHostEnvironment env)
    {
        _dbFactory = dbFactory;
        _env = env;
    }

    [HttpPost("seed")]
    public async Task<IActionResult> Seed()
    {
        if (!_env.IsEnvironment("Local") && !_env.IsDevelopment())
            return Forbid();

        await using var db = await _dbFactory.CreateDbContextAsync();
        var result = await DevSeedOrchestrator.SeedAsync(db);
        return Ok(result);
    }

    [HttpPost("reset")]
    public async Task<IActionResult> Reset([FromQuery] bool includeCostBooks = false, [FromQuery] bool includePm = false)
    {
        if (!_env.IsEnvironment("Local") && !_env.IsDevelopment())
            return Forbid();

        await using var db = await _dbFactory.CreateDbContextAsync();
        var result = await DevResetOrchestrator.ResetAsync(db, includeCostBooks, includePm);
        return Ok(result);
    }
}
