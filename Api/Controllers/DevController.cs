using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stronghold.EnterpriseEstimating.Data;
using Stronghold.EnterpriseEstimating.Data.Models;
using Stronghold.EnterpriseEstimating.Data.Models.Scheduling;

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

        // Each section is independently idempotent — safe to call repeatedly

        await SeedUsers(db);
        await SeedUserCompanies(db);
        await SeedCostBooks(db);
        await SeedRateBooks(db);
        await SeedCrewTemplates(db);
        await SeedStaffingPlans(db);
        await SeedEstimates(db);
        await SeedDemoValeroEstimates(db);
        await AssignRateBooksToEstimates(db);
        await SeedSequences(db);
        await SeedSchedulingData(db);

        return Ok(new
        {
            message = "Seed complete (additive — existing data preserved). CSL + ETS estimates, rate books, crew templates, staffing plans, scheduling resources seeded where missing.",
        });
    }

    [HttpPost("reset")]
    public async Task<IActionResult> Reset([FromQuery] bool includeCostBooks = false)
    {
        if (!_env.IsEnvironment("Local") && !_env.IsDevelopment())
            return Forbid();

        await using var db = await _dbFactory.CreateDbContextAsync();

        // Break circular FK: Estimate.StaffingPlanId ↔ StaffingPlan.ConvertedEstimateId
        await db.Estimates.ExecuteUpdateAsync(s => s.SetProperty(e => e.StaffingPlanId, (int?)null));
        await db.StaffingPlans.ExecuteUpdateAsync(s => s.SetProperty(p => p.ConvertedEstimateId, (int?)null));

        db.FcoEntries.RemoveRange(db.FcoEntries);
        db.EstimateRevisions.RemoveRange(db.EstimateRevisions);
        db.EstimateSummaries.RemoveRange(db.EstimateSummaries);
        db.LaborRows.RemoveRange(db.LaborRows);
        db.EquipmentRows.RemoveRange(db.EquipmentRows);
        db.ExpenseRows.RemoveRange(db.ExpenseRows);
        db.Estimates.RemoveRange(db.Estimates);
        db.StaffingLaborRows.RemoveRange(db.StaffingLaborRows);
        db.StaffingPlans.RemoveRange(db.StaffingPlans);
        db.CrewTemplateRows.RemoveRange(db.CrewTemplateRows);
        db.CrewTemplates.RemoveRange(db.CrewTemplates);
        db.RateBookLaborRates.RemoveRange(db.RateBookLaborRates);
        db.RateBookEquipmentRates.RemoveRange(db.RateBookEquipmentRates);
        db.RateBookExpenseItems.RemoveRange(db.RateBookExpenseItems);
        db.RateBooks.RemoveRange(db.RateBooks);
        if (includeCostBooks)
        {
            db.CostBookLaborRates.RemoveRange(db.CostBookLaborRates);
            db.CostBookEquipmentRates.RemoveRange(db.CostBookEquipmentRates);
            db.CostBookExpenses.RemoveRange(db.CostBookExpenses);
            db.CostBookOverheadItems.RemoveRange(db.CostBookOverheadItems);
            db.CostBooks.RemoveRange(db.CostBooks);
        }

        db.EstimateSequences.RemoveRange(db.EstimateSequences);
        await db.SaveChangesAsync();

        return Ok(new
        {
            message = includeCostBooks
                ? "All estimating data, including cost books, cleared. Ready to re-seed."
                : "Estimating data cleared. Existing cost books were preserved.",
            costBooksPreserved = !includeCostBooks
        });
    }

    // ── Users ─────────────────────────────────────────────────────────────────

    private static async Task SeedUsers(AppDbContext db)
    {
        var existingUsernames = await db.Users.Select(u => u.Username).ToHashSetAsync();

        var roles = await db.Roles.ToDictionaryAsync(r => r.Name, r => r.RoleId);
        if (roles.Count == 0) return;

        var toAdd = new List<User>();

        if (!existingUsernames.Contains("estimator.csl"))
            toAdd.Add(new User { Username = "estimator.csl", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Stronghold2024"), CompanyCode = "CSL", FirstName = "James",  LastName = "Tanner",  Email = "james.tanner@catspec.com",     Active = true });
        if (!existingUsernames.Contains("estimator.ets"))
            toAdd.Add(new User { Username = "estimator.ets", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Stronghold2024"), CompanyCode = "ETS", FirstName = "Maria",  LastName = "Delgado", Email = "maria.delgado@eliteta.com",    Active = true });
        if (!existingUsernames.Contains("executive"))
            toAdd.Add(new User { Username = "executive",     PasswordHash = BCrypt.Net.BCrypt.HashPassword("Stronghold2024"), CompanyCode = "CSL", FirstName = "Robert", LastName = "Callahan",Email = "r.callahan@stronghold.local",   Active = true });

        if (toAdd.Count == 0) return;

        db.Users.AddRange(toAdd);
        await db.SaveChangesAsync();

        var userMap = await db.Users.ToDictionaryAsync(u => u.Username, u => u.UserId);

        var roleEntries = new List<UserRole>();
        if (userMap.TryGetValue("estimator.csl", out var cslId) && roles.TryGetValue("Estimator", out var estRoleId))
            roleEntries.Add(new UserRole { UserId = cslId, RoleId = estRoleId });
        if (userMap.TryGetValue("estimator.ets", out var etsId) && roles.TryGetValue("Estimator", out var estRoleId2))
            roleEntries.Add(new UserRole { UserId = etsId, RoleId = estRoleId2 });
        if (userMap.TryGetValue("executive", out var execId) && roles.TryGetValue("Analytics", out var anaRoleId))
            roleEntries.Add(new UserRole { UserId = execId, RoleId = anaRoleId });

        db.UserRoles.AddRange(roleEntries.Where(r => !db.UserRoles.Any(ur => ur.UserId == r.UserId && ur.RoleId == r.RoleId)));
        await db.SaveChangesAsync();
    }

    // ── User Companies ────────────────────────────────────────────────────────

    private static async Task SeedUserCompanies(AppDbContext db)
    {
        var existing = await db.UserCompanies
            .Select(uc => new { uc.UserId, uc.CompanyCode })
            .ToListAsync();
        var existingSet = existing.Select(x => (x.UserId, x.CompanyCode)).ToHashSet();

        var users = await db.Users.ToDictionaryAsync(u => u.Username, u => u.UserId);
        if (users.Count == 0) return;

        var desired = new List<(string username, string co)>
        {
            ("dev.user",       "CSL"),
            ("dev.user",       "ETS"),
            ("estimator.csl",  "CSL"),
            ("estimator.ets",  "ETS"),
            ("executive",      "CSL"),
        };

        var toAdd = desired
            .Where(d => users.TryGetValue(d.username, out _))
            .Select(d => new UserCompany { UserId = users[d.username], CompanyCode = d.co })
            .Where(uc => !existingSet.Contains((uc.UserId, uc.CompanyCode)))
            .ToList();

        if (toAdd.Count == 0) return;

        db.UserCompanies.AddRange(toAdd);
        await db.SaveChangesAsync();
    }

    // ── Cost Books ────────────────────────────────────────────────────────────

    private static async Task SeedCostBooks(AppDbContext db)
    {
        static List<CostBookOverheadItem> Burden() => new()
        {
            new() { Category = "Burden",    Code = "FICA",   Name = "FICA / Social Security",      BurdenType = "percentage", Value = 7.65m,  SortOrder = 1 },
            new() { Category = "Burden",    Code = "FUTA",   Name = "FUTA (Federal Unemployment)", BurdenType = "percentage", Value = 0.60m,  SortOrder = 2 },
            new() { Category = "Burden",    Code = "SUTA",   Name = "SUTA (State Unemployment)",   BurdenType = "percentage", Value = 2.70m,  SortOrder = 3 },
            new() { Category = "Insurance", Code = "WC",     Name = "Workers' Compensation",       BurdenType = "percentage", Value = 8.50m,  SortOrder = 4 },
            new() { Category = "Insurance", Code = "GL",     Name = "General Liability",           BurdenType = "percentage", Value = 2.50m,  SortOrder = 5 },
            new() { Category = "Insurance", Code = "AUTO",   Name = "Auto Insurance",              BurdenType = "percentage", Value = 1.00m,  SortOrder = 6 },
            new() { Category = "Insurance", Code = "UMB",    Name = "Umbrella / Excess",           BurdenType = "percentage", Value = 0.75m,  SortOrder = 7 },
            new() { Category = "Insurance", Code = "BOND",   Name = "Bonding",                     BurdenType = "percentage", Value = 1.50m,  SortOrder = 8 },
            new() { Category = "Other",     Code = "HEALTH", Name = "Health Benefits",             BurdenType = "percentage", Value = 6.00m,  SortOrder = 9 },
            new() { Category = "Other",     Code = "401K",   Name = "401k Match",                  BurdenType = "percentage", Value = 3.00m,  SortOrder = 10 },
            new() { Category = "Other",     Code = "TRAIN",  Name = "Training / Safety",           BurdenType = "percentage", Value = 1.00m,  SortOrder = 11 },
            new() { Category = "Other",     Code = "GA",     Name = "G&A / Admin",                 BurdenType = "percentage", Value = 5.00m,  SortOrder = 12 },
        };

        static List<CostBookLaborRate> LaborRates(decimal pfSt, decimal bmSt, decimal wdSt) => new()
        {
            new() { NavCode = "PF001",  CraftCode = "PF",  Position = "Pipefitter Journeyman",   LaborType = "Direct",   StRate = pfSt,        OtRate = pfSt * 1.5m,  DtRate = pfSt * 2m,   SortOrder = 1  },
            new() { NavCode = "PF002",  CraftCode = "PFH", Position = "Pipefitter Helper",       LaborType = "Direct",   StRate = pfSt * .67m, OtRate = pfSt * 1.0m,  DtRate = pfSt * 1.33m,SortOrder = 2  },
            new() { NavCode = "BM001",  CraftCode = "BM",  Position = "Boilermaker Journeyman",  LaborType = "Direct",   StRate = bmSt,        OtRate = bmSt * 1.5m,  DtRate = bmSt * 2m,   SortOrder = 3  },
            new() { NavCode = "BM002",  CraftCode = "BMH", Position = "Boilermaker Helper",      LaborType = "Direct",   StRate = bmSt * .67m, OtRate = bmSt * 1.0m,  DtRate = bmSt * 1.33m,SortOrder = 4  },
            new() { NavCode = "WD001",  CraftCode = "WD",  Position = "Welder Journeyman",       LaborType = "Direct",   StRate = wdSt,        OtRate = wdSt * 1.5m,  DtRate = wdSt * 2m,   SortOrder = 5  },
            new() { NavCode = "WD002",  CraftCode = "WDH", Position = "Welder Helper",           LaborType = "Direct",   StRate = wdSt * .65m, OtRate = wdSt * .98m,  DtRate = wdSt * 1.3m, SortOrder = 6  },
            new() { NavCode = "MW001",  CraftCode = "MW",  Position = "Millwright Journeyman",   LaborType = "Direct",   StRate = pfSt + 1m,   OtRate = (pfSt+1)*1.5m,DtRate = (pfSt+1)*2m, SortOrder = 7  },
            new() { NavCode = "EL001",  CraftCode = "EL",  Position = "Electrician Journeyman",  LaborType = "Direct",   StRate = bmSt,        OtRate = bmSt * 1.5m,  DtRate = bmSt * 2m,   SortOrder = 8  },
            new() { NavCode = "IT001",  CraftCode = "IE",  Position = "Instrument Tech",         LaborType = "Direct",   StRate = wdSt + 2m,   OtRate = (wdSt+2)*1.5m,DtRate = (wdSt+2)*2m, SortOrder = 9  },
            new() { NavCode = "CO001",  CraftCode = "OPR", Position = "Crane Operator",          LaborType = "Direct",   StRate = wdSt + 2m,   OtRate = (wdSt+2)*1.5m,DtRate = (wdSt+2)*2m, SortOrder = 10 },
            new() { NavCode = "RG001",  CraftCode = "RIG", Position = "Rigger",                  LaborType = "Direct",   StRate = pfSt - 2m,   OtRate = (pfSt-2)*1.5m,DtRate = (pfSt-2)*2m, SortOrder = 11 },
            new() { NavCode = "SC001",  CraftCode = "SCF", Position = "Scaffold Builder",        LaborType = "Direct",   StRate = pfSt - 6m,   OtRate = (pfSt-6)*1.5m,DtRate = (pfSt-6)*2m, SortOrder = 12 },
            new() { NavCode = "ND001",  CraftCode = "NDT", Position = "NDT Technician",          LaborType = "Direct",   StRate = wdSt + 4m,   OtRate = (wdSt+4)*1.5m,DtRate = (wdSt+4)*2m, SortOrder = 13 },
            new() { NavCode = "DR001",  CraftCode = "DRV", Position = "Driver/Teamster",         LaborType = "Direct",   StRate = pfSt - 10m,  OtRate = (pfSt-10)*1.5m,DtRate=(pfSt-10)*2m, SortOrder = 14 },
            new() { NavCode = "PM001",  CraftCode = "MGT", Position = "Project Manager",         LaborType = "Indirect", StRate = 65.00m, OtRate = 97.50m,  DtRate = 130.00m, SortOrder = 15 },
            new() { NavCode = "GF001",  CraftCode = "SUP", Position = "General Foreman",         LaborType = "Indirect", StRate = 52.00m, OtRate = 78.00m,  DtRate = 104.00m, SortOrder = 16 },
            new() { NavCode = "FM001",  CraftCode = "SUP", Position = "Foreman",                 LaborType = "Indirect", StRate = 45.00m, OtRate = 67.50m,  DtRate = 90.00m,  SortOrder = 17 },
            new() { NavCode = "SW001",  CraftCode = "SAF", Position = "Safety Watch",            LaborType = "Indirect", StRate = 26.00m, OtRate = 39.00m,  DtRate = 52.00m,  SortOrder = 18 },
            new() { NavCode = "FW001",  CraftCode = "SAF", Position = "Fire Watch",              LaborType = "Indirect", StRate = 24.00m, OtRate = 36.00m,  DtRate = 48.00m,  SortOrder = 19 },
            new() { NavCode = "HW001",  CraftCode = "SAF", Position = "Hole Watch",              LaborType = "Indirect", StRate = 24.00m, OtRate = 36.00m,  DtRate = 48.00m,  SortOrder = 20 },
        };

        static List<CostBookEquipmentRate> EquipRates() => new()
        {
            new() { Name = "Crane - 50 Ton",         Daily = 1200m, Weekly = 5000m,  Monthly = 15000m, SortOrder = 1 },
            new() { Name = "Crane - 100 Ton",        Daily = 1900m, Weekly = 8000m,  Monthly = 26000m, SortOrder = 2 },
            new() { Name = "Manlift 40ft",           Daily = 280m,  Weekly = 1100m,  Monthly = 3200m,  SortOrder = 3 },
            new() { Name = "Manlift 60ft",           Daily = 400m,  Weekly = 1600m,  Monthly = 4600m,  SortOrder = 4 },
            new() { Name = "Scissor Lift",           Daily = 175m,  Weekly = 700m,   Monthly = 2000m,  SortOrder = 5 },
            new() { Name = "Forklift 5K",            Daily = 200m,  Weekly = 800m,   Monthly = 2400m,  SortOrder = 6 },
            new() { Name = "Welding Machine 400amp", Daily = 75m,   Weekly = 300m,   Monthly = 850m,   SortOrder = 7 },
            new() { Name = "Air Compressor 185cfm",  Daily = 110m,  Weekly = 440m,   Monthly = 1300m,  SortOrder = 8 },
            new() { Name = "Light Tower",            Daily = 55m,   Weekly = 220m,   Monthly = 650m,   SortOrder = 9 },
            new() { Name = "Generator 25KW",         Daily = 105m,  Weekly = 420m,   Monthly = 1200m,  SortOrder = 10 },
        };

        static List<CostBookExpense> Expenses() => new()
        {
            new() { Category = "PerDiem", Description = "Standard Per Diem (Local)",       Rate = 65.00m,  Unit = "Day",   SortOrder = 1 },
            new() { Category = "PerDiem", Description = "Standard Per Diem (Out of Town)", Rate = 125.00m, Unit = "Day",   SortOrder = 2 },
            new() { Category = "PerDiem", Description = "Per Diem - High Cost Area",       Rate = 150.00m, Unit = "Day",   SortOrder = 3 },
            new() { Category = "Travel",  Description = "Mileage Reimbursement",           Rate = 0.67m,   Unit = "Mile",  SortOrder = 4 },
            new() { Category = "Travel",  Description = "Rental Car",                      Rate = 75.00m,  Unit = "Day",   SortOrder = 5 },
            new() { Category = "Travel",  Description = "Airfare (Average)",               Rate = 450.00m, Unit = "Trip",  SortOrder = 6 },
            new() { Category = "Lodging", Description = "Standard Hotel",                  Rate = 120.00m, Unit = "Night", SortOrder = 7 },
            new() { Category = "Lodging", Description = "Extended Stay",                   Rate = 95.00m,  Unit = "Night", SortOrder = 8 },
        };

        // Standard Cost Book rates = 60% of the lowest-priced rate book (40% below market floor).
        // CSL lowest rate book: Standard Baseline PF $78 / BM $82 / WD $85
        //   → 60% = PF $46.80 / BM $49.20 / WD $51.00  (rounded to nearest dollar)
        // ETS lowest rate book: ETS Standard PF $76 / BM $80 / WD $83
        //   → 60% = PF $45.60 / BM $48.00 / WD $49.80  (rounded)
        var existingCompanies = await db.CostBooks
            .Select(cb => cb.CompanyCode)
            .Distinct()
            .ToHashSetAsync();

        var books = new List<CostBook>();
        if (!existingCompanies.Contains("CSL"))
        {
            books.Add(new CostBook
            {
                CompanyCode = "CSL", Name = "Standard Cost Book", IsDefault = true, UpdatedBy = "dev.user",
                OverheadItems = Burden(), LaborRates = LaborRates(47m, 49m, 51m), EquipmentRates = EquipRates(), Expenses = Expenses()
            });
        }

        if (!existingCompanies.Contains("ETS"))
        {
            books.Add(new CostBook
            {
                CompanyCode = "ETS", Name = "Standard Cost Book", IsDefault = true, UpdatedBy = "dev.user",
                OverheadItems = Burden(), LaborRates = LaborRates(46m, 48m, 50m), EquipmentRates = EquipRates(), Expenses = Expenses()
            });
        }

        if (books.Count > 0)
        {
            db.CostBooks.AddRange(books);
            await db.SaveChangesAsync();
        }
    }

    // ── Rate Books ────────────────────────────────────────────────────────────

    private static async Task SeedRateBooks(AppDbContext db)
    {
        if (await db.RateBooks.AnyAsync()) return;

        static (string, string, string, decimal, decimal, decimal)[] Std() => new[]
        {
            ("Pipefitter Journeyman",  "Direct",   "PF",  78.00m, 117.00m, 156.00m),
            ("Pipefitter Helper",      "Direct",   "PFH", 52.00m,  78.00m, 104.00m),
            ("Boilermaker Journeyman", "Direct",   "BM",  82.00m, 123.00m, 164.00m),
            ("Boilermaker Helper",     "Direct",   "BMH", 54.00m,  81.00m, 108.00m),
            ("Welder Journeyman",      "Direct",   "WD",  85.00m, 127.50m, 170.00m),
            ("Welder Helper",          "Direct",   "WDH", 55.00m,  82.50m, 110.00m),
            ("Millwright Journeyman",  "Direct",   "MW",  80.00m, 120.00m, 160.00m),
            ("Electrician Journeyman", "Direct",   "EL",  82.00m, 123.00m, 164.00m),
            ("Instrument Tech",        "Direct",   "IE",  88.00m, 132.00m, 176.00m),
            ("Crane Operator",         "Direct",   "OPR", 95.00m, 142.50m, 190.00m),
            ("Rigger",                 "Direct",   "RIG", 72.00m, 108.00m, 144.00m),
            ("Scaffold Builder",       "Direct",   "SCF", 65.00m,  97.50m, 130.00m),
            ("NDT Technician",         "Direct",   "NDT", 95.00m, 142.50m, 190.00m),
            ("Project Manager",        "Indirect", "MGT", 125.00m, 187.50m, 250.00m),
            ("General Foreman",        "Indirect", "SUP",  98.00m, 147.00m, 196.00m),
            ("Foreman",                "Indirect", "SUP",  85.00m, 127.50m, 170.00m),
            ("Safety Watch",           "Indirect", "SAF",  48.00m,  72.00m,  96.00m),
            ("Fire Watch",             "Indirect", "SAF",  45.00m,  67.50m,  90.00m),
            ("Hole Watch",             "Indirect", "SAF",  45.00m,  67.50m,  90.00m),
            ("Driver/Teamster",        "Indirect", "DRV",  58.00m,  87.00m, 116.00m),
        };

        static (string, decimal?, decimal?, decimal?)[] Equip() => new[]
        {
            ("Forklift 5K",            (decimal?)185m,  (decimal?)750m,  (decimal?)2400m),
            ("Forklift 10K",           (decimal?)250m,  (decimal?)1000m, (decimal?)3200m),
            ("Crane - 50 Ton",         (decimal?)1200m, (decimal?)5000m, (decimal?)16000m),
            ("Crane - 100 Ton",        (decimal?)1800m, (decimal?)7500m, (decimal?)24000m),
            ("Manlift 40ft",           (decimal?)220m,  (decimal?)900m,  (decimal?)2800m),
            ("Manlift 60ft",           (decimal?)280m,  (decimal?)1150m, (decimal?)3600m),
            ("Scissor Lift",           (decimal?)150m,  (decimal?)600m,  (decimal?)1900m),
            ("Welding Machine 400amp", (decimal?)95m,   (decimal?)380m,  (decimal?)1200m),
            ("Air Compressor 185cfm",  (decimal?)120m,  (decimal?)480m,  (decimal?)1500m),
            ("Light Tower",            (decimal?)75m,   (decimal?)300m,  (decimal?)950m),
        };

        // Expense items match cost book descriptions exactly + include Lodging
        static (string, string, decimal, string)[] Exp() => new[]
        {
            ("PerDiem",  "Standard Per Diem (Local)",        65.00m,  "day"),
            ("PerDiem",  "Standard Per Diem (Out of Town)", 125.00m,  "day"),
            ("PerDiem",  "Per Diem - High Cost Area",       150.00m,  "day"),
            ("Travel",   "Mileage Reimbursement",             0.67m,  "mile"),
            ("Travel",   "Airfare (Average)",               450.00m,  "trip"),
            ("Travel",   "Rental Car",                       75.00m,  "day"),
            ("Lodging",  "Standard Hotel",                  120.00m,  "night"),
            ("Lodging",  "Extended Stay",                    95.00m,  "night"),
        };

        var books = new List<RateBook>
        {
            // CSL — each client has distinct rates so you can tell which book is loaded
            MakeRateBook("CSL", "Standard (Baseline)",              null, null, null, null, true,  Std(),        Equip(), Exp()),
            MakeRateBook("CSL", "Shell Deer Park, TX 2024",         "Shell Oil Company",    "SHELL", "Deer Park",  "TX", false, ShellRates(),   Equip(), Exp()),
            MakeRateBook("CSL", "BP Baytown, TX 2024",              "British Petroleum",    "BP",    "Baytown",    "TX", false, BpRates(),      Equip(), Exp()),
            MakeRateBook("CSL", "ExxonMobil Baytown, TX 2024",      "ExxonMobil",           "XOM",   "Baytown",    "TX", false, XomRates(),     Equip(), Exp()),
            MakeRateBook("CSL", "Valero Port Arthur, TX 2024",      "Valero Energy",        "VLO",   "Port Arthur","TX", false, ValeroRates(),  Equip(), Exp()),
            MakeRateBook("CSL", "Chevron Pascagoula, MS 2024",      "Chevron",              "CVX",   "Pascagoula", "MS", false, ChevronRates(), Equip(), Exp()),
            MakeRateBook("CSL", "Marathon Petroleum Texas City, TX 2024", "Marathon Petroleum Corp.", "MPC", "Texas City", "TX", false, MarathonRates(), Equip(), Exp()),
            // ETS
            MakeRateBook("ETS", "Standard (Baseline)",                    null, null, null, null, true, EtsStd(),          Equip(), Exp()),
            MakeRateBook("ETS", "Valero Corpus Christi, TX 2024",         "Valero Energy",           "VLO", "Corpus Christi", "TX", false, EtsValeroRates(),   Equip(), Exp()),
            MakeRateBook("ETS", "Flint Hills Corpus Christi, TX 2024",    "Flint Hills Resources",   "FHR", "Corpus Christi", "TX", false, EtsFlintRates(),    Equip(), Exp()),
            MakeRateBook("ETS", "Cheniere Corpus Christi, TX 2024",       "Cheniere Energy",         "CHN", "Corpus Christi", "TX", false, EtsChenieRates(),   Equip(), Exp()),
        };

        db.RateBooks.AddRange(books);
        await db.SaveChangesAsync();
    }

    // Helper to build a full 20-position rate array from 3 anchor rates
    private static (string, string, string, decimal, decimal, decimal)[] MakeRates(decimal pf, decimal bm, decimal wd) => new[]
    {
        ("Pipefitter Journeyman",  "Direct",   "PF",  pf,          pf*1.5m,       pf*2m        ),
        ("Pipefitter Helper",      "Direct",   "PFH", pf*.67m,     pf*1.005m,     pf*1.34m     ),
        ("Boilermaker Journeyman", "Direct",   "BM",  bm,          bm*1.5m,       bm*2m        ),
        ("Boilermaker Helper",     "Direct",   "BMH", bm*.67m,     bm*1.005m,     bm*1.34m     ),
        ("Welder Journeyman",      "Direct",   "WD",  wd,          wd*1.5m,       wd*2m        ),
        ("Welder Helper",          "Direct",   "WDH", wd*.65m,     wd*.975m,      wd*1.3m      ),
        ("Millwright Journeyman",  "Direct",   "MW",  pf+2m,       (pf+2m)*1.5m,  (pf+2m)*2m  ),
        ("Electrician Journeyman", "Direct",   "EL",  bm,          bm*1.5m,       bm*2m        ),
        ("Instrument Tech",        "Direct",   "IE",  wd+3m,       (wd+3m)*1.5m,  (wd+3m)*2m  ),
        ("Crane Operator",         "Direct",   "OPR", wd+10m,      (wd+10m)*1.5m, (wd+10m)*2m ),
        ("Rigger",                 "Direct",   "RIG", pf-4m,       (pf-4m)*1.5m,  (pf-4m)*2m  ),
        ("Scaffold Builder",       "Direct",   "SCF", pf-10m,      (pf-10m)*1.5m, (pf-10m)*2m ),
        ("NDT Technician",         "Direct",   "NDT", wd+8m,       (wd+8m)*1.5m,  (wd+8m)*2m  ),
        ("Driver/Teamster",        "Indirect", "DRV", pf-12m,      (pf-12m)*1.5m, (pf-12m)*2m ),
        ("Project Manager",        "Indirect", "MGT", pf+47m,      (pf+47m)*1.5m, (pf+47m)*2m ),
        ("General Foreman",        "Indirect", "SUP", pf+20m,      (pf+20m)*1.5m, (pf+20m)*2m ),
        ("Foreman",                "Indirect", "SUP", pf+7m,       (pf+7m)*1.5m,  (pf+7m)*2m  ),
        ("Safety Watch",           "Indirect", "SAF", pf-30m,      (pf-30m)*1.5m, (pf-30m)*2m ),
        ("Fire Watch",             "Indirect", "SAF", pf-33m,      (pf-33m)*1.5m, (pf-33m)*2m ),
        ("Hole Watch",             "Indirect", "SAF", pf-33m,      (pf-33m)*1.5m, (pf-33m)*2m ),
    };

    // CSL rate books — clearly different rates per client so you can see which book loaded
    // Standard baseline: PF $78 / BM $82 / WD $85
    private static (string, string, string, decimal, decimal, decimal)[] BpRates()      => MakeRates(80m, 84m, 87m);   // BP: +$2 across
    private static (string, string, string, decimal, decimal, decimal)[] ShellRates()   => MakeRates(84m, 88m, 91m);   // Shell: +$6 premium
    private static (string, string, string, decimal, decimal, decimal)[] XomRates()     => MakeRates(82m, 86m, 89m);   // ExxonMobil: +$4
    private static (string, string, string, decimal, decimal, decimal)[] ValeroRates()  => MakeRates(79m, 83m, 86m);   // Valero PA: +$1
    private static (string, string, string, decimal, decimal, decimal)[] ChevronRates() => MakeRates(86m, 90m, 93m);   // Chevron MS: +$8 out-of-state
    private static (string, string, string, decimal, decimal, decimal)[] MarathonRates()=> MakeRates(78m, 82m, 85m);   // Marathon: standard (same as baseline)

    // ETS rate books — slightly lower market, distinct per client
    private static (string, string, string, decimal, decimal, decimal)[] EtsStd()         => MakeRates(76m, 80m, 83m);   // ETS baseline
    private static (string, string, string, decimal, decimal, decimal)[] EtsValeroRates() => MakeRates(77m, 81m, 84m);   // Valero CC: +$1
    private static (string, string, string, decimal, decimal, decimal)[] EtsFlintRates()  => MakeRates(79m, 83m, 86m);   // Flint Hills: +$3
    private static (string, string, string, decimal, decimal, decimal)[] EtsChenieRates() => MakeRates(88m, 92m, 95m);   // Cheniere LNG: +$12 (hazmat premium)

    private static RateBook MakeRateBook(
        string co, string name, string? client, string? clientCode, string? city, string? state, bool isBaseline,
        (string pos, string type, string cc, decimal st, decimal ot, decimal dt)[] labor,
        (string name, decimal? daily, decimal? weekly, decimal? monthly)[] equip,
        (string cat, string desc, decimal rate, string unit)[] exp)
        => new()
        {
            CompanyCode = co, Name = name, Client = client, ClientCode = clientCode,
            City = city, State = state, IsStandardBaseline = isBaseline,
            EffectiveDate = new DateTime(2024, 1, 1), ExpiresDate = new DateTime(2026, 12, 31),
            CreatedBy = "dev.user",
            LaborRates = labor.Select((r, i) => new RateBookLaborRate
            {
                Position = r.pos, LaborType = r.type, CraftCode = r.cc, NavCode = r.cc,
                StRate = r.st, OtRate = r.ot, DtRate = r.dt, SortOrder = i + 1
            }).ToList(),
            EquipmentRates = equip.Select((r, i) => new RateBookEquipmentRate
            {
                Name = r.name, Daily = r.daily, Weekly = r.weekly, Monthly = r.monthly, SortOrder = i + 1
            }).ToList(),
            ExpenseItems = exp.Select((r, i) => new RateBookExpenseItem
            {
                Category = r.cat, Description = r.desc, Rate = r.rate, Unit = r.unit, SortOrder = i + 1
            }).ToList()
        };

    // ── Crew Templates ────────────────────────────────────────────────────────

    private static async Task SeedCrewTemplates(AppDbContext db)
    {
        if (await db.CrewTemplates.AnyAsync()) return;

        db.CrewTemplates.AddRange(
            new CrewTemplate
            {
                CompanyCode = "CSL", Name = "Standard Piping Crew", Description = "6-man piping crew for turnarounds",
                Rows = new List<CrewTemplateRow>
                {
                    new() { Position = "Foreman",               LaborType = "Indirect", CraftCode = "SUP", Qty = 1, SortOrder = 1 },
                    new() { Position = "Pipefitter Journeyman", LaborType = "Direct",   CraftCode = "PF",  Qty = 3, SortOrder = 2 },
                    new() { Position = "Pipefitter Helper",     LaborType = "Direct",   CraftCode = "PFH", Qty = 2, SortOrder = 3 },
                }
            },
            new CrewTemplate
            {
                CompanyCode = "CSL", Name = "Turnaround Package Crew", Description = "Full turnaround crew with supervision",
                Rows = new List<CrewTemplateRow>
                {
                    new() { Position = "Project Manager",        LaborType = "Indirect", CraftCode = "MGT", Qty = 1, SortOrder = 1 },
                    new() { Position = "General Foreman",        LaborType = "Indirect", CraftCode = "SUP", Qty = 1, SortOrder = 2 },
                    new() { Position = "Foreman",                LaborType = "Indirect", CraftCode = "SUP", Qty = 2, SortOrder = 3 },
                    new() { Position = "Safety Watch",           LaborType = "Indirect", CraftCode = "SAF", Qty = 2, SortOrder = 4 },
                    new() { Position = "Pipefitter Journeyman",  LaborType = "Direct",   CraftCode = "PF",  Qty = 6, SortOrder = 5 },
                    new() { Position = "Pipefitter Helper",      LaborType = "Direct",   CraftCode = "PFH", Qty = 4, SortOrder = 6 },
                    new() { Position = "Welder Journeyman",      LaborType = "Direct",   CraftCode = "WD",  Qty = 2, SortOrder = 7 },
                    new() { Position = "Boilermaker Journeyman", LaborType = "Direct",   CraftCode = "BM",  Qty = 2, SortOrder = 8 },
                }
            },
            new CrewTemplate
            {
                CompanyCode = "CSL", Name = "Boilermaker Crew", Description = "Vessel and exchanger work",
                Rows = new List<CrewTemplateRow>
                {
                    new() { Position = "Foreman",                LaborType = "Indirect", CraftCode = "SUP", Qty = 1, SortOrder = 1 },
                    new() { Position = "Boilermaker Journeyman", LaborType = "Direct",   CraftCode = "BM",  Qty = 3, SortOrder = 2 },
                    new() { Position = "Boilermaker Helper",     LaborType = "Direct",   CraftCode = "BMH", Qty = 2, SortOrder = 3 },
                }
            },
            new CrewTemplate
            {
                CompanyCode = "ETS", Name = "ETS Standard Turnaround Crew", Description = "Core TA crew for South TX refineries",
                Rows = new List<CrewTemplateRow>
                {
                    new() { Position = "General Foreman",        LaborType = "Indirect", CraftCode = "SUP", Qty = 1, SortOrder = 1 },
                    new() { Position = "Foreman",                LaborType = "Indirect", CraftCode = "SUP", Qty = 2, SortOrder = 2 },
                    new() { Position = "Safety Watch",           LaborType = "Indirect", CraftCode = "SAF", Qty = 2, SortOrder = 3 },
                    new() { Position = "Pipefitter Journeyman",  LaborType = "Direct",   CraftCode = "PF",  Qty = 5, SortOrder = 4 },
                    new() { Position = "Pipefitter Helper",      LaborType = "Direct",   CraftCode = "PFH", Qty = 3, SortOrder = 5 },
                    new() { Position = "Welder Journeyman",      LaborType = "Direct",   CraftCode = "WD",  Qty = 2, SortOrder = 6 },
                }
            },
            new CrewTemplate
            {
                CompanyCode = "ETS", Name = "ETS Inspection Crew", Description = "NDT and instrument tech crew",
                Rows = new List<CrewTemplateRow>
                {
                    new() { Position = "Foreman",         LaborType = "Indirect", CraftCode = "SUP", Qty = 1, SortOrder = 1 },
                    new() { Position = "NDT Technician",  LaborType = "Direct",   CraftCode = "NDT", Qty = 2, SortOrder = 2 },
                    new() { Position = "Instrument Tech", LaborType = "Direct",   CraftCode = "IE",  Qty = 2, SortOrder = 3 },
                }
            }
        );
        await db.SaveChangesAsync();
    }

    // ── Staffing Plans ────────────────────────────────────────────────────────

    private static async Task SeedStaffingPlans(AppDbContext db)
    {
        if (await db.StaffingPlans.AnyAsync()) return;

        var plans = new List<StaffingPlan>
        {
            // CSL Approved plans (show in future revenue)
            new()
            {
                CompanyCode = "CSL", StaffingPlanNumber = "SP-S-26-0010-BP",
                Name = "BP Baytown Unit 5 Turnaround", Client = "British Petroleum", ClientCode = "BP",
                Branch = "Industrial", City = "Baytown", State = "TX",
                Status = "Approved", Shift = "Both", HoursPerShift = 12, Days = 26,
                StartDate = new DateTime(2026, 4, 7), EndDate = new DateTime(2026, 5, 2),
                OtMethod = "daily8_weekly40", DtWeekends = "sat_sun", RoughLaborTotal = 612400m, CreatedBy = "james.tanner",
                LaborRows = SpLaborRows(new DateTime(2026, 4, 7), 26, new[]
                {
                    ("Project Manager",        "Indirect", "MGT", 65.00m, 97.50m,  130.00m, 1),
                    ("General Foreman",        "Indirect", "SUP", 52.00m, 78.00m,  104.00m, 1),
                    ("Foreman",                "Indirect", "SUP", 45.00m, 67.50m,   90.00m, 2),
                    ("Safety Watch",           "Indirect", "SAF", 26.00m, 39.00m,   52.00m, 3),
                    ("Pipefitter Journeyman",  "Direct",   "PF",  42.00m, 63.00m,   84.00m, 6),
                    ("Pipefitter Helper",      "Direct",   "PFH", 28.00m, 42.00m,   56.00m, 4),
                    ("Welder Journeyman",      "Direct",   "WD",  46.00m, 69.00m,   92.00m, 3),
                    ("Boilermaker Journeyman", "Direct",   "BM",  44.00m, 66.00m,   88.00m, 3),
                    ("Crane Operator",         "Direct",   "OPR", 48.00m, 72.00m,   96.00m, 1),
                    ("Rigger",                 "Direct",   "RIG", 40.00m, 60.00m,   80.00m, 1),
                })
            },
            new()
            {
                CompanyCode = "CSL", StaffingPlanNumber = "SP-S-26-0012-XOM",
                Name = "ExxonMobil Baton Rouge Delayed Coker TA", Client = "ExxonMobil", ClientCode = "XOM",
                Branch = "Industrial", City = "Baton Rouge", State = "LA",
                Status = "Approved", Shift = "Both", HoursPerShift = 12, Days = 35,
                StartDate = new DateTime(2026, 4, 14), EndDate = new DateTime(2026, 5, 18),
                OtMethod = "daily8_weekly40", DtWeekends = "sat_sun", RoughLaborTotal = 821500m, CreatedBy = "carol.whitfield",
                LaborRows = SpLaborRows(new DateTime(2026, 4, 14), 35, new[]
                {
                    ("Project Manager",        "Indirect", "MGT", 65.00m, 97.50m,  130.00m, 1),
                    ("General Foreman",        "Indirect", "SUP", 52.00m, 78.00m,  104.00m, 1),
                    ("Foreman",                "Indirect", "SUP", 45.00m, 67.50m,   90.00m, 2),
                    ("Safety Watch",           "Indirect", "SAF", 26.00m, 39.00m,   52.00m, 4),
                    ("Pipefitter Journeyman",  "Direct",   "PF",  42.00m, 63.00m,   84.00m, 7),
                    ("Pipefitter Helper",      "Direct",   "PFH", 28.00m, 42.00m,   56.00m, 4),
                    ("Welder Journeyman",      "Direct",   "WD",  46.00m, 69.00m,   92.00m, 3),
                    ("Boilermaker Journeyman", "Direct",   "BM",  44.00m, 66.00m,   88.00m, 3),
                    ("Crane Operator",         "Direct",   "OPR", 48.00m, 72.00m,   96.00m, 1),
                    ("Rigger",                 "Direct",   "RIG", 40.00m, 60.00m,   80.00m, 1),
                })
            },
            new()
            {
                CompanyCode = "CSL", StaffingPlanNumber = "SP-S-26-0024-SHELL",
                Name = "Shell Deer Park CDU Turnaround", Client = "Shell Oil Company", ClientCode = "SHELL",
                Branch = "Industrial", City = "Deer Park", State = "TX",
                Status = "Approved", Shift = "Both", HoursPerShift = 12, Days = 28,
                StartDate = new DateTime(2026, 6, 2), EndDate = new DateTime(2026, 6, 29),
                OtMethod = "daily8_weekly40", DtWeekends = "sat_sun", RoughLaborTotal = 734200m, CreatedBy = "michael.santos",
                LaborRows = SpLaborRows(new DateTime(2026, 6, 2), 28, new[]
                {
                    ("Project Manager",        "Indirect", "MGT", 65.00m, 97.50m,  130.00m, 1),
                    ("General Foreman",        "Indirect", "SUP", 52.00m, 78.00m,  104.00m, 1),
                    ("Foreman",                "Indirect", "SUP", 45.00m, 67.50m,   90.00m, 2),
                    ("Safety Watch",           "Indirect", "SAF", 26.00m, 39.00m,   52.00m, 3),
                    ("Fire Watch",             "Indirect", "SAF", 24.00m, 36.00m,   48.00m, 2),
                    ("Pipefitter Journeyman",  "Direct",   "PF",  42.00m, 63.00m,   84.00m, 7),
                    ("Pipefitter Helper",      "Direct",   "PFH", 28.00m, 42.00m,   56.00m, 4),
                    ("Welder Journeyman",      "Direct",   "WD",  46.00m, 69.00m,   92.00m, 3),
                    ("Welder Helper",          "Direct",   "WDH", 30.00m, 45.00m,   60.00m, 2),
                    ("Boilermaker Journeyman", "Direct",   "BM",  44.00m, 66.00m,   88.00m, 2),
                })
            },
            new()
            {
                CompanyCode = "CSL", StaffingPlanNumber = "SP-S-26-0007-XOM-FUTURE",
                Name = "ExxonMobil Baytown Expansion", Client = "ExxonMobil", ClientCode = "XOM",
                Branch = "Industrial", City = "Baytown", State = "TX",
                Status = "Draft", Shift = "Day", HoursPerShift = 10, Days = 28,
                StartDate = new DateTime(2026, 7, 6), EndDate = new DateTime(2026, 8, 2),
                OtMethod = "daily8_weekly40", DtWeekends = "none", RoughLaborTotal = 418000m, CreatedBy = "tom.bishop",
                LaborRows = SpLaborRows(new DateTime(2026, 7, 6), 28, new[]
                {
                    ("General Foreman",       "Indirect", "SUP", 52.00m, 78.00m,  104.00m, 1),
                    ("Foreman",               "Indirect", "SUP", 45.00m, 67.50m,   90.00m, 1),
                    ("Pipefitter Journeyman", "Direct",   "PF",  42.00m, 63.00m,   84.00m, 5),
                    ("Pipefitter Helper",     "Direct",   "PFH", 28.00m, 42.00m,   56.00m, 3),
                    ("Welder Journeyman",     "Direct",   "WD",  46.00m, 69.00m,   92.00m, 2),
                })
            },
            new()
            {
                CompanyCode = "CSL", StaffingPlanNumber = "SP-S-26-0018-CVX",
                Name = "Chevron Pascagoula Piping Modification", Client = "Chevron", ClientCode = "CVX",
                Branch = "Industrial", City = "Pascagoula", State = "MS",
                Status = "Draft", Shift = "Day", HoursPerShift = 10, Days = 21,
                StartDate = new DateTime(2026, 5, 11), EndDate = new DateTime(2026, 5, 31),
                OtMethod = "daily8_weekly40", DtWeekends = "none", RoughLaborTotal = 224000m, CreatedBy = "sarah.mendez",
                LaborRows = SpLaborRows(new DateTime(2026, 5, 11), 21, new[]
                {
                    ("General Foreman",       "Indirect", "SUP", 52.00m, 78.00m,  104.00m, 1),
                    ("Foreman",               "Indirect", "SUP", 45.00m, 67.50m,   90.00m, 1),
                    ("Pipefitter Journeyman", "Direct",   "PF",  42.00m, 63.00m,   84.00m, 4),
                    ("Pipefitter Helper",     "Direct",   "PFH", 28.00m, 42.00m,   56.00m, 2),
                    ("Welder Journeyman",     "Direct",   "WD",  46.00m, 69.00m,   92.00m, 1),
                })
            },
            // ETS Staffing Plans
            new()
            {
                CompanyCode = "ETS", StaffingPlanNumber = "SP-H-26-007-CHN",
                Name = "Cheniere Corpus Christi LNG Pre-TA", Client = "Cheniere Energy", ClientCode = "CHN",
                Branch = "Industrial", City = "Corpus Christi", State = "TX",
                Status = "Approved", Shift = "Both", HoursPerShift = 12, Days = 31,
                StartDate = new DateTime(2026, 4, 10), EndDate = new DateTime(2026, 5, 10),
                OtMethod = "daily8_weekly40", DtWeekends = "sat_sun", RoughLaborTotal = 529400m, CreatedBy = "r.santos",
                LaborRows = SpLaborRows(new DateTime(2026, 4, 10), 31, new[]
                {
                    ("Project Manager",       "Indirect", "MGT", 65.00m, 97.50m,  130.00m, 1),
                    ("General Foreman",       "Indirect", "SUP", 52.00m, 78.00m,  104.00m, 1),
                    ("Foreman",               "Indirect", "SUP", 45.00m, 67.50m,   90.00m, 2),
                    ("Safety Watch",          "Indirect", "SAF", 26.00m, 39.00m,   52.00m, 2),
                    ("Pipefitter Journeyman", "Direct",   "PF",  43.00m, 64.50m,   86.00m, 6),
                    ("Pipefitter Helper",     "Direct",   "PFH", 29.00m, 43.50m,   58.00m, 4),
                    ("Welder Journeyman",     "Direct",   "WD",  47.00m, 70.50m,   94.00m, 3),
                    ("Boilermaker Journeyman","Direct",   "BM",  45.00m, 67.50m,   90.00m, 2),
                    ("Rigger",               "Direct",   "RIG", 40.00m, 60.00m,   80.00m, 1),
                })
            },
            new()
            {
                CompanyCode = "ETS", StaffingPlanNumber = "SP-H-26-002-CTG",
                Name = "CITGO Corpus Christi FCC Turnaround", Client = "CITGO Petroleum", ClientCode = "CTG",
                Branch = "Industrial", City = "Corpus Christi", State = "TX",
                Status = "Approved", Shift = "Both", HoursPerShift = 12, Days = 28,
                StartDate = new DateTime(2026, 5, 5), EndDate = new DateTime(2026, 6, 1),
                OtMethod = "daily8_weekly40", DtWeekends = "sat_sun", RoughLaborTotal = 468000m, CreatedBy = "m.delgado",
                LaborRows = SpLaborRows(new DateTime(2026, 5, 5), 28, new[]
                {
                    ("General Foreman",       "Indirect", "SUP", 52.00m, 78.00m,  104.00m, 1),
                    ("Foreman",               "Indirect", "SUP", 45.00m, 67.50m,   90.00m, 2),
                    ("Safety Watch",          "Indirect", "SAF", 26.00m, 39.00m,   52.00m, 2),
                    ("Pipefitter Journeyman", "Direct",   "PF",  43.00m, 64.50m,   86.00m, 6),
                    ("Pipefitter Helper",     "Direct",   "PFH", 29.00m, 43.50m,   58.00m, 4),
                    ("Welder Journeyman",     "Direct",   "WD",  47.00m, 70.50m,   94.00m, 2),
                    ("Boilermaker Journeyman","Direct",   "BM",  45.00m, 67.50m,   90.00m, 1),
                })
            },
            new()
            {
                CompanyCode = "ETS", StaffingPlanNumber = "SP-H-26-011-CHN",
                Name = "Cheniere Sabine Pass Unit 4 Tie-in", Client = "Cheniere Energy", ClientCode = "CHN",
                Branch = "Industrial", City = "Sabine Pass", State = "TX",
                Status = "Draft", Shift = "Both", HoursPerShift = 12, Days = 21,
                StartDate = new DateTime(2026, 6, 16), EndDate = new DateTime(2026, 7, 6),
                OtMethod = "daily8_weekly40", DtWeekends = "sat_sun", RoughLaborTotal = 362000m, CreatedBy = "r.santos",
                LaborRows = SpLaborRows(new DateTime(2026, 6, 16), 21, new[]
                {
                    ("General Foreman",       "Indirect", "SUP", 52.00m, 78.00m,  104.00m, 1),
                    ("Foreman",               "Indirect", "SUP", 45.00m, 67.50m,   90.00m, 2),
                    ("Safety Watch",          "Indirect", "SAF", 26.00m, 39.00m,   52.00m, 2),
                    ("Pipefitter Journeyman", "Direct",   "PF",  43.00m, 64.50m,   86.00m, 5),
                    ("Pipefitter Helper",     "Direct",   "PFH", 29.00m, 43.50m,   58.00m, 3),
                    ("Welder Journeyman",     "Direct",   "WD",  47.00m, 70.50m,   94.00m, 2),
                    ("Boilermaker Journeyman","Direct",   "BM",  45.00m, 67.50m,   90.00m, 1),
                })
            },
            new()
            {
                CompanyCode = "ETS", StaffingPlanNumber = "SP-H-26-010-CTG",
                Name = "CITGO Lake Charles Turnaround", Client = "CITGO Petroleum", ClientCode = "CTG",
                Branch = "Industrial", City = "Lake Charles", State = "LA",
                Status = "Draft", Shift = "Both", HoursPerShift = 12, Days = 28,
                StartDate = new DateTime(2026, 6, 30), EndDate = new DateTime(2026, 7, 27),
                OtMethod = "daily8_weekly40", DtWeekends = "sat_sun", RoughLaborTotal = 512000m, CreatedBy = "m.delgado",
                LaborRows = SpLaborRows(new DateTime(2026, 6, 30), 28, new[]
                {
                    ("General Foreman",       "Indirect", "SUP", 52.00m, 78.00m,  104.00m, 1),
                    ("Foreman",               "Indirect", "SUP", 45.00m, 67.50m,   90.00m, 2),
                    ("Safety Watch",          "Indirect", "SAF", 26.00m, 39.00m,   52.00m, 3),
                    ("Pipefitter Journeyman", "Direct",   "PF",  43.00m, 64.50m,   86.00m, 7),
                    ("Pipefitter Helper",     "Direct",   "PFH", 29.00m, 43.50m,   58.00m, 4),
                    ("Welder Journeyman",     "Direct",   "WD",  47.00m, 70.50m,   94.00m, 2),
                    ("Boilermaker Journeyman","Direct",   "BM",  45.00m, 67.50m,   90.00m, 1),
                })
            },
            new()
            {
                CompanyCode = "ETS", StaffingPlanNumber = "SP-H-26-004-FHR",
                Name = "Flint Hills Resources CDU Tie-in", Client = "Flint Hills Resources", ClientCode = "FHR",
                Branch = "Industrial", City = "Corpus Christi", State = "TX",
                Status = "Approved", Shift = "Day", HoursPerShift = 10, Days = 21,
                StartDate = new DateTime(2026, 3, 9), EndDate = new DateTime(2026, 3, 29),
                OtMethod = "daily8_weekly40", DtWeekends = "none", RoughLaborTotal = 218000m, CreatedBy = "r.santos",
                LaborRows = SpLaborRows(new DateTime(2026, 3, 9), 21, new[]
                {
                    ("General Foreman",       "Indirect", "SUP", 52.00m, 78.00m,  104.00m, 1),
                    ("Foreman",               "Indirect", "SUP", 45.00m, 67.50m,   90.00m, 1),
                    ("Pipefitter Journeyman", "Direct",   "PF",  43.00m, 64.50m,   86.00m, 4),
                    ("Pipefitter Helper",     "Direct",   "PFH", 29.00m, 43.50m,   58.00m, 3),
                    ("Welder Journeyman",     "Direct",   "WD",  47.00m, 70.50m,   94.00m, 2),
                    ("Boilermaker Journeyman","Direct",   "BM",  45.00m, 67.50m,   90.00m, 1),
                })
            },

            // ── Expired 2025 Staffing Plans (historical, never converted) ───
            new()
            {
                CompanyCode = "CSL", StaffingPlanNumber = "SP-S-25-0003-XOM",
                Name = "ExxonMobil Baytown Olefins Turnaround 2025", Client = "ExxonMobil", ClientCode = "XOM",
                Branch = "Industrial", City = "Baytown", State = "TX",
                Status = "Approved", Shift = "Both", HoursPerShift = 12, Days = 28,
                StartDate = new DateTime(2025, 3, 3), EndDate = new DateTime(2025, 3, 30),
                OtMethod = "daily8_weekly40", DtWeekends = "sat_sun", RoughLaborTotal = 682000m, CreatedBy = "carol.whitfield",
                LaborRows = SpLaborRows(new DateTime(2025, 3, 3), 28, new[]
                {
                    ("Project Manager",        "Indirect", "MGT", 65.00m, 97.50m,  130.00m, 1),
                    ("General Foreman",        "Indirect", "SUP", 52.00m, 78.00m,  104.00m, 1),
                    ("Foreman",                "Indirect", "SUP", 45.00m, 67.50m,   90.00m, 2),
                    ("Safety Watch",           "Indirect", "SAF", 26.00m, 39.00m,   52.00m, 3),
                    ("Pipefitter Journeyman",  "Direct",   "PF",  42.00m, 63.00m,   84.00m, 7),
                    ("Pipefitter Helper",      "Direct",   "PFH", 28.00m, 42.00m,   56.00m, 4),
                    ("Welder Journeyman",      "Direct",   "WD",  46.00m, 69.00m,   92.00m, 3),
                    ("Boilermaker Journeyman", "Direct",   "BM",  44.00m, 66.00m,   88.00m, 2),
                })
            },
            new()
            {
                CompanyCode = "CSL", StaffingPlanNumber = "SP-S-25-0009-SHELL",
                Name = "Shell Deer Park Coker Turnaround 2025", Client = "Shell Oil Company", ClientCode = "SHELL",
                Branch = "Industrial", City = "Deer Park", State = "TX",
                Status = "Approved", Shift = "Both", HoursPerShift = 12, Days = 35,
                StartDate = new DateTime(2025, 9, 8), EndDate = new DateTime(2025, 10, 12),
                OtMethod = "daily8_weekly40", DtWeekends = "sat_sun", RoughLaborTotal = 924000m, CreatedBy = "michael.santos",
                LaborRows = SpLaborRows(new DateTime(2025, 9, 8), 35, new[]
                {
                    ("Project Manager",        "Indirect", "MGT", 65.00m, 97.50m,  130.00m, 1),
                    ("General Foreman",        "Indirect", "SUP", 52.00m, 78.00m,  104.00m, 2),
                    ("Foreman",                "Indirect", "SUP", 45.00m, 67.50m,   90.00m, 3),
                    ("Safety Watch",           "Indirect", "SAF", 26.00m, 39.00m,   52.00m, 4),
                    ("Fire Watch",             "Indirect", "SAF", 24.00m, 36.00m,   48.00m, 2),
                    ("Pipefitter Journeyman",  "Direct",   "PF",  42.00m, 63.00m,   84.00m, 9),
                    ("Pipefitter Helper",      "Direct",   "PFH", 28.00m, 42.00m,   56.00m, 6),
                    ("Welder Journeyman",      "Direct",   "WD",  46.00m, 69.00m,   92.00m, 4),
                    ("Boilermaker Journeyman", "Direct",   "BM",  44.00m, 66.00m,   88.00m, 3),
                    ("Crane Operator",         "Direct",   "OPR", 48.00m, 72.00m,   96.00m, 1),
                })
            },
            new()
            {
                CompanyCode = "ETS", StaffingPlanNumber = "SP-H-25-005-VLO",
                Name = "Valero Texas City FCC Turnaround 2025", Client = "Valero Energy", ClientCode = "VLO",
                Branch = "Industrial", City = "Texas City", State = "TX",
                Status = "Approved", Shift = "Both", HoursPerShift = 12, Days = 28,
                StartDate = new DateTime(2025, 10, 6), EndDate = new DateTime(2025, 11, 2),
                OtMethod = "daily8_weekly40", DtWeekends = "sat_sun", RoughLaborTotal = 548000m, CreatedBy = "m.delgado",
                LaborRows = SpLaborRows(new DateTime(2025, 10, 6), 28, new[]
                {
                    ("General Foreman",       "Indirect", "SUP", 52.00m, 78.00m,  104.00m, 1),
                    ("Foreman",               "Indirect", "SUP", 45.00m, 67.50m,   90.00m, 2),
                    ("Safety Watch",          "Indirect", "SAF", 26.00m, 39.00m,   52.00m, 2),
                    ("Pipefitter Journeyman", "Direct",   "PF",  43.00m, 64.50m,   86.00m, 7),
                    ("Pipefitter Helper",     "Direct",   "PFH", 29.00m, 43.50m,   58.00m, 4),
                    ("Welder Journeyman",     "Direct",   "WD",  47.00m, 70.50m,   94.00m, 3),
                    ("Boilermaker Journeyman","Direct",   "BM",  45.00m, 67.50m,   90.00m, 2),
                })
            },

            // ── CSL 2027 Staffing Plans ──────────────────────────────────────
            new()
            {
                CompanyCode = "CSL", StaffingPlanNumber = "SP-S-27-0001-SHELL",
                Name = "Shell Deer Park FHT Turnaround 2027", Client = "Shell Oil Company", ClientCode = "SHELL",
                Branch = "Industrial", City = "Deer Park", State = "TX",
                Status = "Approved", Shift = "Both", HoursPerShift = 12, Days = 28,
                StartDate = new DateTime(2027, 1, 11), EndDate = new DateTime(2027, 2, 7),
                OtMethod = "daily8_weekly40", DtWeekends = "sat_sun", RoughLaborTotal = 748000m, CreatedBy = "michael.santos",
                LaborRows = SpLaborRows(new DateTime(2027, 1, 11), 28, new[]
                {
                    ("Project Manager",        "Indirect", "MGT", 65.00m, 97.50m,  130.00m, 1),
                    ("General Foreman",        "Indirect", "SUP", 52.00m, 78.00m,  104.00m, 1),
                    ("Foreman",                "Indirect", "SUP", 45.00m, 67.50m,   90.00m, 2),
                    ("Safety Watch",           "Indirect", "SAF", 26.00m, 39.00m,   52.00m, 3),
                    ("Fire Watch",             "Indirect", "SAF", 24.00m, 36.00m,   48.00m, 2),
                    ("Pipefitter Journeyman",  "Direct",   "PF",  42.00m, 63.00m,   84.00m, 8),
                    ("Pipefitter Helper",      "Direct",   "PFH", 28.00m, 42.00m,   56.00m, 5),
                    ("Welder Journeyman",      "Direct",   "WD",  46.00m, 69.00m,   92.00m, 4),
                    ("Boilermaker Journeyman", "Direct",   "BM",  44.00m, 66.00m,   88.00m, 3),
                    ("Crane Operator",         "Direct",   "OPR", 48.00m, 72.00m,   96.00m, 1),
                })
            },
            new()
            {
                CompanyCode = "CSL", StaffingPlanNumber = "SP-S-27-0003-XOM",
                Name = "ExxonMobil Beaumont FCCU Turnaround 2027", Client = "ExxonMobil", ClientCode = "XOM",
                Branch = "Industrial", City = "Beaumont", State = "TX",
                Status = "Approved", Shift = "Both", HoursPerShift = 12, Days = 35,
                StartDate = new DateTime(2027, 2, 8), EndDate = new DateTime(2027, 3, 14),
                OtMethod = "daily8_weekly40", DtWeekends = "sat_sun", RoughLaborTotal = 912000m, CreatedBy = "carol.whitfield",
                LaborRows = SpLaborRows(new DateTime(2027, 2, 8), 35, new[]
                {
                    ("Project Manager",        "Indirect", "MGT", 65.00m, 97.50m,  130.00m, 1),
                    ("General Foreman",        "Indirect", "SUP", 52.00m, 78.00m,  104.00m, 2),
                    ("Foreman",                "Indirect", "SUP", 45.00m, 67.50m,   90.00m, 3),
                    ("Safety Watch",           "Indirect", "SAF", 26.00m, 39.00m,   52.00m, 4),
                    ("Pipefitter Journeyman",  "Direct",   "PF",  42.00m, 63.00m,   84.00m, 9),
                    ("Pipefitter Helper",      "Direct",   "PFH", 28.00m, 42.00m,   56.00m, 6),
                    ("Welder Journeyman",      "Direct",   "WD",  46.00m, 69.00m,   92.00m, 4),
                    ("Boilermaker Journeyman", "Direct",   "BM",  44.00m, 66.00m,   88.00m, 4),
                    ("Crane Operator",         "Direct",   "OPR", 48.00m, 72.00m,   96.00m, 2),
                    ("Rigger",                 "Direct",   "RIG", 40.00m, 60.00m,   80.00m, 2),
                })
            },
            new()
            {
                CompanyCode = "CSL", StaffingPlanNumber = "SP-S-27-0005-DOW",
                Name = "Dow Freeport Unit 6 Piping Tie-in", Client = "Dow Chemical", ClientCode = "DOW",
                Branch = "Industrial", City = "Freeport", State = "TX",
                Status = "Draft", Shift = "Day", HoursPerShift = 10, Days = 21,
                StartDate = new DateTime(2027, 4, 5), EndDate = new DateTime(2027, 4, 25),
                OtMethod = "daily8_weekly40", DtWeekends = "none", RoughLaborTotal = 385000m, CreatedBy = "james.tanner",
                LaborRows = SpLaborRows(new DateTime(2027, 4, 5), 21, new[]
                {
                    ("General Foreman",       "Indirect", "SUP", 52.00m, 78.00m,  104.00m, 1),
                    ("Foreman",               "Indirect", "SUP", 45.00m, 67.50m,   90.00m, 2),
                    ("Safety Watch",          "Indirect", "SAF", 26.00m, 39.00m,   52.00m, 2),
                    ("Pipefitter Journeyman", "Direct",   "PF",  42.00m, 63.00m,   84.00m, 6),
                    ("Pipefitter Helper",     "Direct",   "PFH", 28.00m, 42.00m,   56.00m, 4),
                    ("Welder Journeyman",     "Direct",   "WD",  46.00m, 69.00m,   92.00m, 2),
                })
            },
            new()
            {
                CompanyCode = "CSL", StaffingPlanNumber = "SP-S-27-0008-VLO",
                Name = "Valero Port Arthur CDU Maintenance 2027", Client = "Valero Energy", ClientCode = "VLO",
                Branch = "Industrial", City = "Port Arthur", State = "TX",
                Status = "Draft", Shift = "Day", HoursPerShift = 10, Days = 14,
                StartDate = new DateTime(2027, 5, 3), EndDate = new DateTime(2027, 5, 16),
                OtMethod = "daily8_weekly40", DtWeekends = "none", RoughLaborTotal = 196000m, CreatedBy = "james.tanner",
                LaborRows = SpLaborRows(new DateTime(2027, 5, 3), 14, new[]
                {
                    ("General Foreman",       "Indirect", "SUP", 52.00m, 78.00m,  104.00m, 1),
                    ("Foreman",               "Indirect", "SUP", 45.00m, 67.50m,   90.00m, 1),
                    ("Pipefitter Journeyman", "Direct",   "PF",  42.00m, 63.00m,   84.00m, 5),
                    ("Pipefitter Helper",     "Direct",   "PFH", 28.00m, 42.00m,   56.00m, 3),
                    ("Welder Journeyman",     "Direct",   "WD",  46.00m, 69.00m,   92.00m, 2),
                })
            },
            new()
            {
                CompanyCode = "CSL", StaffingPlanNumber = "SP-S-27-0011-BASF",
                Name = "BASF Geismar Compressor Overhaul 2027", Client = "BASF Corporation", ClientCode = "BASF",
                Branch = "Industrial", City = "Geismar", State = "LA",
                Status = "Draft", Shift = "Both", HoursPerShift = 12, Days = 28,
                StartDate = new DateTime(2027, 6, 7), EndDate = new DateTime(2027, 7, 4),
                OtMethod = "daily8_weekly40", DtWeekends = "sat_sun", RoughLaborTotal = 524000m, CreatedBy = "tom.bishop",
                LaborRows = SpLaborRows(new DateTime(2027, 6, 7), 28, new[]
                {
                    ("General Foreman",        "Indirect", "SUP", 52.00m, 78.00m,  104.00m, 1),
                    ("Foreman",                "Indirect", "SUP", 45.00m, 67.50m,   90.00m, 2),
                    ("Safety Watch",           "Indirect", "SAF", 26.00m, 39.00m,   52.00m, 2),
                    ("Pipefitter Journeyman",  "Direct",   "PF",  42.00m, 63.00m,   84.00m, 5),
                    ("Welder Journeyman",      "Direct",   "WD",  46.00m, 69.00m,   92.00m, 3),
                    ("Boilermaker Journeyman", "Direct",   "BM",  44.00m, 66.00m,   88.00m, 2),
                })
            },

            // ── ETS 2027 Staffing Plans ──────────────────────────────────────
            new()
            {
                CompanyCode = "ETS", StaffingPlanNumber = "SP-H-27-001-CHN",
                Name = "Cheniere Corpus Christi T3 Turnaround 2027", Client = "Cheniere Energy", ClientCode = "CHN",
                Branch = "Industrial", City = "Corpus Christi", State = "TX",
                Status = "Approved", Shift = "Both", HoursPerShift = 12, Days = 31,
                StartDate = new DateTime(2027, 2, 1), EndDate = new DateTime(2027, 3, 3),
                OtMethod = "daily8_weekly40", DtWeekends = "sat_sun", RoughLaborTotal = 618000m, CreatedBy = "r.santos",
                LaborRows = SpLaborRows(new DateTime(2027, 2, 1), 31, new[]
                {
                    ("Project Manager",       "Indirect", "MGT", 65.00m, 97.50m,  130.00m, 1),
                    ("General Foreman",       "Indirect", "SUP", 52.00m, 78.00m,  104.00m, 1),
                    ("Foreman",               "Indirect", "SUP", 45.00m, 67.50m,   90.00m, 2),
                    ("Safety Watch",          "Indirect", "SAF", 26.00m, 39.00m,   52.00m, 3),
                    ("Pipefitter Journeyman", "Direct",   "PF",  43.00m, 64.50m,   86.00m, 7),
                    ("Pipefitter Helper",     "Direct",   "PFH", 29.00m, 43.50m,   58.00m, 5),
                    ("Welder Journeyman",     "Direct",   "WD",  47.00m, 70.50m,   94.00m, 3),
                    ("Boilermaker Journeyman","Direct",   "BM",  45.00m, 67.50m,   90.00m, 2),
                    ("Rigger",               "Direct",   "RIG", 40.00m, 60.00m,   80.00m, 1),
                })
            },
            new()
            {
                CompanyCode = "ETS", StaffingPlanNumber = "SP-H-27-003-LYB",
                Name = "LyondellBasell La Porte HVU Inspection 2027", Client = "LyondellBasell", ClientCode = "LYB",
                Branch = "Industrial", City = "La Porte", State = "TX",
                Status = "Approved", Shift = "Day", HoursPerShift = 10, Days = 14,
                StartDate = new DateTime(2027, 3, 15), EndDate = new DateTime(2027, 3, 28),
                OtMethod = "daily8_weekly40", DtWeekends = "none", RoughLaborTotal = 228000m, CreatedBy = "m.delgado",
                LaborRows = SpLaborRows(new DateTime(2027, 3, 15), 14, new[]
                {
                    ("General Foreman",       "Indirect", "SUP", 52.00m, 78.00m,  104.00m, 1),
                    ("Foreman",               "Indirect", "SUP", 45.00m, 67.50m,   90.00m, 1),
                    ("Pipefitter Journeyman", "Direct",   "PF",  43.00m, 64.50m,   86.00m, 5),
                    ("Pipefitter Helper",     "Direct",   "PFH", 29.00m, 43.50m,   58.00m, 3),
                    ("Welder Journeyman",     "Direct",   "WD",  47.00m, 70.50m,   94.00m, 2),
                })
            },
            new()
            {
                CompanyCode = "ETS", StaffingPlanNumber = "SP-H-27-006-CTG",
                Name = "CITGO Corpus Christi Alkylation Unit TA 2027", Client = "CITGO Petroleum", ClientCode = "CTG",
                Branch = "Industrial", City = "Corpus Christi", State = "TX",
                Status = "Draft", Shift = "Both", HoursPerShift = 12, Days = 28,
                StartDate = new DateTime(2027, 4, 12), EndDate = new DateTime(2027, 5, 9),
                OtMethod = "daily8_weekly40", DtWeekends = "sat_sun", RoughLaborTotal = 496000m, CreatedBy = "m.delgado",
                LaborRows = SpLaborRows(new DateTime(2027, 4, 12), 28, new[]
                {
                    ("General Foreman",       "Indirect", "SUP", 52.00m, 78.00m,  104.00m, 1),
                    ("Foreman",               "Indirect", "SUP", 45.00m, 67.50m,   90.00m, 2),
                    ("Safety Watch",          "Indirect", "SAF", 26.00m, 39.00m,   52.00m, 2),
                    ("Pipefitter Journeyman", "Direct",   "PF",  43.00m, 64.50m,   86.00m, 6),
                    ("Pipefitter Helper",     "Direct",   "PFH", 29.00m, 43.50m,   58.00m, 4),
                    ("Welder Journeyman",     "Direct",   "WD",  47.00m, 70.50m,   94.00m, 2),
                    ("Boilermaker Journeyman","Direct",   "BM",  45.00m, 67.50m,   90.00m, 1),
                })
            },
            new()
            {
                CompanyCode = "ETS", StaffingPlanNumber = "SP-B-27-002-FHR",
                Name = "Flint Hills Resources Corpus Christi Recontacter TA", Client = "Flint Hills Resources", ClientCode = "FHR",
                Branch = "Industrial", City = "Corpus Christi", State = "TX",
                Status = "Draft", Shift = "Day", HoursPerShift = 10, Days = 21,
                StartDate = new DateTime(2027, 5, 10), EndDate = new DateTime(2027, 5, 30),
                OtMethod = "daily8_weekly40", DtWeekends = "none", RoughLaborTotal = 312000m, CreatedBy = "r.santos",
                LaborRows = SpLaborRows(new DateTime(2027, 5, 10), 21, new[]
                {
                    ("General Foreman",       "Indirect", "SUP", 52.00m, 78.00m,  104.00m, 1),
                    ("Foreman",               "Indirect", "SUP", 45.00m, 67.50m,   90.00m, 1),
                    ("Pipefitter Journeyman", "Direct",   "PF",  43.00m, 64.50m,   86.00m, 5),
                    ("Pipefitter Helper",     "Direct",   "PFH", 29.00m, 43.50m,   58.00m, 3),
                    ("Welder Journeyman",     "Direct",   "WD",  47.00m, 70.50m,   94.00m, 2),
                })
            },
            new()
            {
                CompanyCode = "ETS", StaffingPlanNumber = "SP-H-27-009-SHELL",
                Name = "Shell Deer Park EO Plant Turnaround 2027", Client = "Shell Oil Company", ClientCode = "SHELL",
                Branch = "Industrial", City = "Deer Park", State = "TX",
                Status = "Draft", Shift = "Both", HoursPerShift = 12, Days = 35,
                StartDate = new DateTime(2027, 6, 14), EndDate = new DateTime(2027, 7, 18),
                OtMethod = "daily8_weekly40", DtWeekends = "sat_sun", RoughLaborTotal = 724000m, CreatedBy = "r.santos",
                LaborRows = SpLaborRows(new DateTime(2027, 6, 14), 35, new[]
                {
                    ("Project Manager",       "Indirect", "MGT", 65.00m, 97.50m,  130.00m, 1),
                    ("General Foreman",       "Indirect", "SUP", 52.00m, 78.00m,  104.00m, 1),
                    ("Foreman",               "Indirect", "SUP", 45.00m, 67.50m,   90.00m, 2),
                    ("Safety Watch",          "Indirect", "SAF", 26.00m, 39.00m,   52.00m, 3),
                    ("Pipefitter Journeyman", "Direct",   "PF",  43.00m, 64.50m,   86.00m, 8),
                    ("Pipefitter Helper",     "Direct",   "PFH", 29.00m, 43.50m,   58.00m, 5),
                    ("Welder Journeyman",     "Direct",   "WD",  47.00m, 70.50m,   94.00m, 3),
                    ("Boilermaker Journeyman","Direct",   "BM",  45.00m, 67.50m,   90.00m, 2),
                    ("Crane Operator",        "Direct",   "OPR", 48.00m, 72.00m,   96.00m, 1),
                })
            }
        };

        db.StaffingPlans.AddRange(plans);
        await db.SaveChangesAsync();
    }

    // ── Estimates ─────────────────────────────────────────────────────────────

    private static async Task SeedEstimates(AppDbContext db)
    {
        if (await db.Estimates.AnyAsync()) return;

        var estimates = new List<Estimate>
        {
            // ── CSL AWARDED ──────────────────────────────────────────────────
            E("CSL","26-0001-BP","BP Baytown Crude Unit Pre-TA Isolation",
                "British Petroleum","BP","MSA-BP-2024-01","Turnaround","Baytown","TX","BP Baytown Refinery",
                14,new DateTime(2026,1,5),new DateTime(2026,1,18),"Awarded",100,"Day",10,
                "David Torres","Rachel Kim","Gulf","james.tanner",-115,-108,
                new[]{
                    ("Project Manager",        "Indirect","MGT",125.00m,187.50m,250.00m,1,1),
                    ("General Foreman",        "Indirect","SUP", 98.00m,147.00m,196.00m,1,2),
                    ("Foreman",                "Indirect","SUP", 85.00m,127.50m,170.00m,1,3),
                    ("Safety Watch",           "Indirect","SAF", 48.00m, 72.00m, 96.00m,1,4),
                    ("Pipefitter Journeyman",  "Direct",  "PF",  78.00m,117.00m,156.00m,3,5),
                    ("Pipefitter Helper",      "Direct",  "PFH", 52.00m, 78.00m,104.00m,2,6),
                    ("Boilermaker Journeyman", "Direct",  "BM",  82.00m,123.00m,164.00m,1,7),
                },No(),Pd("Per Diem – 8 persons out of town",125m,14,8),0.69m),

            E("CSL","26-0004-SHELL","Shell Deer Park FCC Piping Replacement",
                "Shell Oil Company","SHELL","MSA-SHELL-2024-01","Turnaround","Deer Park","TX","Shell Deer Park Refinery",
                21,new DateTime(2026,1,12),new DateTime(2026,2,1),"Awarded",100,"Both",20,
                "Mark Ellis","Rachel Kim","Gulf","michael.santos",-108,-102,
                new[]{
                    ("Project Manager",        "Indirect","MGT",125.00m,187.50m,250.00m,1,1),
                    ("General Foreman",        "Indirect","SUP", 98.00m,147.00m,196.00m,1,2),
                    ("Foreman",                "Indirect","SUP", 85.00m,127.50m,170.00m,2,3),
                    ("Safety Watch",           "Indirect","SAF", 48.00m, 72.00m, 96.00m,2,4),
                    ("Fire Watch",             "Indirect","SAF", 45.00m, 67.50m, 90.00m,1,5),
                    ("Pipefitter Journeyman",  "Direct",  "PF",  78.00m,117.00m,156.00m,5,6),
                    ("Pipefitter Helper",      "Direct",  "PFH", 52.00m, 78.00m,104.00m,4,7),
                    ("Welder Journeyman",      "Direct",  "WD",  85.00m,127.50m,170.00m,2,8),
                    ("Boilermaker Journeyman", "Direct",  "BM",  82.00m,123.00m,164.00m,2,9),
                },No(),Pd("Per Diem – 17 persons out of town",125m,21,17),0.68m),

            E("CSL","26-0005-XOM","ExxonMobil Texas City CDU Inspection",
                "ExxonMobil","XOM",null,"Inspection","Texas City","TX","ExxonMobil Texas City",
                7,new DateTime(2026,2,2),new DateTime(2026,2,8),"Awarded",100,"Day",5,
                "David Torres","Rachel Kim","Gulf","carol.whitfield",-95,-92,
                new[]{
                    ("Foreman",          "Indirect","SUP",85.00m,127.50m,170.00m,1,1),
                    ("Instrument Tech",  "Direct",  "IE", 88.00m,132.00m,176.00m,2,2),
                    ("NDT Technician",   "Direct",  "NDT",95.00m,142.50m,190.00m,2,3),
                },No(),No2(),0.70m),

            E("CSL","26-0007-XOM","Rusty 16-43",
                "ExxonMobil","XOM",null,"Turnaround","Baytown","TX","ExxonMobil Baytown Refinery",
                21,new DateTime(2026,1,13),new DateTime(2026,2,2),"Awarded",100,"Day",9,
                "David Torres","Rachel Kim","Gulf","tom.bishop",-108,-100,
                new[]{
                    ("General Foreman",       "Indirect","SUP",98.00m,147.00m,196.00m,1,1),
                    ("Foreman",               "Indirect","SUP",85.00m,127.50m,170.00m,1,2),
                    ("Pipefitter Journeyman", "Direct",  "PF", 78.00m,117.00m,156.00m,4,3),
                    ("Pipefitter Helper",     "Direct",  "PFH",52.00m, 78.00m,104.00m,3,4),
                },No(),No2(),0.68m),

            E("CSL","26-0009-SHELL","Shell Norco Pipe Replacement",
                "Shell Oil Company","SHELL",null,"Maintenance","Norco","LA","Shell Norco Refinery",
                21,new DateTime(2026,2,3),new DateTime(2026,2,23),"Awarded",100,"Day",13,
                "Mark Ellis","Rachel Kim","Gulf","michael.santos",-88,-82,
                new[]{
                    ("General Foreman",       "Indirect","SUP",98.00m,147.00m,196.00m,1,1),
                    ("Foreman",               "Indirect","SUP",85.00m,127.50m,170.00m,1,2),
                    ("Pipefitter Journeyman", "Direct",  "PF", 78.00m,117.00m,156.00m,4,3),
                    ("Pipefitter Helper",     "Direct",  "PFH",52.00m, 78.00m,104.00m,4,4),
                    ("Welder Journeyman",     "Direct",  "WD", 85.00m,127.50m,170.00m,2,5),
                    ("Boilermaker Journeyman","Direct",  "BM", 82.00m,123.00m,164.00m,1,6),
                },No(),Pd("Per Diem – 13 persons out of town",125m,21,13),0.69m),

            // ACTIVE on April 24 — 26 days, Both shifts, 25 workers
            E("CSL","26-0010-BP","BP Baytown Unit 5 Turnaround",
                "British Petroleum","BP","MSA-BP-2024-01","Turnaround","Baytown","TX","BP Baytown Refinery",
                26,new DateTime(2026,4,7),new DateTime(2026,5,2),"Awarded",100,"Both",25,
                "Mark Ellis","Jennifer Bates","Gulf","james.tanner",-55,-40,
                new[]{
                    ("Project Manager",        "Indirect","MGT",128.00m,192.00m,256.00m,1,1),
                    ("General Foreman",        "Indirect","SUP",100.00m,150.00m,200.00m,1,2),
                    ("Foreman",                "Indirect","SUP", 87.00m,130.50m,174.00m,2,3),
                    ("Safety Watch",           "Indirect","SAF", 50.00m, 75.00m,100.00m,3,4),
                    ("Fire Watch",             "Indirect","SAF", 47.00m, 70.50m, 94.00m,1,5),
                    ("Pipefitter Journeyman",  "Direct",  "PF",  80.00m,120.00m,160.00m,6,6),
                    ("Pipefitter Helper",      "Direct",  "PFH", 54.00m, 81.00m,108.00m,4,7),
                    ("Welder Journeyman",      "Direct",  "WD",  87.00m,130.50m,174.00m,3,8),
                    ("Boilermaker Journeyman", "Direct",  "BM",  84.00m,126.00m,168.00m,2,9),
                    ("Crane Operator",         "Direct",  "OPR", 97.00m,145.50m,194.00m,1,10),
                    ("Rigger",                 "Direct",  "RIG", 74.00m,111.00m,148.00m,1,11),
                },No(),Pd("Per Diem – 22 persons out of town",125m,26,22),0.68m),

            // ACTIVE on April 24 — 35 days, Both shifts, 27 workers
            E("CSL","26-0012-XOM","ExxonMobil Baton Rouge Delayed Coker TA",
                "ExxonMobil","XOM","MSA-XOM-2024-02","Turnaround","Baton Rouge","LA","ExxonMobil Baton Rouge Refinery",
                35,new DateTime(2026,4,14),new DateTime(2026,5,18),"Awarded",100,"Both",27,
                "David Torres","Jennifer Bates","Gulf","carol.whitfield",-48,-35,
                new[]{
                    ("Project Manager",        "Indirect","MGT",125.00m,187.50m,250.00m,1,1),
                    ("General Foreman",        "Indirect","SUP", 98.00m,147.00m,196.00m,1,2),
                    ("Foreman",                "Indirect","SUP", 85.00m,127.50m,170.00m,2,3),
                    ("Safety Watch",           "Indirect","SAF", 48.00m, 72.00m, 96.00m,4,4),
                    ("Fire Watch",             "Indirect","SAF", 45.00m, 67.50m, 90.00m,1,5),
                    ("Pipefitter Journeyman",  "Direct",  "PF",  78.00m,117.00m,156.00m,7,6),
                    ("Pipefitter Helper",      "Direct",  "PFH", 52.00m, 78.00m,104.00m,4,7),
                    ("Welder Journeyman",      "Direct",  "WD",  85.00m,127.50m,170.00m,3,8),
                    ("Boilermaker Journeyman", "Direct",  "BM",  82.00m,123.00m,164.00m,2,9),
                    ("Crane Operator",         "Direct",  "OPR", 95.00m,142.50m,190.00m,1,10),
                    ("Rigger",                 "Direct",  "RIG", 72.00m,108.00m,144.00m,1,11),
                },No(),new[]{("PerDiem","Per Diem – 24 persons out of town",125m,"Day",35,24,true),
                             ("Travel","Mobilization / Demobilization",8500m,"Each",1,1,true)},0.68m),

            // ── CSL LOST ─────────────────────────────────────────────────────
            E("CSL","26-0006-CVX","Chevron El Segundo Coker TA",
                "Chevron","CVX",null,"Turnaround","El Segundo","CA","Chevron El Segundo Refinery",
                29,new DateTime(2026,2,9),new DateTime(2026,3,9),"Lost",0,"Both",25,
                "Mark Ellis","Rachel Kim","West","michael.santos",-88,-75,
                new[]{
                    ("Project Manager",        "Indirect","MGT",125.00m,187.50m,250.00m,1,1),
                    ("General Foreman",        "Indirect","SUP", 98.00m,147.00m,196.00m,1,2),
                    ("Foreman",                "Indirect","SUP", 85.00m,127.50m,170.00m,2,3),
                    ("Safety Watch",           "Indirect","SAF", 48.00m, 72.00m, 96.00m,3,4),
                    ("Pipefitter Journeyman",  "Direct",  "PF",  78.00m,117.00m,156.00m,6,5),
                    ("Pipefitter Helper",      "Direct",  "PFH", 52.00m, 78.00m,104.00m,4,6),
                    ("Welder Journeyman",      "Direct",  "WD",  85.00m,127.50m,170.00m,3,7),
                    ("Boilermaker Journeyman", "Direct",  "BM",  82.00m,123.00m,164.00m,3,8),
                    ("Crane Operator",         "Direct",  "OPR", 95.00m,142.50m,190.00m,1,9),
                    ("Rigger",                 "Direct",  "RIG", 72.00m,108.00m,144.00m,1,10),
                },No(),new[]{("PerDiem","Per Diem – High Cost Area (22 persons)",150m,"Day",29,22,true),
                             ("Travel","Airfare – 22 round trips",450m,"Trip",1,22,true)},
                0.69m,"Went with incumbent contractor"),

            E("CSL","26-0014-SHELL","Shell Deer Park Heat Exchanger Revamp",
                "Shell Oil Company","SHELL",null,"Maintenance","Deer Park","TX","Shell Deer Park Refinery",
                21,new DateTime(2026,2,23),new DateTime(2026,3,15),"Lost",0,"Day",12,
                "Mark Ellis","Rachel Kim","Gulf","tom.bishop",-70,-62,
                new[]{
                    ("General Foreman",       "Indirect","SUP",98.00m,147.00m,196.00m,1,1),
                    ("Foreman",               "Indirect","SUP",85.00m,127.50m,170.00m,1,2),
                    ("Pipefitter Journeyman", "Direct",  "PF", 78.00m,117.00m,156.00m,4,3),
                    ("Pipefitter Helper",     "Direct",  "PFH",52.00m, 78.00m,104.00m,3,4),
                    ("Welder Journeyman",     "Direct",  "WD", 85.00m,127.50m,170.00m,2,5),
                    ("Boilermaker Journeyman","Direct",  "BM", 82.00m,123.00m,164.00m,1,6),
                },No(),Pd("Per Diem – 9 persons",125m,21,9),0.71m,"Went with competitor"),

            // ── CSL SUBMITTED ────────────────────────────────────────────────
            E("CSL","26-0003-SHELL","Catdog 24-10h",
                "Shell Oil Company","SHELL",null,"Turnaround","Deer Park","TX","Shell Deer Park Refinery",
                21,new DateTime(2026,4,28),new DateTime(2026,5,18),"Submitted",75,"Day",12,
                "Mark Ellis","Jennifer Bates","Gulf","james.tanner",-5,-2,
                new[]{
                    ("General Foreman",       "Indirect","SUP",98.00m,147.00m,196.00m,1,1),
                    ("Foreman",               "Indirect","SUP",85.00m,127.50m,170.00m,1,2),
                    ("Pipefitter Journeyman", "Direct",  "PF", 78.00m,117.00m,156.00m,4,3),
                    ("Pipefitter Helper",     "Direct",  "PFH",52.00m, 78.00m,104.00m,3,4),
                    ("Welder Journeyman",     "Direct",  "WD", 85.00m,127.50m,170.00m,2,5),
                    ("Boilermaker Journeyman","Direct",  "BM", 82.00m,123.00m,164.00m,1,6),
                },No(),Pd("Per Diem – 12 persons",125m,21,12),0.70m),

            E("CSL","26-0015-BP","BP Texas City Emergency Tie-in",
                "British Petroleum","BP",null,"Maintenance","Texas City","TX","BP Texas City Refinery",
                7,new DateTime(2026,1,26),new DateTime(2026,2,1),"Submitted",70,"Day",4,
                "David Torres","Rachel Kim","Gulf","sarah.mendez",-80,-76,
                new[]{
                    ("Foreman",               "Indirect","SUP",85.00m,127.50m,170.00m,1,1),
                    ("Pipefitter Journeyman", "Direct",  "PF", 78.00m,117.00m,156.00m,2,2),
                    ("Welder Journeyman",     "Direct",  "WD", 85.00m,127.50m,170.00m,1,3),
                },No(),No2(),0.70m),

            E("CSL","26-0020-CVX","Chevron Pascagoula Unit Tie-in",
                "Chevron","CVX",null,"Maintenance","Pascagoula","MS","Chevron Pascagoula Refinery",
                10,new DateTime(2026,3,9),new DateTime(2026,3,18),"Submitted",75,"Day",5,
                "Mark Ellis","Jennifer Bates","Gulf","tom.bishop",-40,-36,
                new[]{
                    ("Foreman",               "Indirect","SUP",85.00m,127.50m,170.00m,1,1),
                    ("Pipefitter Journeyman", "Direct",  "PF", 78.00m,117.00m,156.00m,2,2),
                    ("Pipefitter Helper",     "Direct",  "PFH",52.00m, 78.00m,104.00m,2,3),
                },No(),Pd("Per Diem – 5 persons out of town",125m,10,5),0.70m),

            E("CSL","26-0021-MPC","Marathon Garyville Flange Isolation",
                "Marathon Petroleum Corp.","MPC",null,"Maintenance","Garyville","LA",null,
                5,new DateTime(2026,3,2),new DateTime(2026,3,6),"Submitted",50,"Day",3,
                "David Torres","Jennifer Bates","Gulf","james.tanner",-52,-48,
                new[]{
                    ("Foreman",               "Indirect","SUP",85.00m,127.50m,170.00m,1,1),
                    ("Pipefitter Journeyman", "Direct",  "PF", 78.00m,117.00m,156.00m,2,2),
                },No(),No2(),0.72m),

            E("CSL","26-0023-MPC","Molasses 44-14A",
                "Marathon Petroleum Corp.","MPC",null,"Turnaround","Texas City","TX",null,
                7,new DateTime(2026,3,16),new DateTime(2026,3,22),"Submitted",65,"Day",4,
                "David Torres","Jennifer Bates","Gulf","carol.whitfield",-38,-34,
                new[]{
                    ("Foreman",               "Indirect","SUP",85.00m,127.50m,170.00m,1,1),
                    ("Pipefitter Journeyman", "Direct",  "PF", 78.00m,117.00m,156.00m,2,2),
                    ("Welder Journeyman",     "Direct",  "WD", 85.00m,127.50m,170.00m,1,3),
                },No(),No2(),0.70m),

            // PRIMARY DEMO ESTIMATE — Full detail
            E("CSL","26-0024-SHELL","Shell Deer Park CDU Turnaround",
                "Shell Oil Company","SHELL","MSA-SHELL-2024-01","Turnaround","Deer Park","TX","Shell Deer Park Refinery",
                28,new DateTime(2026,6,2),new DateTime(2026,6,29),"Submitted",80,"Both",27,
                "Mark Ellis","Jennifer Bates","Gulf","michael.santos",-22,-18,
                new[]{
                    ("Project Manager",        "Indirect","MGT",125.00m,187.50m,250.00m,1,1),
                    ("General Foreman",        "Indirect","SUP", 98.00m,147.00m,196.00m,1,2),
                    ("Foreman",                "Indirect","SUP", 85.00m,127.50m,170.00m,2,3),
                    ("Safety Watch",           "Indirect","SAF", 48.00m, 72.00m, 96.00m,2,4),
                    ("Fire Watch",             "Indirect","SAF", 45.00m, 67.50m, 90.00m,1,5),
                    ("Pipefitter Journeyman",  "Direct",  "PF",  78.00m,117.00m,156.00m,7,6),
                    ("Pipefitter Helper",      "Direct",  "PFH", 52.00m, 78.00m,104.00m,4,7),
                    ("Welder Journeyman",      "Direct",  "WD",  85.00m,127.50m,170.00m,3,8),
                    ("Welder Helper",          "Direct",  "WDH", 55.00m, 82.50m,110.00m,2,9),
                    ("Boilermaker Journeyman", "Direct",  "BM",  82.00m,123.00m,164.00m,2,10),
                    ("Crane Operator",         "Direct",  "OPR", 95.00m,142.50m,190.00m,1,11),
                    ("Rigger",                 "Direct",  "RIG", 72.00m,108.00m,144.00m,1,12),
                },
                new[]{ ("Manlift 40ft","Weekly",900m,2,28) },
                new[]{("PerDiem","Per Diem – 24 persons out of town",125m,"Day",28,24,true),
                      ("Travel","Mobilization / Demobilization",7500m,"Each",1,1,true)},0.69m),

            // ── CSL PENDING ──────────────────────────────────────────────────
            E("CSL","26-0011-VLO","Valero Houston Maintenance",
                "Valero Energy","VLO",null,"Maintenance","Houston","TX","Valero Houston Refinery",
                14,new DateTime(2026,4,14),new DateTime(2026,4,27),"Pending",60,"Day",7,
                "David Torres","Jennifer Bates","Gulf","james.tanner",-35,-30,
                new[]{
                    ("Foreman",               "Indirect","SUP",85.00m,127.50m,170.00m,1,1),
                    ("Pipefitter Journeyman", "Direct",  "PF", 78.00m,117.00m,156.00m,3,2),
                    ("Pipefitter Helper",     "Direct",  "PFH",52.00m, 78.00m,104.00m,3,3),
                },No(),No2(),0.71m),

            E("CSL","26-0013-CVX","Chevron El Segundo Small Repair",
                "Chevron","CVX",null,"Maintenance","El Segundo","CA","Chevron El Segundo Refinery",
                7,new DateTime(2026,5,5),new DateTime(2026,5,11),"Pending",55,"Day",4,
                "Mark Ellis","Jennifer Bates","West","sarah.mendez",-28,-24,
                new[]{
                    ("Foreman",               "Indirect","SUP",85.00m,127.50m,170.00m,1,1),
                    ("Pipefitter Journeyman", "Direct",  "PF", 78.00m,117.00m,156.00m,2,2),
                    ("Pipefitter Helper",     "Direct",  "PFH",52.00m, 78.00m,104.00m,1,3),
                },No(),new[]{("PerDiem","Per Diem – High Cost Area (4 persons)",150m,"Day",7,4,true),
                             ("Travel","Airfare – 4 round trips",450m,"Trip",1,4,true)},0.72m),

            E("CSL","26-0016-VLO","Valero Port Arthur Inspection Support",
                "Valero Energy","VLO",null,"Inspection","Port Arthur","TX","Valero Port Arthur Refinery",
                14,new DateTime(2026,4,28),new DateTime(2026,5,11),"Pending",65,"Day",6,
                "David Torres","Jennifer Bates","Gulf","tom.bishop",-20,-17,
                new[]{
                    ("General Foreman", "Indirect","SUP",98.00m,147.00m,196.00m,1,1),
                    ("Instrument Tech", "Direct",  "IE", 88.00m,132.00m,176.00m,3,2),
                    ("NDT Technician",  "Direct",  "NDT",95.00m,142.50m,190.00m,2,3),
                },No(),No2(),0.70m),

            E("CSL","26-0018-CVX","Chevron Pascagoula Piping Modification",
                "Chevron","CVX",null,"Maintenance","Pascagoula","MS","Chevron Pascagoula Refinery",
                21,new DateTime(2026,5,11),new DateTime(2026,5,31),"Pending",70,"Day",9,
                "Mark Ellis","Jennifer Bates","Gulf","michael.santos",-18,-14,
                new[]{
                    ("Project Manager",       "Indirect","MGT",125.00m,187.50m,250.00m,1,1),
                    ("General Foreman",       "Indirect","SUP", 98.00m,147.00m,196.00m,1,2),
                    ("Foreman",               "Indirect","SUP", 85.00m,127.50m,170.00m,1,3),
                    ("Pipefitter Journeyman", "Direct",  "PF",  78.00m,117.00m,156.00m,4,4),
                    ("Pipefitter Helper",     "Direct",  "PFH", 52.00m, 78.00m,104.00m,2,5),
                },No(),Pd("Per Diem – 8 persons out of town",125m,21,8),0.70m),

            E("CSL","26-0022-VLO","Valero Port Arthur Maintenance",
                "Valero Energy","VLO",null,"Maintenance","Port Arthur","TX","Valero Port Arthur Refinery",
                14,new DateTime(2026,5,18),new DateTime(2026,5,31),"Pending",60,"Day",5,
                "David Torres","Jennifer Bates","Gulf","sarah.mendez",-12,-10,
                new[]{
                    ("Foreman",               "Indirect","SUP",85.00m,127.50m,170.00m,1,1),
                    ("Pipefitter Journeyman", "Direct",  "PF", 78.00m,117.00m,156.00m,2,2),
                    ("Pipefitter Helper",     "Direct",  "PFH",52.00m, 78.00m,104.00m,2,3),
                },No(),No2(),0.71m),

            E("CSL","26-0025-SHELL","Kangaroo 11-25h",
                "Shell Oil Company","SHELL",null,"Turnaround","Deer Park","TX","Shell Deer Park Refinery",
                14,new DateTime(2026,5,4),new DateTime(2026,5,17),"Pending",60,"Day",7,
                "Mark Ellis","Jennifer Bates","Gulf","james.tanner",-8,-5,
                new[]{
                    ("Foreman",               "Indirect","SUP",85.00m,127.50m,170.00m,1,1),
                    ("Pipefitter Journeyman", "Direct",  "PF", 78.00m,117.00m,156.00m,3,2),
                    ("Pipefitter Helper",     "Direct",  "PFH",52.00m, 78.00m,104.00m,2,3),
                    ("Welder Journeyman",     "Direct",  "WD", 85.00m,127.50m,170.00m,1,4),
                },No(),No2(),0.70m),

            // ── ETS AWARDED ──────────────────────────────────────────────────
            E("ETS","H26-001-VLO","Valero Corpus Christi Coker Isolation",
                "Valero Energy","VLO",null,"Turnaround","Corpus Christi","TX","Valero Corpus Christi Refinery",
                21,new DateTime(2026,2,3),new DateTime(2026,2,23),"Awarded",100,"Day",14,
                "Mike Rodriguez","Lisa Herrera","South TX","r.santos",-80,-74,
                new[]{
                    ("General Foreman",       "Indirect","SUP",95.00m,142.50m,190.00m,1,1),
                    ("Foreman",               "Indirect","SUP",82.00m,123.00m,164.00m,1,2),
                    ("Safety Watch",          "Indirect","SAF",46.00m, 69.00m, 92.00m,2,3),
                    ("Pipefitter Journeyman", "Direct",  "PF", 76.00m,114.00m,152.00m,4,4),
                    ("Pipefitter Helper",     "Direct",  "PFH",50.00m, 75.00m,100.00m,3,5),
                    ("Welder Journeyman",     "Direct",  "WD", 83.00m,124.50m,166.00m,2,6),
                    ("Boilermaker Journeyman","Direct",  "BM", 80.00m,120.00m,160.00m,1,7),
                },No(),Pd("Per Diem – 14 persons",125m,21,14),0.70m),

            E("ETS","H26-004-FHR","Flint Hills Resources CDU Tie-in",
                "Flint Hills Resources","FHR",null,"Turnaround","Corpus Christi","TX","Flint Hills Corpus Christi",
                21,new DateTime(2026,3,9),new DateTime(2026,3,29),"Awarded",100,"Day",12,
                "Carlos Vega","Lisa Herrera","South TX","m.delgado",-46,-40,
                new[]{
                    ("General Foreman",       "Indirect","SUP",95.00m,142.50m,190.00m,1,1),
                    ("Foreman",               "Indirect","SUP",82.00m,123.00m,164.00m,1,2),
                    ("Safety Watch",          "Indirect","SAF",46.00m, 69.00m, 92.00m,2,3),
                    ("Pipefitter Journeyman", "Direct",  "PF", 76.00m,114.00m,152.00m,4,4),
                    ("Pipefitter Helper",     "Direct",  "PFH",50.00m, 75.00m,100.00m,2,5),
                    ("Welder Journeyman",     "Direct",  "WD", 83.00m,124.50m,166.00m,2,6),
                    ("Boilermaker Journeyman","Direct",  "BM", 80.00m,120.00m,160.00m,1,7),
                },No(),Pd("Per Diem – 12 persons",125m,21,12),0.70m),

            // ACTIVE on April 24 — 31 days, Both shifts, 22 workers
            E("ETS","H26-007-CHN","Cheniere Corpus Christi LNG Pre-TA",
                "Cheniere Energy","CHN",null,"Turnaround","Corpus Christi","TX","Cheniere Corpus Christi LNG Terminal",
                31,new DateTime(2026,4,10),new DateTime(2026,5,10),"Awarded",100,"Both",22,
                "Mike Rodriguez","Lisa Herrera","South TX","r.santos",-30,-22,
                new[]{
                    ("Project Manager",        "Indirect","MGT",122.00m,183.00m,244.00m,1,1),
                    ("General Foreman",        "Indirect","SUP", 95.00m,142.50m,190.00m,1,2),
                    ("Foreman",                "Indirect","SUP", 82.00m,123.00m,164.00m,2,3),
                    ("Safety Watch",           "Indirect","SAF", 46.00m, 69.00m, 92.00m,2,4),
                    ("Pipefitter Journeyman",  "Direct",  "PF",  76.00m,114.00m,152.00m,5,5),
                    ("Pipefitter Helper",      "Direct",  "PFH", 50.00m, 75.00m,100.00m,3,6),
                    ("Welder Journeyman",      "Direct",  "WD",  83.00m,124.50m,166.00m,3,7),
                    ("Boilermaker Journeyman", "Direct",  "BM",  80.00m,120.00m,160.00m,2,8),
                    ("Rigger",                 "Direct",  "RIG", 70.00m,105.00m,140.00m,1,9),
                    ("Crane Operator",         "Direct",  "OPR", 92.00m,138.00m,184.00m,1,10),
                    ("NDT Technician",         "Direct",  "NDT", 92.00m,138.00m,184.00m,1,11),
                },No(),new[]{("PerDiem","Per Diem – 19 persons",125m,"Day",31,19,true),
                             ("Travel","Mobilization",4500m,"Each",1,1,true)},0.69m),

            // ── ETS LOST ─────────────────────────────────────────────────────
            E("ETS","H26-003-LYB","LyondellBasell Port Arthur Exchanger",
                "LyondellBasell Industries","LYB",null,"Maintenance","Port Arthur","TX","LyondellBasell Port Arthur",
                21,new DateTime(2026,2,17),new DateTime(2026,3,9),"Lost",0,"Day",13,
                "Carlos Vega","Lisa Herrera","Gulf","m.delgado",-60,-55,
                new[]{
                    ("General Foreman",       "Indirect","SUP",95.00m,142.50m,190.00m,1,1),
                    ("Foreman",               "Indirect","SUP",82.00m,123.00m,164.00m,1,2),
                    ("Pipefitter Journeyman", "Direct",  "PF", 76.00m,114.00m,152.00m,4,3),
                    ("Pipefitter Helper",     "Direct",  "PFH",50.00m, 75.00m,100.00m,3,4),
                    ("Boilermaker Journeyman","Direct",  "BM", 80.00m,120.00m,160.00m,3,5),
                    ("Boilermaker Helper",    "Direct",  "BMH",52.00m, 78.00m,104.00m,1,6),
                },No(),Pd("Per Diem – 13 persons",125m,21,13),0.71m,"Rates too high"),

            // ── ETS SUBMITTED ────────────────────────────────────────────────
            E("ETS","H26-002-CTG","CITGO Corpus Christi FCC Turnaround",
                "CITGO Petroleum","CTG",null,"Turnaround","Corpus Christi","TX","CITGO Corpus Christi Refinery",
                28,new DateTime(2026,5,5),new DateTime(2026,6,1),"Submitted",70,"Both",18,
                "Mike Rodriguez","Lisa Herrera","South TX","r.santos",-15,-12,
                new[]{
                    ("Project Manager",        "Indirect","MGT",122.00m,183.00m,244.00m,1,1),
                    ("General Foreman",        "Indirect","SUP", 95.00m,142.50m,190.00m,1,2),
                    ("Foreman",                "Indirect","SUP", 82.00m,123.00m,164.00m,2,3),
                    ("Safety Watch",           "Indirect","SAF", 46.00m, 69.00m, 92.00m,2,4),
                    ("Pipefitter Journeyman",  "Direct",  "PF",  76.00m,114.00m,152.00m,5,5),
                    ("Pipefitter Helper",      "Direct",  "PFH", 50.00m, 75.00m,100.00m,3,6),
                    ("Welder Journeyman",      "Direct",  "WD",  83.00m,124.50m,166.00m,2,7),
                    ("Boilermaker Journeyman", "Direct",  "BM",  80.00m,120.00m,160.00m,2,8),
                },No(),Pd("Per Diem – 15 persons",125m,28,15),0.70m),

            E("ETS","H26-006-VLO","Valero Three Rivers Maintenance",
                "Valero Energy","VLO",null,"Maintenance","Three Rivers","TX",null,
                14,new DateTime(2026,5,12),new DateTime(2026,5,25),"Submitted",65,"Day",8,
                "Carlos Vega","Lisa Herrera","South TX","m.delgado",-12,-9,
                new[]{
                    ("Foreman",               "Indirect","SUP",82.00m,123.00m,164.00m,1,1),
                    ("Pipefitter Journeyman", "Direct",  "PF", 76.00m,114.00m,152.00m,3,2),
                    ("Pipefitter Helper",     "Direct",  "PFH",50.00m, 75.00m,100.00m,2,3),
                    ("Welder Journeyman",     "Direct",  "WD", 83.00m,124.50m,166.00m,2,4),
                },No(),No2(),0.71m),

            E("ETS","H26-009-CAL","Calumet San Antonio Piping",
                "Calumet Specialty Products","CAL",null,"Maintenance","San Antonio","TX",null,
                14,new DateTime(2026,6,2),new DateTime(2026,6,15),"Submitted",60,"Day",7,
                "Mike Rodriguez","Lisa Herrera","South TX","r.santos",-8,-6,
                new[]{
                    ("Foreman",               "Indirect","SUP",82.00m,123.00m,164.00m,1,1),
                    ("Pipefitter Journeyman", "Direct",  "PF", 76.00m,114.00m,152.00m,3,2),
                    ("Pipefitter Helper",     "Direct",  "PFH",50.00m, 75.00m,100.00m,2,3),
                    ("Welder Journeyman",     "Direct",  "WD", 83.00m,124.50m,166.00m,1,4),
                },No(),No2(),0.71m),

            E("ETS","H26-011-CHN","Cheniere Sabine Pass Unit 4 Tie-in",
                "Cheniere Energy","CHN",null,"Turnaround","Sabine Pass","TX",null,
                21,new DateTime(2026,6,16),new DateTime(2026,7,6),"Submitted",65,"Both",16,
                "Carlos Vega","Lisa Herrera","Gulf","m.delgado",-5,-3,
                new[]{
                    ("General Foreman",        "Indirect","SUP", 95.00m,142.50m,190.00m,1,1),
                    ("Foreman",                "Indirect","SUP", 82.00m,123.00m,164.00m,2,2),
                    ("Safety Watch",           "Indirect","SAF", 46.00m, 69.00m, 92.00m,2,3),
                    ("Pipefitter Journeyman",  "Direct",  "PF",  76.00m,114.00m,152.00m,4,4),
                    ("Pipefitter Helper",      "Direct",  "PFH", 50.00m, 75.00m,100.00m,3,5),
                    ("Welder Journeyman",      "Direct",  "WD",  83.00m,124.50m,166.00m,2,6),
                    ("Boilermaker Journeyman", "Direct",  "BM",  80.00m,120.00m,160.00m,2,7),
                },No(),Pd("Per Diem – 13 persons",125m,21,13),0.70m),

            // ── ETS PENDING ──────────────────────────────────────────────────
            E("ETS","H26-005-FHR","Flint Hills Corpus Christi Inspection",
                "Flint Hills Resources","FHR",null,"Inspection","Corpus Christi","TX","Flint Hills Corpus Christi",
                14,new DateTime(2026,5,20),new DateTime(2026,6,2),"Pending",55,"Day",6,
                "Carlos Vega","Lisa Herrera","South TX","r.santos",-10,-8,
                new[]{
                    ("Foreman",         "Indirect","SUP",82.00m,123.00m,164.00m,1,1),
                    ("NDT Technician",  "Direct",  "NDT",92.00m,138.00m,184.00m,2,2),
                    ("Instrument Tech", "Direct",  "IE", 86.00m,129.00m,172.00m,3,3),
                },No(),No2(),0.70m),

            E("ETS","H26-008-LYB","LyondellBasell Channelview Maintenance",
                "LyondellBasell Industries","LYB",null,"Maintenance","Channelview","TX",null,
                14,new DateTime(2026,6,9),new DateTime(2026,6,22),"Pending",50,"Day",7,
                "Mike Rodriguez","Lisa Herrera","Gulf","m.delgado",-7,-5,
                new[]{
                    ("Foreman",               "Indirect","SUP",82.00m,123.00m,164.00m,1,1),
                    ("Pipefitter Journeyman", "Direct",  "PF", 76.00m,114.00m,152.00m,3,2),
                    ("Pipefitter Helper",     "Direct",  "PFH",50.00m, 75.00m,100.00m,2,3),
                    ("Welder Journeyman",     "Direct",  "WD", 83.00m,124.50m,166.00m,1,4),
                },No(),No2(),0.71m),

            E("ETS","H26-010-CTG","CITGO Lake Charles Turnaround",
                "CITGO Petroleum","CTG",null,"Turnaround","Lake Charles","LA",null,
                28,new DateTime(2026,6,30),new DateTime(2026,7,27),"Pending",55,"Both",20,
                "Carlos Vega","Lisa Herrera","Gulf","r.santos",-5,-3,
                new[]{
                    ("General Foreman",        "Indirect","SUP", 95.00m,142.50m,190.00m,1,1),
                    ("Foreman",                "Indirect","SUP", 82.00m,123.00m,164.00m,2,2),
                    ("Safety Watch",           "Indirect","SAF", 46.00m, 69.00m, 92.00m,3,3),
                    ("Pipefitter Journeyman",  "Direct",  "PF",  76.00m,114.00m,152.00m,5,4),
                    ("Pipefitter Helper",      "Direct",  "PFH", 50.00m, 75.00m,100.00m,3,5),
                    ("Welder Journeyman",      "Direct",  "WD",  83.00m,124.50m,166.00m,2,6),
                    ("Boilermaker Journeyman", "Direct",  "BM",  80.00m,120.00m,160.00m,2,7),
                    ("Crane Operator",         "Direct",  "OPR", 92.00m,138.00m,184.00m,1,8),
                    ("Rigger",                 "Direct",  "RIG", 70.00m,105.00m,140.00m,1,9),
                },No(),Pd("Per Diem – 16 persons",125m,28,16),0.70m),

            E("ETS","H26-012-VLO","Valero McKee Refinery Repair",
                "Valero Energy","VLO",null,"Maintenance","Sunray","TX","Valero McKee Refinery",
                14,new DateTime(2026,7,7),new DateTime(2026,7,20),"Pending",50,"Day",7,
                "Mike Rodriguez","Lisa Herrera","South TX","m.delgado",-3,-1,
                new[]{
                    ("Foreman",               "Indirect","SUP",82.00m,123.00m,164.00m,1,1),
                    ("Pipefitter Journeyman", "Direct",  "PF", 76.00m,114.00m,152.00m,3,2),
                    ("Pipefitter Helper",     "Direct",  "PFH",50.00m, 75.00m,100.00m,2,3),
                    ("Welder Journeyman",     "Direct",  "WD", 83.00m,124.50m,166.00m,1,4),
                    ("Boilermaker Journeyman","Direct",  "BM", 80.00m,120.00m,160.00m,1,5),
                },No(),No2(),0.71m),

            // ── Q3/Q4 2026 — CSL ────────────────────────────────────────────────
            E("CSL","26-0015-SHELL","Shell Deer Park HDS Turnaround",
                "Shell Oil Company","SHELL","MSA-SHELL-2024-01","Turnaround","Deer Park","TX","Shell Deer Park Refinery",
                28,new DateTime(2026,8,3),new DateTime(2026,8,30),"Awarded",100,"Both",22,
                "Mark Ellis","Rachel Kim","Gulf","michael.santos",-28,-21,
                new[]{
                    ("Project Manager",        "Indirect","MGT",125.00m,187.50m,250.00m,1,1),
                    ("General Foreman",        "Indirect","SUP", 98.00m,147.00m,196.00m,1,2),
                    ("Foreman",                "Indirect","SUP", 85.00m,127.50m,170.00m,2,3),
                    ("Safety Watch",           "Indirect","SAF", 48.00m, 72.00m, 96.00m,3,4),
                    ("Fire Watch",             "Indirect","SAF", 45.00m, 67.50m, 90.00m,2,5),
                    ("Pipefitter Journeyman",  "Direct",  "PF",  78.00m,117.00m,156.00m,6,6),
                    ("Pipefitter Helper",      "Direct",  "PFH", 52.00m, 78.00m,104.00m,4,7),
                    ("Welder Journeyman",      "Direct",  "WD",  85.00m,127.50m,170.00m,2,8),
                    ("Boilermaker Journeyman", "Direct",  "BM",  82.00m,123.00m,164.00m,1,9),
                },No(),Pd("Per Diem – 18 persons",125m,28,18),0.68m),

            E("CSL","26-0017-XOM","ExxonMobil Beaumont SRU Inspection",
                "ExxonMobil","XOM","MSA-XOM-2024-02","Inspection","Beaumont","TX","ExxonMobil Beaumont Refinery",
                14,new DateTime(2026,9,8),new DateTime(2026,9,21),"Awarded",100,"Day",8,
                "David Torres","Rachel Kim","Gulf","carol.whitfield",-21,-14,
                new[]{
                    ("General Foreman",       "Indirect","SUP", 98.00m,147.00m,196.00m,1,1),
                    ("Foreman",               "Indirect","SUP", 85.00m,127.50m,170.00m,1,2),
                    ("Safety Watch",          "Indirect","SAF", 48.00m, 72.00m, 96.00m,1,3),
                    ("Pipefitter Journeyman", "Direct",  "PF",  78.00m,117.00m,156.00m,3,4),
                    ("Pipefitter Helper",     "Direct",  "PFH", 52.00m, 78.00m,104.00m,2,5),
                },No(),No2(),0.70m),

            E("CSL","26-0019-LYB","LyondellBasell Houston Polyethylene TA",
                "LyondellBasell","LYB",null,"Turnaround","Houston","TX","LyondellBasell Houston Complex",
                28,new DateTime(2026,10,5),new DateTime(2026,11,1),"Submitted",80,"Both",20,
                "David Torres","Rachel Kim","Gulf","james.tanner",-14,-7,
                new[]{
                    ("Project Manager",        "Indirect","MGT",125.00m,187.50m,250.00m,1,1),
                    ("General Foreman",        "Indirect","SUP", 98.00m,147.00m,196.00m,1,2),
                    ("Foreman",                "Indirect","SUP", 85.00m,127.50m,170.00m,2,3),
                    ("Safety Watch",           "Indirect","SAF", 48.00m, 72.00m, 96.00m,2,4),
                    ("Pipefitter Journeyman",  "Direct",  "PF",  78.00m,117.00m,156.00m,6,5),
                    ("Pipefitter Helper",      "Direct",  "PFH", 52.00m, 78.00m,104.00m,4,6),
                    ("Welder Journeyman",      "Direct",  "WD",  85.00m,127.50m,170.00m,2,7),
                    ("Boilermaker Journeyman", "Direct",  "BM",  82.00m,123.00m,164.00m,2,8),
                },No(),Pd("Per Diem – 16 persons",125m,28,16),0.69m),

            E("CSL","26-0021-VLO","Valero Port Arthur Hydrocracker TA",
                "Valero Energy","VLO",null,"Turnaround","Port Arthur","TX","Valero Port Arthur Refinery",
                35,new DateTime(2026,11,2),new DateTime(2026,12,6),"Pending",65,"Both",26,
                "Mark Ellis","Rachel Kim","Gulf","tom.bishop",-10,-5,
                new[]{
                    ("Project Manager",        "Indirect","MGT",125.00m,187.50m,250.00m,1,1),
                    ("General Foreman",        "Indirect","SUP", 98.00m,147.00m,196.00m,1,2),
                    ("Foreman",                "Indirect","SUP", 85.00m,127.50m,170.00m,3,3),
                    ("Safety Watch",           "Indirect","SAF", 48.00m, 72.00m, 96.00m,3,4),
                    ("Fire Watch",             "Indirect","SAF", 45.00m, 67.50m, 90.00m,2,5),
                    ("Pipefitter Journeyman",  "Direct",  "PF",  78.00m,117.00m,156.00m,7,6),
                    ("Pipefitter Helper",      "Direct",  "PFH", 52.00m, 78.00m,104.00m,4,7),
                    ("Welder Journeyman",      "Direct",  "WD",  85.00m,127.50m,170.00m,3,8),
                    ("Boilermaker Journeyman", "Direct",  "BM",  82.00m,123.00m,164.00m,2,9),
                },No(),Pd("Per Diem – 22 persons",125m,35,22),0.68m),

            // ── Q3/Q4 2026 — ETS ────────────────────────────────────────────────
            E("ETS","H26-013-SHELL","Shell Deer Park Ethylene Pipe Repairs",
                "Shell Oil Company","SHELL","MSA-SHELL-2024-01","Maintenance","Deer Park","TX","Shell Deer Park Ethylene Plant",
                21,new DateTime(2026,8,10),new DateTime(2026,8,30),"Awarded",100,"Both",14,
                "John Castillo","Lisa Herrera","Gulf","m.delgado",-25,-18,
                new[]{
                    ("General Foreman",       "Indirect","SUP", 95.00m,142.50m,190.00m,1,1),
                    ("Foreman",               "Indirect","SUP", 82.00m,123.00m,164.00m,2,2),
                    ("Safety Watch",          "Indirect","SAF", 46.00m, 69.00m, 92.00m,2,3),
                    ("Pipefitter Journeyman", "Direct",  "PF",  76.00m,114.00m,152.00m,5,4),
                    ("Pipefitter Helper",     "Direct",  "PFH", 50.00m, 75.00m,100.00m,3,5),
                    ("Welder Journeyman",     "Direct",  "WD",  83.00m,124.50m,166.00m,1,6),
                },No(),Pd("Per Diem – 11 persons",125m,21,11),0.70m),

            E("ETS","H26-014-DOW","Dow Freeport Ethylene Compressor TA",
                "Dow Chemical","DOW",null,"Turnaround","Freeport","TX","Dow Chemical Freeport Complex",
                28,new DateTime(2026,9,7),new DateTime(2026,10,4),"Pending",60,"Both",18,
                "John Castillo","Lisa Herrera","Gulf","r.santos",-15,-8,
                new[]{
                    ("General Foreman",       "Indirect","SUP", 95.00m,142.50m,190.00m,1,1),
                    ("Foreman",               "Indirect","SUP", 82.00m,123.00m,164.00m,2,2),
                    ("Safety Watch",          "Indirect","SAF", 46.00m, 69.00m, 92.00m,2,3),
                    ("Pipefitter Journeyman", "Direct",  "PF",  76.00m,114.00m,152.00m,6,4),
                    ("Pipefitter Helper",     "Direct",  "PFH", 50.00m, 75.00m,100.00m,4,5),
                    ("Welder Journeyman",     "Direct",  "WD",  83.00m,124.50m,166.00m,2,6),
                    ("Boilermaker Journeyman","Direct",  "BM",  80.00m,120.00m,160.00m,1,7),
                },No(),Pd("Per Diem – 14 persons",125m,28,14),0.70m),

            E("ETS","H26-015-BASF","BASF Geismar Ammonia Unit Inspection",
                "BASF Corporation","BASF",null,"Inspection","Geismar","LA","BASF Geismar Complex",
                21,new DateTime(2026,10,12),new DateTime(2026,11,1),"Submitted",75,"Day",10,
                "John Castillo","Lisa Herrera","Gulf","m.delgado",-12,-6,
                new[]{
                    ("General Foreman",       "Indirect","SUP", 95.00m,142.50m,190.00m,1,1),
                    ("Foreman",               "Indirect","SUP", 82.00m,123.00m,164.00m,1,2),
                    ("Pipefitter Journeyman", "Direct",  "PF",  76.00m,114.00m,152.00m,5,3),
                    ("Pipefitter Helper",     "Direct",  "PFH", 50.00m, 75.00m,100.00m,2,4),
                    ("Welder Journeyman",     "Direct",  "WD",  83.00m,124.50m,166.00m,1,5),
                },No(),No2(),0.71m),

            E("ETS","H26-016-CHN","Cheniere Sabine Pass LNG Tank Inspection",
                "Cheniere Energy","CHN",null,"Inspection","Sabine Pass","TX","Cheniere Sabine Pass LNG",
                28,new DateTime(2026,11,9),new DateTime(2026,12,6),"Pending",55,"Both",16,
                "Mike Rodriguez","Lisa Herrera","South TX","r.santos",-8,-3,
                new[]{
                    ("General Foreman",       "Indirect","SUP", 95.00m,142.50m,190.00m,1,1),
                    ("Foreman",               "Indirect","SUP", 82.00m,123.00m,164.00m,2,2),
                    ("Safety Watch",          "Indirect","SAF", 46.00m, 69.00m, 92.00m,2,3),
                    ("Pipefitter Journeyman", "Direct",  "PF",  76.00m,114.00m,152.00m,5,4),
                    ("Pipefitter Helper",     "Direct",  "PFH", 50.00m, 75.00m,100.00m,4,5),
                    ("Welder Journeyman",     "Direct",  "WD",  83.00m,124.50m,166.00m,2,6),
                },No(),Pd("Per Diem – 12 persons",125m,28,12),0.70m),

            // ── 2025 HISTORICAL — CSL ────────────────────────────────────────────
            E("CSL","25-0001-SHELL","Shell Deer Park Crude Unit TA",
                "Shell Oil Company","SHELL","MSA-SHELL-2024-01","Turnaround","Deer Park","TX","Shell Deer Park Refinery",
                28,new DateTime(2025,1,6),new DateTime(2025,2,2),"Awarded",100,"Both",22,
                "Mark Ellis","Rachel Kim","Gulf","michael.santos",-475,-460,
                new[]{
                    ("Project Manager",        "Indirect","MGT",122.00m,183.00m,244.00m,1,1),
                    ("General Foreman",        "Indirect","SUP", 95.00m,142.50m,190.00m,1,2),
                    ("Foreman",                "Indirect","SUP", 82.00m,123.00m,164.00m,2,3),
                    ("Safety Watch",           "Indirect","SAF", 46.00m, 69.00m, 92.00m,2,4),
                    ("Pipefitter Journeyman",  "Direct",  "PF",  76.00m,114.00m,152.00m,6,5),
                    ("Pipefitter Helper",      "Direct",  "PFH", 50.00m, 75.00m,100.00m,4,6),
                    ("Welder Journeyman",      "Direct",  "WD",  83.00m,124.50m,166.00m,3,7),
                    ("Boilermaker Journeyman", "Direct",  "BM",  80.00m,120.00m,160.00m,2,8),
                    ("Rigger",                 "Direct",  "RIG", 72.00m,108.00m,144.00m,1,9),
                },No(),Pd("Per Diem – 19 persons",125m,28,19),0.69m),

            E("CSL","25-0002-BP","BP Texas City Coker TA",
                "British Petroleum","BP","MSA-BP-2024-01","Turnaround","Texas City","TX","BP Texas City Refinery",
                21,new DateTime(2025,2,10),new DateTime(2025,3,2),"Awarded",100,"Both",18,
                "David Torres","Rachel Kim","Gulf","james.tanner",-450,-440,
                new[]{
                    ("General Foreman",        "Indirect","SUP", 95.00m,142.50m,190.00m,1,1),
                    ("Foreman",                "Indirect","SUP", 82.00m,123.00m,164.00m,2,2),
                    ("Safety Watch",           "Indirect","SAF", 46.00m, 69.00m, 92.00m,2,3),
                    ("Pipefitter Journeyman",  "Direct",  "PF",  76.00m,114.00m,152.00m,5,4),
                    ("Pipefitter Helper",      "Direct",  "PFH", 50.00m, 75.00m,100.00m,3,5),
                    ("Welder Journeyman",      "Direct",  "WD",  83.00m,124.50m,166.00m,2,6),
                    ("Boilermaker Journeyman", "Direct",  "BM",  80.00m,120.00m,160.00m,2,7),
                },No(),Pd("Per Diem – 16 persons",125m,21,16),0.68m),

            E("CSL","25-0003-DOW","Dow Chemical Deer Park Ethylene TA",
                "Dow Chemical","DOW",null,"Turnaround","Deer Park","TX","Dow Chemical Deer Park",
                14,new DateTime(2025,3,3),new DateTime(2025,3,16),"Awarded",100,"Day",12,
                "Mark Ellis","Rachel Kim","Gulf","carol.whitfield",-420,-415,
                new[]{
                    ("General Foreman",       "Indirect","SUP",95.00m,142.50m,190.00m,1,1),
                    ("Foreman",               "Indirect","SUP",82.00m,123.00m,164.00m,1,2),
                    ("Safety Watch",          "Indirect","SAF",46.00m, 69.00m, 92.00m,1,3),
                    ("Pipefitter Journeyman", "Direct",  "PF", 76.00m,114.00m,152.00m,4,4),
                    ("Pipefitter Helper",     "Direct",  "PFH",50.00m, 75.00m,100.00m,2,5),
                    ("Welder Journeyman",     "Direct",  "WD", 83.00m,124.50m,166.00m,2,6),
                },No(),No2(),0.70m),

            E("CSL","25-0004-VLO","Valero Port Arthur Hydrocracker",
                "Valero Energy","VLO",null,"Turnaround","Port Arthur","TX","Valero Port Arthur Refinery",
                35,new DateTime(2025,3,17),new DateTime(2025,4,20),"Awarded",100,"Both",28,
                "David Torres","Rachel Kim","Gulf","tom.bishop",-410,-400,
                new[]{
                    ("Project Manager",        "Indirect","MGT",122.00m,183.00m,244.00m,1,1),
                    ("General Foreman",        "Indirect","SUP", 95.00m,142.50m,190.00m,1,2),
                    ("Foreman",                "Indirect","SUP", 82.00m,123.00m,164.00m,3,3),
                    ("Safety Watch",           "Indirect","SAF", 46.00m, 69.00m, 92.00m,3,4),
                    ("Fire Watch",             "Indirect","SAF", 43.00m, 64.50m, 86.00m,2,5),
                    ("Pipefitter Journeyman",  "Direct",  "PF",  76.00m,114.00m,152.00m,8,6),
                    ("Pipefitter Helper",      "Direct",  "PFH", 50.00m, 75.00m,100.00m,4,7),
                    ("Welder Journeyman",      "Direct",  "WD",  83.00m,124.50m,166.00m,3,8),
                    ("Boilermaker Journeyman", "Direct",  "BM",  80.00m,120.00m,160.00m,2,9),
                    ("Crane Operator",         "Direct",  "OPR", 94.00m,141.00m,188.00m,1,10),
                },No(),Pd("Per Diem – 24 persons",125m,35,24),0.68m),

            E("CSL","25-0005-XOM","ExxonMobil Baytown FCC Unit",
                "ExxonMobil","XOM","MSA-XOM-2024-02","Turnaround","Baytown","TX","ExxonMobil Baytown Refinery",
                42,new DateTime(2025,4,1),new DateTime(2025,5,12),"Awarded",100,"Both",32,
                "David Torres","Jennifer Bates","Gulf","james.tanner",-390,-375,
                new[]{
                    ("Project Manager",        "Indirect","MGT",122.00m,183.00m,244.00m,1,1),
                    ("General Foreman",        "Indirect","SUP", 95.00m,142.50m,190.00m,2,2),
                    ("Foreman",                "Indirect","SUP", 82.00m,123.00m,164.00m,4,3),
                    ("Safety Watch",           "Indirect","SAF", 46.00m, 69.00m, 92.00m,4,4),
                    ("Fire Watch",             "Indirect","SAF", 43.00m, 64.50m, 86.00m,2,5),
                    ("Pipefitter Journeyman",  "Direct",  "PF",  76.00m,114.00m,152.00m,10,6),
                    ("Pipefitter Helper",      "Direct",  "PFH", 50.00m, 75.00m,100.00m,5,7),
                    ("Welder Journeyman",      "Direct",  "WD",  83.00m,124.50m,166.00m,4,8),
                    ("Boilermaker Journeyman", "Direct",  "BM",  80.00m,120.00m,160.00m,3,9),
                },No(),Pd("Per Diem – 28 persons",125m,42,28),0.68m),

            E("CSL","25-0006-BASF","BASF Freeport Reactor Work",
                "BASF Corporation","BASF",null,"Maintenance","Freeport","TX","BASF Freeport Complex",
                10,new DateTime(2025,5,19),new DateTime(2025,5,28),"Awarded",100,"Day",8,
                "Mark Ellis","Rachel Kim","Gulf","michael.santos",-345,-342,
                new[]{
                    ("Foreman",               "Indirect","SUP",82.00m,123.00m,164.00m,1,1),
                    ("Pipefitter Journeyman", "Direct",  "PF", 76.00m,114.00m,152.00m,3,2),
                    ("Pipefitter Helper",     "Direct",  "PFH",50.00m, 75.00m,100.00m,2,3),
                    ("Welder Journeyman",     "Direct",  "WD", 83.00m,124.50m,166.00m,1,4),
                },No(),No2(),0.71m),

            E("CSL","25-0007-LYB","LyondellBasell Channelview Olefins",
                "LyondellBasell","LYB",null,"Turnaround","Channelview","TX","LyondellBasell Channelview",
                21,new DateTime(2025,6,2),new DateTime(2025,6,22),"Awarded",100,"Day",15,
                "David Torres","Rachel Kim","Gulf","carol.whitfield",-330,-325,
                new[]{
                    ("General Foreman",       "Indirect","SUP",95.00m,142.50m,190.00m,1,1),
                    ("Foreman",               "Indirect","SUP",82.00m,123.00m,164.00m,1,2),
                    ("Safety Watch",          "Indirect","SAF",46.00m, 69.00m, 92.00m,2,3),
                    ("Pipefitter Journeyman", "Direct",  "PF", 76.00m,114.00m,152.00m,5,4),
                    ("Pipefitter Helper",     "Direct",  "PFH",50.00m, 75.00m,100.00m,3,5),
                    ("Welder Journeyman",     "Direct",  "WD", 83.00m,124.50m,166.00m,2,6),
                },No(),No2(),0.70m),

            E("CSL","25-0008-VLO","Valero Meraux Pipe Replacement",
                "Valero Energy","VLO",null,"Maintenance","Meraux","LA","Valero Meraux Refinery",
                7,new DateTime(2025,7,7),new DateTime(2025,7,13),"Lost",0,"Day",5,
                "Mark Ellis","Rachel Kim","Gulf","michael.santos",-295,-294,
                new[]{
                    ("Foreman",               "Indirect","SUP",82.00m,123.00m,164.00m,1,1),
                    ("Pipefitter Journeyman", "Direct",  "PF", 76.00m,114.00m,152.00m,2,2),
                    ("Pipefitter Helper",     "Direct",  "PFH",50.00m, 75.00m,100.00m,1,3),
                },No(),No2(),0.71m,"Competitor"),

            E("CSL","25-0009-DOW","Dow Deer Park Styrene Unit",
                "Dow Chemical","DOW",null,"Turnaround","Deer Park","TX","Dow Chemical Deer Park",
                18,new DateTime(2025,7,14),new DateTime(2025,7,31),"Awarded",100,"Both",14,
                "Mark Ellis","Rachel Kim","Gulf","james.tanner",-285,-280,
                new[]{
                    ("General Foreman",       "Indirect","SUP",95.00m,142.50m,190.00m,1,1),
                    ("Foreman",               "Indirect","SUP",82.00m,123.00m,164.00m,1,2),
                    ("Safety Watch",          "Indirect","SAF",46.00m, 69.00m, 92.00m,2,3),
                    ("Pipefitter Journeyman", "Direct",  "PF", 76.00m,114.00m,152.00m,4,4),
                    ("Pipefitter Helper",     "Direct",  "PFH",50.00m, 75.00m,100.00m,2,5),
                    ("Welder Journeyman",     "Direct",  "WD", 83.00m,124.50m,166.00m,2,6),
                },No(),No2(),0.69m),

            E("CSL","25-0010-SHELL","Shell Norco Alkylation Unit",
                "Shell Oil Company","SHELL","MSA-SHELL-2024-01","Turnaround","Norco","LA","Shell Norco Refinery",
                28,new DateTime(2025,8,4),new DateTime(2025,9,1),"Awarded",100,"Both",20,
                "Mark Ellis","Jennifer Bates","Gulf","michael.santos",-265,-255,
                new[]{
                    ("Project Manager",        "Indirect","MGT",122.00m,183.00m,244.00m,1,1),
                    ("General Foreman",        "Indirect","SUP", 95.00m,142.50m,190.00m,1,2),
                    ("Foreman",                "Indirect","SUP", 82.00m,123.00m,164.00m,2,3),
                    ("Safety Watch",           "Indirect","SAF", 46.00m, 69.00m, 92.00m,2,4),
                    ("Pipefitter Journeyman",  "Direct",  "PF",  76.00m,114.00m,152.00m,6,5),
                    ("Pipefitter Helper",      "Direct",  "PFH", 50.00m, 75.00m,100.00m,3,6),
                    ("Welder Journeyman",      "Direct",  "WD",  83.00m,124.50m,166.00m,2,7),
                    ("Boilermaker Journeyman", "Direct",  "BM",  80.00m,120.00m,160.00m,2,8),
                },No(),Pd("Per Diem – 17 persons",125m,28,17),0.69m),

            E("CSL","25-0011-BP","BP Baytown Vacuum Unit — Lost",
                "British Petroleum","BP","MSA-BP-2024-01","Turnaround","Baytown","TX","BP Baytown Refinery",
                21,new DateTime(2025,9,8),new DateTime(2025,9,28),"Lost",0,"Both",18,
                "David Torres","Rachel Kim","Gulf","james.tanner",-235,-234,
                new[]{
                    ("General Foreman",       "Indirect","SUP",95.00m,142.50m,190.00m,1,1),
                    ("Foreman",               "Indirect","SUP",82.00m,123.00m,164.00m,2,2),
                    ("Pipefitter Journeyman", "Direct",  "PF", 76.00m,114.00m,152.00m,6,3),
                    ("Pipefitter Helper",     "Direct",  "PFH",50.00m, 75.00m,100.00m,4,4),
                    ("Welder Journeyman",     "Direct",  "WD", 83.00m,124.50m,166.00m,2,5),
                },No(),Pd("Per Diem – 15 persons",125m,21,15),0.69m,"Pricing"),

            E("CSL","25-0012-XOM","ExxonMobil Beaumont Reformer",
                "ExxonMobil","XOM","MSA-XOM-2024-02","Turnaround","Beaumont","TX","ExxonMobil Beaumont Refinery",
                28,new DateTime(2025,10,6),new DateTime(2025,11,2),"Awarded",100,"Both",24,
                "David Torres","Jennifer Bates","Gulf","carol.whitfield",-205,-195,
                new[]{
                    ("Project Manager",        "Indirect","MGT",122.00m,183.00m,244.00m,1,1),
                    ("General Foreman",        "Indirect","SUP", 95.00m,142.50m,190.00m,1,2),
                    ("Foreman",                "Indirect","SUP", 82.00m,123.00m,164.00m,2,3),
                    ("Safety Watch",           "Indirect","SAF", 46.00m, 69.00m, 92.00m,3,4),
                    ("Pipefitter Journeyman",  "Direct",  "PF",  76.00m,114.00m,152.00m,7,5),
                    ("Pipefitter Helper",      "Direct",  "PFH", 50.00m, 75.00m,100.00m,4,6),
                    ("Welder Journeyman",      "Direct",  "WD",  83.00m,124.50m,166.00m,3,7),
                    ("Boilermaker Journeyman", "Direct",  "BM",  80.00m,120.00m,160.00m,2,8),
                    ("Crane Operator",         "Direct",  "OPR", 94.00m,141.00m,188.00m,1,9),
                },No(),Pd("Per Diem – 21 persons",125m,28,21),0.68m),

            E("CSL","25-0013-LYB","LyondellBasell Corpus Cristi Polypropylene",
                "LyondellBasell","LYB",null,"Maintenance","Corpus Christi","TX","LyondellBasell Corpus Christi",
                14,new DateTime(2025,11,3),new DateTime(2025,11,16),"Lost",0,"Day",10,
                "Mark Ellis","Rachel Kim","Gulf","michael.santos",-175,-174,
                new[]{
                    ("Foreman",               "Indirect","SUP",82.00m,123.00m,164.00m,1,1),
                    ("Pipefitter Journeyman", "Direct",  "PF", 76.00m,114.00m,152.00m,4,2),
                    ("Pipefitter Helper",     "Direct",  "PFH",50.00m, 75.00m,100.00m,2,3),
                },No(),No2(),0.71m,"No Decision"),

            E("CSL","25-0014-SHELL","Shell Deer Park HDS Unit",
                "Shell Oil Company","SHELL","MSA-SHELL-2024-01","Turnaround","Deer Park","TX","Shell Deer Park Refinery",
                21,new DateTime(2025,11,10),new DateTime(2025,11,30),"Awarded",100,"Both",16,
                "Mark Ellis","Jennifer Bates","Gulf","james.tanner",-170,-162,
                new[]{
                    ("General Foreman",       "Indirect","SUP",95.00m,142.50m,190.00m,1,1),
                    ("Foreman",               "Indirect","SUP",82.00m,123.00m,164.00m,2,2),
                    ("Safety Watch",          "Indirect","SAF",46.00m, 69.00m, 92.00m,2,3),
                    ("Pipefitter Journeyman", "Direct",  "PF", 76.00m,114.00m,152.00m,5,4),
                    ("Pipefitter Helper",     "Direct",  "PFH",50.00m, 75.00m,100.00m,3,5),
                    ("Welder Journeyman",     "Direct",  "WD", 83.00m,124.50m,166.00m,2,6),
                },No(),Pd("Per Diem – 13 persons",125m,21,13),0.70m),

            // ── 2025 HISTORICAL — ETS ────────────────────────────────────────────
            E("ETS","H25-001-SHELL","Shell Deer Park CDU Inspection",
                "Shell Oil Company","SHELL",null,"Inspection","Deer Park","TX","Shell Deer Park Refinery",
                14,new DateTime(2025,1,13),new DateTime(2025,1,26),"Awarded",100,"Day",8,
                "Tom Martinez","Lisa Herrera","Gulf","a.ramirez",-470,-465,
                new[]{
                    ("Foreman",          "Indirect","SUP",80.00m,120.00m,160.00m,1,1),
                    ("NDT Technician",   "Direct",  "NDT",93.00m,139.50m,186.00m,3,2),
                    ("Instrument Tech",  "Direct",  "IE", 86.00m,129.00m,172.00m,2,3),
                },No(),No2(),0.71m),

            E("ETS","H25-002-BP","BP Texas City FCC Turnaround",
                "British Petroleum","BP",null,"Turnaround","Texas City","TX","BP Texas City Refinery",
                21,new DateTime(2025,2,3),new DateTime(2025,2,23),"Awarded",100,"Both",16,
                "Tom Martinez","Lisa Herrera","Gulf","m.delgado",-450,-445,
                new[]{
                    ("General Foreman",        "Indirect","SUP", 93.00m,139.50m,186.00m,1,1),
                    ("Foreman",                "Indirect","SUP", 80.00m,120.00m,160.00m,2,2),
                    ("Safety Watch",           "Indirect","SAF", 44.00m, 66.00m, 88.00m,2,3),
                    ("Pipefitter Journeyman",  "Direct",  "PF",  74.00m,111.00m,148.00m,5,4),
                    ("Pipefitter Helper",      "Direct",  "PFH", 48.00m, 72.00m, 96.00m,3,5),
                    ("Welder Journeyman",      "Direct",  "WD",  81.00m,121.50m,162.00m,2,6),
                },No(),Pd("Per Diem – 14 persons",125m,21,14),0.70m),

            E("ETS","H25-003-DOW","Dow Chemical Deer Park Ethylene Cracker",
                "Dow Chemical","DOW",null,"Turnaround","Deer Park","TX","Dow Chemical Deer Park",
                35,new DateTime(2025,3,10),new DateTime(2025,4,13),"Awarded",100,"Both",26,
                "Tom Martinez","Lisa Herrera","Gulf","a.ramirez",-420,-410,
                new[]{
                    ("Project Manager",        "Indirect","MGT",120.00m,180.00m,240.00m,1,1),
                    ("General Foreman",        "Indirect","SUP", 93.00m,139.50m,186.00m,1,2),
                    ("Foreman",                "Indirect","SUP", 80.00m,120.00m,160.00m,3,3),
                    ("Safety Watch",           "Indirect","SAF", 44.00m, 66.00m, 88.00m,3,4),
                    ("Pipefitter Journeyman",  "Direct",  "PF",  74.00m,111.00m,148.00m,8,5),
                    ("Pipefitter Helper",      "Direct",  "PFH", 48.00m, 72.00m, 96.00m,4,6),
                    ("Welder Journeyman",      "Direct",  "WD",  81.00m,121.50m,162.00m,3,7),
                    ("Boilermaker Journeyman", "Direct",  "BM",  78.00m,117.00m,156.00m,2,8),
                },No(),Pd("Per Diem – 22 persons",125m,35,22),0.69m),

            E("ETS","H25-004-VLO","Valero Port Arthur Reformer — Lost",
                "Valero Energy","VLO",null,"Turnaround","Port Arthur","TX","Valero Port Arthur Refinery",
                28,new DateTime(2025,4,14),new DateTime(2025,5,11),"Lost",0,"Both",20,
                "Mike Rodriguez","Lisa Herrera","South TX","m.delgado",-385,-384,
                new[]{
                    ("General Foreman",       "Indirect","SUP",93.00m,139.50m,186.00m,1,1),
                    ("Foreman",               "Indirect","SUP",80.00m,120.00m,160.00m,2,2),
                    ("Pipefitter Journeyman", "Direct",  "PF", 74.00m,111.00m,148.00m,6,3),
                    ("Pipefitter Helper",     "Direct",  "PFH",48.00m, 72.00m, 96.00m,4,4),
                },No(),Pd("Per Diem – 13 persons",125m,28,13),0.70m,"Pricing"),

            E("ETS","H25-005-XOM","ExxonMobil Beaumont CDU Inspection",
                "ExxonMobil","XOM",null,"Inspection","Beaumont","TX","ExxonMobil Beaumont Refinery",
                10,new DateTime(2025,5,5),new DateTime(2025,5,14),"Awarded",100,"Day",6,
                "Tom Martinez","Lisa Herrera","Gulf","a.ramirez",-360,-358,
                new[]{
                    ("Foreman",          "Indirect","SUP",80.00m,120.00m,160.00m,1,1),
                    ("NDT Technician",   "Direct",  "NDT",93.00m,139.50m,186.00m,2,2),
                    ("Instrument Tech",  "Direct",  "IE", 86.00m,129.00m,172.00m,2,3),
                },No(),No2(),0.72m),

            E("ETS","H25-006-BASF","BASF Geismar Amines Unit",
                "BASF Corporation","BASF",null,"Maintenance","Geismar","LA","BASF Geismar Complex",
                14,new DateTime(2025,6,9),new DateTime(2025,6,22),"Awarded",100,"Day",10,
                "Tom Martinez","Lisa Herrera","Gulf","m.delgado",-320,-318,
                new[]{
                    ("Foreman",               "Indirect","SUP",80.00m,120.00m,160.00m,1,1),
                    ("Pipefitter Journeyman", "Direct",  "PF", 74.00m,111.00m,148.00m,4,2),
                    ("Pipefitter Helper",     "Direct",  "PFH",48.00m, 72.00m, 96.00m,2,3),
                    ("Welder Journeyman",     "Direct",  "WD", 81.00m,121.50m,162.00m,2,4),
                },No(),No2(),0.71m),

            E("ETS","H25-007-LYB","LyondellBasell Houston Refinery Piping",
                "LyondellBasell","LYB",null,"Maintenance","Houston","TX","LyondellBasell Houston Refinery",
                7,new DateTime(2025,7,14),new DateTime(2025,7,20),"Lost",0,"Day",6,
                "Mike Rodriguez","Lisa Herrera","South TX","a.ramirez",-290,-289,
                new[]{
                    ("Foreman",               "Indirect","SUP",80.00m,120.00m,160.00m,1,1),
                    ("Pipefitter Journeyman", "Direct",  "PF", 74.00m,111.00m,148.00m,3,2),
                    ("Pipefitter Helper",     "Direct",  "PFH",48.00m, 72.00m, 96.00m,2,3),
                },No(),No2(),0.71m,"Competitor"),

            E("ETS","H25-008-SHELL","Shell Deer Park FCCU Piping",
                "Shell Oil Company","SHELL",null,"Turnaround","Deer Park","TX","Shell Deer Park Refinery",
                21,new DateTime(2025,9,8),new DateTime(2025,9,28),"Awarded",100,"Both",16,
                "Tom Martinez","Lisa Herrera","Gulf","m.delgado",-235,-228,
                new[]{
                    ("General Foreman",       "Indirect","SUP",93.00m,139.50m,186.00m,1,1),
                    ("Foreman",               "Indirect","SUP",80.00m,120.00m,160.00m,2,2),
                    ("Safety Watch",          "Indirect","SAF",44.00m, 66.00m, 88.00m,2,3),
                    ("Pipefitter Journeyman", "Direct",  "PF", 74.00m,111.00m,148.00m,5,4),
                    ("Pipefitter Helper",     "Direct",  "PFH",48.00m, 72.00m, 96.00m,3,5),
                    ("Welder Journeyman",     "Direct",  "WD", 81.00m,121.50m,162.00m,2,6),
                },No(),Pd("Per Diem – 13 persons",125m,21,13),0.70m),

            E("ETS","H25-009-DOW","Dow Deer Park Polymer Reactor Maintenance",
                "Dow Chemical","DOW",null,"Maintenance","Deer Park","TX","Dow Chemical Deer Park",
                14,new DateTime(2025,10,13),new DateTime(2025,10,26),"Awarded",100,"Day",11,
                "Tom Martinez","Lisa Herrera","Gulf","a.ramirez",-200,-197,
                new[]{
                    ("Foreman",               "Indirect","SUP",80.00m,120.00m,160.00m,1,1),
                    ("Pipefitter Journeyman", "Direct",  "PF", 74.00m,111.00m,148.00m,4,2),
                    ("Pipefitter Helper",     "Direct",  "PFH",48.00m, 72.00m, 96.00m,2,3),
                    ("Welder Journeyman",     "Direct",  "WD", 81.00m,121.50m,162.00m,2,6),
                },No(),No2(),0.71m),

            E("ETS","H25-010-XOM","ExxonMobil Baton Rouge Naphtha Splitter",
                "ExxonMobil","XOM","MSA-XOM-2024-02","Turnaround","Baton Rouge","LA","ExxonMobil Baton Rouge Refinery",
                28,new DateTime(2025,10,27),new DateTime(2025,11,23),"Awarded",100,"Both",22,
                "Tom Martinez","Jennifer Bates","Gulf","m.delgado",-183,-175,
                new[]{
                    ("Project Manager",        "Indirect","MGT",120.00m,180.00m,240.00m,1,1),
                    ("General Foreman",        "Indirect","SUP", 93.00m,139.50m,186.00m,1,2),
                    ("Foreman",                "Indirect","SUP", 80.00m,120.00m,160.00m,2,3),
                    ("Safety Watch",           "Indirect","SAF", 44.00m, 66.00m, 88.00m,2,4),
                    ("Pipefitter Journeyman",  "Direct",  "PF",  74.00m,111.00m,148.00m,6,5),
                    ("Pipefitter Helper",      "Direct",  "PFH", 48.00m, 72.00m, 96.00m,3,6),
                    ("Welder Journeyman",      "Direct",  "WD",  81.00m,121.50m,162.00m,2,7),
                    ("Boilermaker Journeyman", "Direct",  "BM",  78.00m,117.00m,156.00m,2,8),
                },No(),Pd("Per Diem – 18 persons",125m,28,18),0.69m),

            E("ETS","H25-011-VLO","Valero Corpus Christi East Refinery",
                "Valero Energy","VLO",null,"Turnaround","Corpus Christi","TX","Valero Corpus Christi East",
                21,new DateTime(2025,11,10),new DateTime(2025,11,30),"Awarded",100,"Both",17,
                "Mike Rodriguez","Lisa Herrera","South TX","a.ramirez",-170,-164,
                new[]{
                    ("General Foreman",       "Indirect","SUP",93.00m,139.50m,186.00m,1,1),
                    ("Foreman",               "Indirect","SUP",80.00m,120.00m,160.00m,2,2),
                    ("Safety Watch",          "Indirect","SAF",44.00m, 66.00m, 88.00m,2,3),
                    ("Pipefitter Journeyman", "Direct",  "PF", 74.00m,111.00m,148.00m,5,4),
                    ("Pipefitter Helper",     "Direct",  "PFH",48.00m, 72.00m, 96.00m,3,5),
                    ("Welder Journeyman",     "Direct",  "WD", 81.00m,121.50m,162.00m,2,6),
                },No(),Pd("Per Diem – 14 persons",125m,21,14),0.70m),
        };

        db.Estimates.AddRange(estimates);
        await db.SaveChangesAsync();
    }

    // ── Demo Valero Estimates (additive — always upsert by number) ────────────

    private static async Task SeedDemoValeroEstimates(AppDbContext db)
    {
        // 2025 reference job — full crew + equipment (the AI will find this when reviewing 2026)
        if (!await db.Estimates.AnyAsync(e => e.EstimateNumber == "25-0031-VLO"))
        {
            var e1 = E("CSL","25-0031-VLO","Valero Port Arthur CDU Turnaround 2025",
                "Valero Energy","VLO",null,"Turnaround","Port Arthur","TX","Valero Port Arthur Refinery",
                21,new DateTime(2025,8,4),new DateTime(2025,8,24),"Awarded",100,"Day",22,
                "David Torres","Jennifer Bates","Gulf","james.tanner",-265,-260,
                new[]{
                    ("General Foreman",        "Indirect","SUP", 98.00m,147.00m,196.00m,1,1),
                    ("Foreman",                "Indirect","SUP", 85.00m,127.50m,170.00m,2,2),
                    ("Pipefitter Journeyman",  "Direct",  "PF",  78.00m,117.00m,156.00m,6,3),
                    ("Pipefitter Helper",      "Direct",  "PFH", 52.00m, 78.00m,104.00m,4,4),
                    ("Welder Journeyman",      "Direct",  "WD",  85.00m,127.50m,170.00m,3,5),
                    ("Boilermaker Journeyman", "Direct",  "BM",  82.00m,123.00m,164.00m,2,6),
                    ("Crane Operator",         "Direct",  "OPR", 95.00m,142.50m,190.00m,1,7),
                    ("Safety Watch",           "Indirect","SAF", 48.00m, 72.00m, 96.00m,2,8),
                    ("NDT Technician",         "Direct",  "NDT", 95.00m,142.50m,190.00m,1,9),
                },
                new[]{
                    ("Crane 100 Ton",    "Daily",2500m,1,21),
                    ("Crane 50 Ton",     "Daily",1500m,2,21),
                    ("Manlift 60ft",     "Daily", 350m,4,21),
                    ("Forklift 5K",      "Daily", 150m,2,21),
                    ("Welding Machine",  "Daily",  85m,4,21),
                    ("Air Compressor",   "Daily", 125m,2,21),
                    ("Light Tower",      "Daily",  95m,2,21),
                },
                new[]{("PerDiem","Per Diem – 19 persons out of town",125m,"Day",21,19,true)},
                0.69m);
            db.Estimates.Add(e1);
        }

        // 2026 demo job — stripped crew, no equipment (what the boss opens for the demo)
        if (!await db.Estimates.AnyAsync(e => e.EstimateNumber == "26-0031-VLO"))
        {
            var e2 = E("CSL","26-0031-VLO","Valero Port Arthur Hydrocracker Turnaround",
                "Valero Energy","VLO",null,"Turnaround","Port Arthur","TX","Valero Port Arthur Refinery",
                21,new DateTime(2026,8,3),new DateTime(2026,8,23),"Draft",60,"Day",10,
                "David Torres","Jennifer Bates","Gulf","james.tanner",-2,-1,
                new[]{
                    ("General Foreman",       "Indirect","SUP",98.00m,147.00m,196.00m,1,1),
                    ("Foreman",               "Indirect","SUP",85.00m,127.50m,170.00m,1,2),
                    ("Pipefitter Journeyman", "Direct",  "PF", 78.00m,117.00m,156.00m,4,3),
                    ("Pipefitter Helper",     "Direct",  "PFH",52.00m, 78.00m,104.00m,2,4),
                    ("Welder Journeyman",     "Direct",  "WD", 85.00m,127.50m,170.00m,2,5),
                },
                No(),
                No2(),
                0.69m);
            db.Estimates.Add(e2);
        }

        await db.SaveChangesAsync();
    }

    // ── Rate Book Assignment ──────────────────────────────────────────────────

    private static async Task AssignRateBooksToEstimates(AppDbContext db)
    {
        var rateBooks = await db.RateBooks.ToListAsync();
        var estimates = await db.Estimates.ToListAsync();

        foreach (var est in estimates)
        {
            // Try exact client code + company match first
            var rb = rateBooks.FirstOrDefault(r =>
                r.CompanyCode == est.CompanyCode &&
                r.ClientCode == est.ClientCode &&
                !string.IsNullOrEmpty(r.City));

            // Fall back to any rate book for this client (ignoring city)
            rb ??= rateBooks.FirstOrDefault(r =>
                r.CompanyCode == est.CompanyCode &&
                r.ClientCode == est.ClientCode);

            // Final fallback: standard baseline for the company
            rb ??= rateBooks.FirstOrDefault(r =>
                r.CompanyCode == est.CompanyCode && r.IsStandardBaseline);

            if (rb != null)
                est.RateBookId = rb.RateBookId;
        }

        await db.SaveChangesAsync();
    }

    // ── Sequences ─────────────────────────────────────────────────────────────

    private static async Task SeedSequences(AppDbContext db)
    {
        if (await db.EstimateSequences.AnyAsync()) return;

        db.EstimateSequences.AddRange(
            new EstimateSequence { CompanyCode = "CSL", Year = 2026, SequenceType = "Estimate",     LastSequence = 25 },
            new EstimateSequence { CompanyCode = "CSL", Year = 2026, SequenceType = "StaffingPlan", LastSequence = 10 },
            new EstimateSequence { CompanyCode = "ETS", Year = 2026, SequenceType = "Estimate",     LastSequence = 12 },
            new EstimateSequence { CompanyCode = "ETS", Year = 2026, SequenceType = "StaffingPlan", LastSequence = 5  }
        );
        await db.SaveChangesAsync();
    }

    // ── Scheduling Demo Data ──────────────────────────────────────────────────

    private static async Task SeedSchedulingData(AppDbContext db)
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

    // ── Helpers ───────────────────────────────────────────────────────────────

    // Short aliases for empty arrays
    private static (string, string, decimal, int, int)[] No() => Array.Empty<(string, string, decimal, int, int)>();
    private static (string, string, decimal, string, int, int, bool)[] No2() => Array.Empty<(string, string, decimal, string, int, int, bool)>();

    // Single per-diem expense line
    private static (string, string, decimal, string, int, int, bool)[] Pd(string desc, decimal rate, int days, int people)
        => new[] { ("PerDiem", desc, rate, "Day", days, people, true) };

    private static Estimate E(
        string co, string number, string name,
        string client, string clientCode, string? msa,
        string jobType, string city, string state, string? site,
        int days, DateTime start, DateTime end,
        string status, decimal confidence, string shift, int totalHc,
        string vp, string director, string region, string createdBy,
        int createdDaysAgo, int updatedDaysAgo,
        (string pos, string type, string cc, decimal st, decimal ot, decimal dt, int hc, int sort)[] labor,
        (string name, string rateType, decimal rate, int qty, int days)[] equip,
        (string cat, string desc, decimal rate, string unit, int daysOrQty, int people, bool billable)[] exp,
        decimal costPct,
        string? lostReason = null)
    {
        var laborRows = labor.Select(r =>
        {
            var stH = days * 8m * r.hc;
            var otH = days * 2m * r.hc;
            return new LaborRow
            {
                Position = r.pos, LaborType = r.type, CraftCode = r.cc, NavCode = r.cc,
                Shift = shift == "Both" ? "Day" : shift,
                BillStRate = r.st, BillOtRate = r.ot, BillDtRate = r.dt,
                ScheduleJson = MakeScheduleJson(start, days, r.hc),
                StHours = stH, OtHours = otH, DtHours = 0,
                Subtotal = stH * r.st + otH * r.ot,
                SortOrder = r.sort
            };
        }).ToList();

        var equipRows = equip.Select((r, i) =>
        {
            var weeks = (int)Math.Ceiling(r.days / 7.0);
            var months = (int)Math.Ceiling(r.days / 30.0);
            var sub = r.rateType switch { "Weekly" => r.rate * weeks * r.qty, "Monthly" => r.rate * months * r.qty, _ => r.rate * r.days * r.qty };
            return new EquipmentRow { Name = r.name, RateType = r.rateType, Rate = r.rate, Qty = r.qty, Days = r.days, Subtotal = sub, SortOrder = i + 1 };
        }).ToList();

        var expRows = exp.Select((r, i) => new ExpenseRow
        {
            Category = r.cat, Description = r.desc, Rate = r.rate, Unit = r.unit,
            DaysOrQty = r.daysOrQty, People = r.people, Billable = r.billable,
            Subtotal = r.rate * r.daysOrQty * r.people, SortOrder = i + 1
        }).ToList();

        var billSub = laborRows.Sum(x => x.Subtotal) + equipRows.Sum(x => x.Subtotal) + expRows.Sum(x => x.Subtotal);
        var internalCost = Math.Round(billSub * costPct, 2);
        var profit = Math.Round(billSub - internalCost, 2);
        var margin = billSub > 0 ? Math.Round(profit / billSub * 100, 4) : 0m;

        return new Estimate
        {
            CompanyCode = co, EstimateNumber = number, Name = name,
            Client = client, ClientCode = clientCode, MsaNumber = msa,
            JobType = jobType, Branch = "Industrial", City = city, State = state,
            Site = site, Shift = shift, HoursPerShift = shift == "Both" ? 12 : 10,
            VP = vp, Director = director, Region = region,
            Days = days, StartDate = start, EndDate = end,
            OtMethod = "daily8_weekly40", DtWeekends = shift == "Both" ? "sat_sun" : "none",
            Status = status, ConfidencePct = confidence, IsScenario = false,
            LostReason = lostReason,
            CreatedBy = createdBy, UpdatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(createdDaysAgo),
            UpdatedAt = DateTimeOffset.UtcNow.AddDays(updatedDaysAgo),
            LaborRows = laborRows, EquipmentRows = equipRows, ExpenseRows = expRows,
            Summary = new EstimateSummary
            {
                BillSubtotal = billSub, DiscountType = "None",
                GrandTotal = billSub, InternalCostTotal = internalCost,
                GrossProfit = profit, GrossMarginPct = margin
            }
        };
    }

    private static List<StaffingLaborRow> SpLaborRows(DateTime start, int days,
        (string pos, string type, string cc, decimal st, decimal ot, decimal dt, int hc)[] rows)
        => rows.Select((r, i) =>
        {
            var stH = days * 8m * r.hc;
            var otH = days * 2m * r.hc;
            return new StaffingLaborRow
            {
                Position = r.pos, LaborType = r.type, CraftCode = r.cc, NavCode = r.cc, Shift = "Day",
                StRate = r.st, OtRate = r.ot, DtRate = r.dt,
                ScheduleJson = MakeScheduleJson(start, days, r.hc),
                StHours = stH, OtHours = otH, DtHours = 0,
                Subtotal = stH * r.st + otH * r.ot, SortOrder = i + 1
            };
        }).ToList();

    private static string MakeScheduleJson(DateTime start, int days, int headcount)
    {
        var dict = new Dictionary<string, int>();
        for (int i = 0; i < days; i++)
        {
            int hc;
            if (days >= 14)
            {
                hc = (i == 0 || i == days - 1) ? (int)Math.Max(1, Math.Round(headcount * 0.5)) :
                     (i == 1 || i == days - 2) ? (int)Math.Max(1, Math.Round(headcount * 0.75)) :
                     headcount;
            }
            else
            {
                hc = headcount;
            }
            dict[start.AddDays(i).ToString("yyyy-MM-dd")] = hc;
        }
        return System.Text.Json.JsonSerializer.Serialize(dict);
    }
}
