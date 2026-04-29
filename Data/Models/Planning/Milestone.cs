namespace Stronghold.EnterpriseEstimating.Data.Models.Planning;

public class Milestone
{
    public int MilestoneId { get; set; }
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public int? PhaseId { get; set; }
    public ProjectPhase? Phase { get; set; }

    public int? TaskId { get; set; }
    public PlanTask? Task { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public DateTime PlannedDate { get; set; }
    public DateTime? BaselineDate { get; set; }
    public DateTime? ActualDate { get; set; }

    // Pending | Achieved | Missed | Cancelled
    public string Status { get; set; } = "Pending";
    public bool IsDeadline { get; set; }
    public bool IsCritical { get; set; }
    public string? Color { get; set; }

    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
