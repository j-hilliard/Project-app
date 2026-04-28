namespace Stronghold.EnterpriseEstimating.Data.Models.Scheduling;

public class AvailabilityBlock
{
    public int BlockId { get; set; }
    public int ResourceId { get; set; }
    public Resource Resource { get; set; } = null!;

    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string? Reason { get; set; }
}
