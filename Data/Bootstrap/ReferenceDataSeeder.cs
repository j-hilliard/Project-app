using Microsoft.EntityFrameworkCore;
using Stronghold.EnterpriseEstimating.Data.Models;

namespace Stronghold.EnterpriseEstimating.Data.Bootstrap;

public class ReferenceDataSeeder
{
    private static readonly List<(string Name, string Description)> DefaultRoles = new()
    {
        ("Administrator", "Full access to all features and settings."),
        ("Estimator", "Can create, edit, and submit estimates and staffing plans."),
        ("Viewer", "Read-only access to estimates, reports, and analytics."),
        ("Analytics", "Access to analytics and global financial dashboards."),
    };

    public async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        await SeedRolesAsync(db, ct);
        await SeedCompaniesAsync(db, ct);
    }

    private static async Task SeedRolesAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.Roles.AnyAsync(ct)) return;

        foreach (var (name, description) in DefaultRoles)
            db.Roles.Add(new Role { Name = name, Description = description });

        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedCompaniesAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.Companies.AnyAsync(ct)) return;

        db.Companies.AddRange(
            new Company
            {
                CompanyCode = "CSL",
                Name = "Cat-Spec, Ltd.",
                ShortName = "Cat-Spec",
                JobLetter = null,
                OpuNumber = "E464",
                Description = "Catalyst Handling & Specialty Industrial Services",
                Active = true
            },
            new Company
            {
                CompanyCode = "ETS",
                Name = "Elite TA Specialists, LLC",
                ShortName = "Elite TA",
                JobLetter = "H",
                OpuNumber = "E465",
                Description = "Turnaround & Maintenance Specialists – South Texas",
                Active = true
            },
            new Company
            {
                CompanyCode = "STS",
                Name = "Specialty Tank Services, Inc.",
                ShortName = "Specialty Tank",
                JobLetter = "S",
                OpuNumber = "E457",
                Description = "Aboveground Storage Tank Inspection & Repair",
                Active = true
            },
            new Company
            {
                CompanyCode = "STG",
                Name = "Stronghold Tower Group, LLC",
                ShortName = "Tower Group",
                JobLetter = "G",
                OpuNumber = "E466",
                Description = "Tower, Vessel & Reactor Internal Services",
                Active = true
            }
        );
        await db.SaveChangesAsync(ct);
    }
}
