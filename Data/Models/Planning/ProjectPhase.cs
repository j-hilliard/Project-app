namespace Stronghold.EnterpriseEstimating.Data.Models.Planning;

public class ProjectPhase
{
    public int PhaseId { get; set; }
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public DateTime? PlannedStart { get; set; }
    public DateTime? PlannedEnd { get; set; }
    public DateTime? BaselineStart { get; set; }
    public DateTime? BaselineEnd { get; set; }
    public DateTime? ActualStart { get; set; }
    public DateTime? ActualEnd { get; set; }
    public DateTime? ForecastEnd { get; set; }

    public int SortOrder { get; set; }

    // Planning | Active | Complete | OnHold | Cancelled
    public string Status { get; set; } = "Planning";
    public string? Color { get; set; }

    public ICollection<PlanTask> Tasks { get; set; } = new List<PlanTask>();
    public ICollection<Milestone> Milestones { get; set; } = new List<Milestone>();
}
