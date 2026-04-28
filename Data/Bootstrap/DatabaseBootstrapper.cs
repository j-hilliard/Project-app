using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Stronghold.EnterpriseEstimating.Data.Bootstrap;

public class DatabaseBootstrapper
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ReferenceDataSeeder _referenceSeeder;
    private readonly DemoDataSeeder _demoSeeder;
    private readonly IConfiguration _config;
    private readonly ILogger<DatabaseBootstrapper> _logger;

    public DatabaseBootstrapper(
        IDbContextFactory<AppDbContext> dbFactory,
        ReferenceDataSeeder referenceSeeder,
        DemoDataSeeder demoSeeder,
        IConfiguration config,
        ILogger<DatabaseBootstrapper> logger)
    {
        _dbFactory = dbFactory;
        _referenceSeeder = referenceSeeder;
        _demoSeeder = demoSeeder;
        _config = config;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        _logger.LogInformation("Running database migrations...");
        await db.Database.MigrateAsync(ct);

        if (_config.GetValue<bool>("Database:SeedReferenceData", defaultValue: true))
        {
            _logger.LogInformation("Seeding reference data...");
            await _referenceSeeder.SeedAsync(db, ct);
        }

        if (_config.GetValue<bool>("Database:SeedDemoData", defaultValue: false))
        {
            _logger.LogInformation("Seeding demo data...");
            await _demoSeeder.SeedAsync(db, ct);
        }
    }
}
