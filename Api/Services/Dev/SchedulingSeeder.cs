using Microsoft.EntityFrameworkCore;
using Stronghold.EnterpriseEstimating.Data;
using Stronghold.EnterpriseEstimating.Data.Models;
using Stronghold.EnterpriseEstimating.Data.Models.Scheduling;

namespace Stronghold.EnterpriseEstimating.Api.Services.Dev;

public static class SchedulingSeeder
{
    public static async Task SeedSchedulingData(AppDbContext db)
    {
        // Idempotent: skip if any resources exist for CSL
        if (await db.Resources.AnyAsync(r => r.CompanyCode == "CSL")) return;

        // Crafts (shared reference data — upsert by CraftCode)
        var craftCodes = new[] { "PP", "EL", "CR" };
        var existingCrafts = await db.Crafts
            .Where(c => craftCodes.Contains(c.CraftCode))
            .Select(c => c.CraftCode)
            .ToHashSetAsync();

        if (!existingCrafts.Contains("PP"))
            db.Crafts.Add(new Craft { CraftCode = "PP", Title = "Pipefitter", IsDirect = true });
        if (!existingCrafts.Contains("EL"))
            db.Crafts.Add(new Craft { CraftCode = "EL", Title = "Electrician", IsDirect = true });
        if (!existingCrafts.Contains("CR"))
            db.Crafts.Add(new Craft { CraftCode = "CR", Title = "Crane Operator", IsDirect = true });

        await db.SaveChangesAsync();

        var today = DateTime.UtcNow.Date;

        // Resources
        var mike = new Resource { CompanyCode = "CSL", Name = "Mike Torres",    CraftCode = "PP", Branch = "Branch A", IsActive = true };
        var sarah = new Resource { CompanyCode = "CSL", Name = "Sarah Vance",   CraftCode = "PP", Branch = "Branch A", IsActive = true };
        var darren = new Resource { CompanyCode = "CSL", Name = "Darren Hill",  CraftCode = "EL", Branch = "Branch B", IsActive = true };
        var angela = new Resource { CompanyCode = "CSL", Name = "Angela Reyes", CraftCode = "CR", Branch = "Branch A", IsActive = true };

        db.Resources.AddRange(mike, sarah, darren, angela);
        await db.SaveChangesAsync();

        // Certifications
        db.Certifications.AddRange(
            new Certification { ResourceId = mike.ResourceId,   Type = "OSHA-30",      ExpirationDate = new DateTime(2027, 1, 1) },
            new Certification { ResourceId = mike.ResourceId,   Type = "H2S",          ExpirationDate = new DateTime(2026, 6, 1) },
            new Certification { ResourceId = sarah.ResourceId,  Type = "OSHA-10",      ExpirationDate = new DateTime(2026, 12, 1) },
            new Certification { ResourceId = darren.ResourceId, Type = "OSHA-30",      ExpirationDate = new DateTime(2027, 3, 1) },
            new Certification { ResourceId = angela.ResourceId, Type = "Crane-Operator", ExpirationDate = new DateTime(2027, 6, 1) }
        );

        // Find a real CSL estimate to link assignments to
        var jobA = await db.Estimates
            .Where(e => e.CompanyCode == "CSL" && (e.Status == "Awarded" || e.Status == "Pending"))
            .OrderBy(e => e.EstimateId)
            .FirstOrDefaultAsync();

        var jobB = await db.Estimates
            .Where(e => e.CompanyCode == "CSL" && (e.Status == "Awarded" || e.Status == "Pending"))
            .OrderBy(e => e.EstimateId)
            .Skip(1)
            .FirstOrDefaultAsync();

        if (jobA != null)
        {
            // Mike and Sarah: ending in 5 days → appear in "ending soon"
            db.Assignments.AddRange(
                new Assignment
                {
                    CompanyCode = "CSL", ResourceId = mike.ResourceId,
                    JobSourceType = "Estimate", JobSourceId = jobA.EstimateId,
                    JobName = jobA.Name, CraftCode = "PP",
                    Start = today.AddDays(-30), End = today.AddDays(5),
                    Shift = "Day", Status = "Confirmed", CreatedBy = "seed"
                },
                new Assignment
                {
                    CompanyCode = "CSL", ResourceId = sarah.ResourceId,
                    JobSourceType = "Estimate", JobSourceId = jobA.EstimateId,
                    JobName = jobA.Name, CraftCode = "PP",
                    Start = today.AddDays(-30), End = today.AddDays(5),
                    Shift = "Day", Status = "Confirmed", CreatedBy = "seed"
                }
            );
        }

        var jobBEstimate = jobB ?? jobA;
        if (jobBEstimate != null)
        {
            // Darren and Angela: long-running, 60 days out
            db.Assignments.AddRange(
                new Assignment
                {
                    CompanyCode = "CSL", ResourceId = darren.ResourceId,
                    JobSourceType = "Estimate", JobSourceId = jobBEstimate.EstimateId,
                    JobName = jobBEstimate.Name, CraftCode = "EL",
                    Start = today.AddDays(-10), End = today.AddDays(60),
                    Shift = "Day", Status = "Confirmed", CreatedBy = "seed"
                },
                new Assignment
                {
                    CompanyCode = "CSL", ResourceId = angela.ResourceId,
                    JobSourceType = "Estimate", JobSourceId = jobBEstimate.EstimateId,
                    JobName = jobBEstimate.Name, CraftCode = "CR",
                    Start = today.AddDays(-10), End = today.AddDays(60),
                    Shift = "Day", Status = "Confirmed", CreatedBy = "seed"
                }
            );
        }

        await db.SaveChangesAsync();
    }


    public static async Task SeedCraftsComprehensive(AppDbContext db)
    {
        var desired = new (string Code, string Title, bool IsDirect)[]
        {
            ("PF",  "Pipefitter",            true),
            ("PFH", "Pipefitter Helper",      true),
            ("BM",  "Boilermaker",            true),
            ("BMH", "Boilermaker Helper",     true),
            ("WD",  "Welder",                 true),
            ("WDH", "Welder Helper",          true),
            ("MW",  "Millwright",             true),
            ("NDT", "NDT Technician",         true),
            ("IE",  "Instrument Tech",        true),
            ("OPR", "Crane Operator",         true),
            ("RIG", "Rigger",                 true),
            ("SCF", "Scaffold Builder",       true),
            ("DRV", "Driver / Teamster",      true),
            ("MGT", "Project Manager",        false),
            ("SUP", "Supervisor / Foreman",   false),
            ("SAF", "Safety",                 false),
        };
        var existing = await db.Crafts.Select(c => c.CraftCode).ToHashSetAsync();
        foreach (var (code, title, isDirect) in desired)
            if (!existing.Contains(code))
                db.Crafts.Add(new Craft { CraftCode = code, Title = title, IsDirect = isDirect });
        if (db.ChangeTracker.HasChanges()) await db.SaveChangesAsync();
    }

    // ── Resources: 20-person comprehensive workforce ──────────────────────────
    public static async Task SeedResourcesComprehensive(AppDbContext db)
    {
        if (await db.Resources.AnyAsync(r => r.CompanyCode == "CSL" && r.EmployeeId == "CSL-001")) return;

        var today = DateTime.UtcNow.Date;

        static Resource R(string emp, string first, string last, string craft, string region, string branch, string shift = "Day")
            => new() { CompanyCode = "CSL", EmployeeId = emp, FirstName = first, LastName = last,
                Name = $"{first} {last}", CraftCode = craft, Region = region, Branch = branch,
                EmploymentStatus = "Active", IsActive = true, ShiftEligibility = shift };

        var r01 = R("CSL-001", "James",   "Hartley",  "SUP", "Gulf", "Houston");
        var r02 = R("CSL-002", "Tom",     "Bradley",  "PF",  "Gulf", "Houston");
        var r03 = R("CSL-003", "Mike",    "Chen",     "PF",  "Gulf", "Houston");
        var r04 = R("CSL-004", "Carlos",  "Reyes",    "PF",  "Gulf", "Beaumont");
        var r05 = R("CSL-005", "Sarah",   "Moss",     "WD",  "Gulf", "Houston");
        var r06 = R("CSL-006", "Derek",   "Nguyen",   "WD",  "Gulf", "Houston");
        var r07 = R("CSL-007", "Frank",   "Torres",   "BM",  "Gulf", "Beaumont");
        var r08 = R("CSL-008", "Ray",     "Johnson",  "RIG", "Gulf", "Houston");
        var r09 = R("CSL-009", "Pat",     "O'Brien",  "SAF", "Gulf", "Houston");
        var r10 = R("CSL-010", "Kevin",   "Park",     "NDT", "Gulf", "Houston");
        var r11 = R("CSL-011", "Luis",    "Martinez", "PF",  "Gulf", "Corpus Christi");
        var r12 = R("CSL-012", "Jim",     "Perkins",  "WD",  "Gulf", "Houston");
        var r13 = R("CSL-013", "Dana",    "Hill",     "BM",  "Gulf", "Beaumont");
        var r14 = R("CSL-014", "Mark",    "Stevens",  "SUP", "Gulf", "Houston");
        var r15 = R("CSL-015", "Bobby",   "Cruz",     "PF",  "Gulf", "Corpus Christi");
        var r16 = R("CSL-016", "Amy",     "Turner",   "IE",  "Gulf", "Beaumont");
        var r17 = R("CSL-017", "Teresa",  "Ryan",     "SUP", "Gulf", "Galveston");
        var r18 = R("CSL-018", "Eddie",   "Bass",     "WD",  "Gulf", "Beaumont");
        var r19 = R("CSL-019", "Chris",   "Lane",     "PF",  "Gulf", "Houston");
        var r20 = R("CSL-020", "Rosa",    "Fuentes",  "SAF", "Gulf", "Houston");

        db.Resources.AddRange(r01,r02,r03,r04,r05,r06,r07,r08,r09,r10,
                               r11,r12,r13,r14,r15,r16,r17,r18,r19,r20);
        await db.SaveChangesAsync();

        // Certifications
        db.Certifications.AddRange(
            // OSHA-30 for supervisors and safety
            new Certification { ResourceId=r01.ResourceId, Type="OSHA-30", ExpirationDate=new DateTime(2028,3,1) },
            new Certification { ResourceId=r09.ResourceId, Type="OSHA-30", ExpirationDate=new DateTime(2027,9,1) },
            new Certification { ResourceId=r14.ResourceId, Type="OSHA-30", ExpirationDate=new DateTime(2028,1,1) },
            new Certification { ResourceId=r17.ResourceId, Type="OSHA-30", ExpirationDate=new DateTime(2027,6,1) },
            new Certification { ResourceId=r20.ResourceId, Type="OSHA-30", ExpirationDate=new DateTime(2027,11,1) },
            // OSHA-10 for craft workers
            new Certification { ResourceId=r02.ResourceId, Type="OSHA-10", ExpirationDate=new DateTime(2027,5,1) },
            new Certification { ResourceId=r03.ResourceId, Type="OSHA-10", ExpirationDate=new DateTime(2027,8,1) },
            new Certification { ResourceId=r04.ResourceId, Type="OSHA-10", ExpirationDate=new DateTime(2027,2,1) },
            new Certification { ResourceId=r07.ResourceId, Type="OSHA-10", ExpirationDate=new DateTime(2026,11,1) },
            new Certification { ResourceId=r08.ResourceId, Type="OSHA-10", ExpirationDate=new DateTime(2027,4,1) },
            new Certification { ResourceId=r11.ResourceId, Type="OSHA-10", ExpirationDate=new DateTime(2027,7,1) },
            new Certification { ResourceId=r12.ResourceId, Type="OSHA-10", ExpirationDate=new DateTime(2027,1,1) },
            new Certification { ResourceId=r13.ResourceId, Type="OSHA-10", ExpirationDate=new DateTime(2026,12,1) },
            new Certification { ResourceId=r16.ResourceId, Type="OSHA-10", ExpirationDate=new DateTime(2027,3,1) },
            new Certification { ResourceId=r18.ResourceId, Type="OSHA-10", ExpirationDate=new DateTime(2027,10,1) },
            new Certification { ResourceId=r19.ResourceId, Type="OSHA-10", ExpirationDate=new DateTime(2026,8,1) },
            // Bobby Cruz: OSHA-10 expired — visible cert gap
            new Certification { ResourceId=r15.ResourceId, Type="OSHA-10", ExpirationDate=today.AddDays(-365) },
            // Welding certs
            new Certification { ResourceId=r05.ResourceId, Type="AWS-CWI",   ExpirationDate=new DateTime(2028,6,1) },
            new Certification { ResourceId=r06.ResourceId, Type="AWS-CWI",   ExpirationDate=new DateTime(2027,9,1) },
            new Certification { ResourceId=r18.ResourceId, Type="AWS-CWI",   ExpirationDate=new DateTime(2028,2,1) },
            // NDT
            new Certification { ResourceId=r10.ResourceId, Type="RT-Level-2", ExpirationDate=new DateTime(2028,1,1) },
            new Certification { ResourceId=r10.ResourceId, Type="UT-Level-2", ExpirationDate=new DateTime(2027,11,1) },
            // Rigging
            new Certification { ResourceId=r08.ResourceId, Type="Rigging-Level-3", ExpirationDate=new DateTime(2027,8,1) },
            // H2S
            new Certification { ResourceId=r04.ResourceId, Type="H2S", ExpirationDate=new DateTime(2026,10,1) },
            new Certification { ResourceId=r11.ResourceId, Type="H2S", ExpirationDate=new DateTime(2026,9,1) }
        );

        // Availability blocks
        db.AvailabilityBlocks.AddRange(
            new AvailabilityBlock { ResourceId=r12.ResourceId, Start=today.AddDays(2),  End=today.AddDays(47),  Reason="Approved PTO" },
            new AvailabilityBlock { ResourceId=r19.ResourceId, Start=today.AddDays(2),  End=today.AddDays(47),  Reason="Medical Leave — Approved" }
        );

        await db.SaveChangesAsync();
    }

    // ── PM Lifecycle: 4 reference scenarios ───────────────────────────────────
}