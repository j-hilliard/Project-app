using Microsoft.EntityFrameworkCore;
using Stronghold.EnterpriseEstimating.Data;

namespace Stronghold.EnterpriseEstimating.Api.Services;

public record ConflictDto(
    string Type,        // "DoubleBooking" | "Unavailable" | "CertificationMissing"
    string Message
);

public class AssignmentConflictService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public AssignmentConflictService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    /// <summary>
    /// Returns any scheduling conflicts for a proposed assignment.
    /// Conflicts are advisory — caller decides whether to proceed.
    /// </summary>
    public async Task<List<ConflictDto>> DetectConflictsAsync(
        int resourceId,
        DateTime start,
        DateTime end,
        string craftCode,
        string companyCode,
        int? excludeAssignmentId = null,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var conflicts = new List<ConflictDto>();

        // 1. Double-booking: overlapping active assignment for same resource
        var doubleBooked = await db.Assignments
            .Where(a =>
                a.CompanyCode == companyCode &&
                a.ResourceId == resourceId &&
                a.Status != "Cancelled" &&
                a.Start < end &&
                a.End > start &&
                (excludeAssignmentId == null || a.AssignmentId != excludeAssignmentId))
            .AnyAsync(ct);

        if (doubleBooked)
            conflicts.Add(new ConflictDto("DoubleBooking",
                "Resource already has an overlapping active assignment."));

        // 2. Unavailability: AvailabilityBlock covers the period
        var unavailable = await db.AvailabilityBlocks
            .Where(b =>
                b.ResourceId == resourceId &&
                b.Start < end &&
                b.End > start)
            .AnyAsync(ct);

        if (unavailable)
            conflicts.Add(new ConflictDto("Unavailable",
                "Resource has a marked unavailability block during this period."));

        // 3. Certification mismatch: craft requires specific cert that resource lacks or is expired
        var requiredCerts = CraftCertRequirements(craftCode);
        if (requiredCerts.Any())
        {
            var today = DateTime.UtcNow.Date;
            var heldCerts = await db.Certifications
                .Where(c => c.ResourceId == resourceId &&
                            (c.ExpirationDate == null || c.ExpirationDate > today))
                .Select(c => c.Type)
                .ToHashSetAsync(ct);

            foreach (var required in requiredCerts)
            {
                if (!heldCerts.Contains(required))
                    conflicts.Add(new ConflictDto("CertificationMissing",
                        $"Resource does not hold a valid {required} certification required for this craft."));
            }
        }

        return conflicts;
    }

    // Craft → required cert types. Extend as business rules evolve.
    private static IReadOnlyList<string> CraftCertRequirements(string craftCode) =>
        craftCode.ToUpperInvariant() switch
        {
            "CR" => new[] { "Crane-Operator" },
            _ => Array.Empty<string>()
        };
}
