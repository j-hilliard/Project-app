using Stronghold.EnterpriseEstimating.Data;

namespace Stronghold.EnterpriseEstimating.Api.Services.Dev;

public static class DevSeedOrchestrator
{
    public static async Task<object> SeedAsync(AppDbContext db)
    {
        await CoreDataSeeder.SeedUsers(db);
        await CoreDataSeeder.SeedUserCompanies(db);
        await CoreDataSeeder.SeedCostBooks(db);
        await CoreDataSeeder.SeedRateBooks(db);
        await CoreDataSeeder.SeedCrewTemplates(db);
        await EstimateSeeder.SeedStaffingPlans(db);
        await EstimateSeeder.SeedEstimates(db);
        await EstimateSeeder.SeedDemoValeroEstimates(db);
        await EstimateSeeder.AssignRateBooksToEstimates(db);
        await CoreDataSeeder.SeedSequences(db);
        await SchedulingSeeder.SeedSchedulingData(db);
        await SchedulingSeeder.SeedCraftsComprehensive(db);
        await SchedulingSeeder.SeedResourcesComprehensive(db);
        await PmLifecycleSeeder.SeedPmLifecycle(db);

        return new
        {
            message = "Seed complete. CSL+ETS estimates, rate books, crew templates, staffing plans, 20 scheduling resources, and PM lifecycle scenarios A-D seeded where missing.",
        };
    }
}