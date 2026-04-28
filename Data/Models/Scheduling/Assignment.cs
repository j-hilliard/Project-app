namespace Stronghold.EnterpriseEstimating.Data.Models.Scheduling;

public class Assignment
{
    public int AssignmentId { get; set; }
    public string CompanyCode { get; set; } = string.Empty;

    public int ResourceId { get; set; }
    public Resource Resource { get; set; } = null!;

    // Source job reference (Estimate, StaffingPlan, WorkPackage)
    public string JobSourceType { get; set; } = string.Empty;
    public int JobSourceId { get; set; }
    public string? JobName { get; set; }

    public string CraftCode { get; set; } = string.Empty;
    public DateTime Start { get; set; }
    public DateTime End { get; set; }

    // Shift: Day, Night, Rotation
    public string Shift { get; set; } = "Day";

    // Status: Planned, Confirmed, Cancelled
    public string Status { get; set; } = "Planned";

    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
