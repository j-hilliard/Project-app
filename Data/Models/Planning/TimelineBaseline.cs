namespace Stronghold.EnterpriseEstimating.Data.Models.Planning;

public class TimelineBaseline
{
    public int BaselineId { get; set; }

    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    // Project | Phase | Task
    public string EntityType { get; set; } = "Project";
    public int EntityId { get; set; }

    public DateTime SnapshotDate { get; set; }
    public DateTime? PlannedStart { get; set; }
    public DateTime? PlannedEnd { get; set; }
    public int? DurationDays { get; set; }
    public string? BaselineReason { get; set; }
    public string? BaselineLabel { get; set; }
    public string LockedBy { get; set; } = string.Empty;
    public DateTimeOffset LockedAt { get; set; } = DateTimeOffset.UtcNow;
}
