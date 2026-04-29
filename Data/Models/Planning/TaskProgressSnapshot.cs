namespace Stronghold.EnterpriseEstimating.Data.Models.Planning;

public class TaskProgressSnapshot
{
    public int SnapshotId { get; set; }

    public int TaskId { get; set; }
    public PlanTask Task { get; set; } = null!;

    public DateTime SnapshotDate { get; set; }
    public decimal PercentComplete { get; set; }
    public DateTime? ForecastEnd { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ReportedBy { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
