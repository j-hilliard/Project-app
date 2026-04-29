namespace Stronghold.EnterpriseEstimating.Data.Models.Planning;

public class EstimateTaskLink
{
    public int LinkId { get; set; }

    public int TaskId { get; set; }
    public PlanTask Task { get; set; } = null!;

    public int EstimateId { get; set; }

    // Primary | Supporting | Reference
    public string LinkType { get; set; } = "Primary";

    public string? Notes { get; set; }
    public bool IsArchived { get; set; } = false;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
