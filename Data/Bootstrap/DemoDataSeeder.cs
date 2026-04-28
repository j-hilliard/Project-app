using Microsoft.EntityFrameworkCore;
using Stronghold.EnterpriseEstimating.Data.Models;

namespace Stronghold.EnterpriseEstimating.Data.Bootstrap;

public class DemoDataSeeder
{
    public async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        await SeedDefaultUsersAsync(db, ct);
    }

    private static async Task SeedDefaultUsersAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.Users.AnyAsync(ct)) return;

        var adminRole     = db.Roles.First(r => r.Name == "Administrator");
        var estimatorRole = db.Roles.First(r => r.Name == "Estimator");
        var analyticsRole = db.Roles.First(r => r.Name == "Analytics");

        var devUser = new User
        {
            Username     = "dev.user",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("StrongholdDev2024"),
            CompanyCode  = "CSL",
            FirstName    = "Dev",
            LastName     = "User",
            Email        = "dev.user@stronghold.local",
            Active       = true,
        };

        var estimatorCsl = new User
        {
            Username     = "estimator.csl",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Stronghold2024"),
            CompanyCode  = "CSL",
            FirstName    = "James",
            LastName     = "Tanner",
            Email        = "james.tanner@catspec.com",
            Active       = true,
        };

        var estimatorEts = new User
        {
            Username     = "estimator.ets",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Stronghold2024"),
            CompanyCode  = "ETS",
            FirstName    = "Maria",
            LastName     = "Delgado",
            Email        = "maria.delgado@eliteta.com",
            Active       = true,
        };

        var executive = new User
        {
            Username     = "executive",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Stronghold2024"),
            CompanyCode  = "CSL",
            FirstName    = "Robert",
            LastName     = "Callahan",
            Email        = "r.callahan@thestrongholdcompanies.com",
            Active       = true,
        };

        db.Users.AddRange(devUser, estimatorCsl, estimatorEts, executive);
        await db.SaveChangesAsync(ct);

        db.UserRoles.AddRange(
            new UserRole { UserId = devUser.UserId,      RoleId = adminRole.RoleId },
            new UserRole { UserId = estimatorCsl.UserId, RoleId = estimatorRole.RoleId },
            new UserRole { UserId = estimatorEts.UserId, RoleId = estimatorRole.RoleId },
            new UserRole { UserId = executive.UserId,    RoleId = analyticsRole.RoleId }
        );
        await db.SaveChangesAsync(ct);

        db.UserCompanies.AddRange(
            new UserCompany { UserId = devUser.UserId,      CompanyCode = "CSL" },
            new UserCompany { UserId = devUser.UserId,      CompanyCode = "ETS" },
            new UserCompany { UserId = estimatorCsl.UserId, CompanyCode = "CSL" },
            new UserCompany { UserId = estimatorEts.UserId, CompanyCode = "ETS" },
            new UserCompany { UserId = executive.UserId,    CompanyCode = "CSL" }
        );
        await db.SaveChangesAsync(ct);
    }
}
