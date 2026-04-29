using Microsoft.EntityFrameworkCore;
using Stronghold.EnterpriseEstimating.Data;
using Stronghold.EnterpriseEstimating.Data.Models;

namespace Stronghold.EnterpriseEstimating.Api.Services.Dev;

public static class CoreDataSeeder
{
    public static async Task SeedUsers(AppDbContext db)
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

    public static async Task SeedUserCompanies(AppDbContext db)
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

    public static async Task SeedCostBooks(AppDbContext db)
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

    public static async Task SeedRateBooks(AppDbContext db)
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
    public static (string, string, string, decimal, decimal, decimal)[] MakeRates(decimal pf, decimal bm, decimal wd) => new[]
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
    public static (string, string, string, decimal, decimal, decimal)[] BpRates()      => MakeRates(80m, 84m, 87m);   // BP: +$2 across
    public static (string, string, string, decimal, decimal, decimal)[] ShellRates()   => MakeRates(84m, 88m, 91m);   // Shell: +$6 premium
    public static (string, string, string, decimal, decimal, decimal)[] XomRates()     => MakeRates(82m, 86m, 89m);   // ExxonMobil: +$4
    public static (string, string, string, decimal, decimal, decimal)[] ValeroRates()  => MakeRates(79m, 83m, 86m);   // Valero PA: +$1
    public static (string, string, string, decimal, decimal, decimal)[] ChevronRates() => MakeRates(86m, 90m, 93m);   // Chevron MS: +$8 out-of-state
    public static (string, string, string, decimal, decimal, decimal)[] MarathonRates()=> MakeRates(78m, 82m, 85m);   // Marathon: standard (same as baseline)

    // ETS rate books — slightly lower market, distinct per client
    public static (string, string, string, decimal, decimal, decimal)[] EtsStd()         => MakeRates(76m, 80m, 83m);   // ETS baseline
    public static (string, string, string, decimal, decimal, decimal)[] EtsValeroRates() => MakeRates(77m, 81m, 84m);   // Valero CC: +$1
    public static (string, string, string, decimal, decimal, decimal)[] EtsFlintRates()  => MakeRates(79m, 83m, 86m);   // Flint Hills: +$3
    public static (string, string, string, decimal, decimal, decimal)[] EtsChenieRates() => MakeRates(88m, 92m, 95m);   // Cheniere LNG: +$12 (hazmat premium)

    public static RateBook MakeRateBook(
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

    public static async Task SeedCrewTemplates(AppDbContext db)
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

    public static async Task SeedSequences(AppDbContext db)
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

}