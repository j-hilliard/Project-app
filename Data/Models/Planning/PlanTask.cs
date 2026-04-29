namespace Stronghold.EnterpriseEstimating.Data.Models.Planning;

public class PlanTask
{
    public int TaskId { get; set; }
    public int PhaseId { get; set; }
    public ProjectPhase Phase { get; set; } = null!;

    public int? ParentTaskId { get; set; }
    public PlanTask? ParentTask { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Task | Milestone | Gate
    public string TaskType { get; set; } = "Task";

    public int? LinkedEstimateId { get; set; }
    public int? LinkedFcoDocumentId { get; set; }
    public int? LinkedStepOutPlanId { get; set; }

    public DateTime? PlannedStart { get; set; }
    public DateTime? PlannedEnd { get; set; }
    public int DurationDays { get; set; }
    public DateTime? BaselineStart { get; set; }
    public DateTime? BaselineEnd { get; set; }
    public int? BaselineDuration { get; set; }
    public DateTime? ActualStart { get; set; }
    public DateTime? ActualEnd { get; set; }
    public DateTime? ForecastEnd { get; set; }

    public decimal PercentComplete { get; set; }
    public string Status { get; set; } = "Pending";
    public string? CraftCode { get; set; }
    public string? AssignedTo { get; set; }
    public string? OwnerUserId { get; set; }
    public int SortOrder { get; set; }
    public bool IsMilestone { get; set; }

    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<PlanTask> SubTasks { get; set; } = new List<PlanTask>();
    public ICollection<Milestone> Milestones { get; set; } = new List<Milestone>();
    public ICollection<TaskDependency> SuccessorDependencies { get; set; } = new List<TaskDependency>();
    public ICollection<TaskDependency> PredecessorDependencies { get; set; } = new List<TaskDependency>();
}
