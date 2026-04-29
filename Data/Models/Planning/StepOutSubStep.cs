namespace Stronghold.EnterpriseEstimating.Data.Models.Planning;

public class StepOutSubStep
{
    public int SubStepId { get; set; }
    public int StepId { get; set; }
    public StepOutStep Step { get; set; } = null!;

    public string SubStepCode { get; set; } = string.Empty;
    public decimal SortOrder { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public decimal? DurationHours { get; set; }
    public int RequiredPeople { get; set; } = 1;
    public string? CraftCode { get; set; }
    public bool IsParallel { get; set; } = false;

    // Pending | InProgress | Complete | Blocked
    public string Status { get; set; } = "Pending";

    public DateTime? ActualStart { get; set; }
    public DateTime? ActualEnd { get; set; }
    public decimal? ActualDurationHours { get; set; }
    public string? Notes { get; set; }
}
