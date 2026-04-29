namespace Stronghold.EnterpriseEstimating.Data.Models.Planning;

public class FcoTaskLink
{
    public int LinkId { get; set; }

    public int TaskId { get; set; }
    public PlanTask Task { get; set; } = null!;

    public int FcoDocumentId { get; set; }
    public FcoDocument FcoDocument { get; set; } = null!;

    // ScopeAddition | ScheduleImpact | Reference
    public string LinkType { get; set; } = "ScopeAddition";

    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
