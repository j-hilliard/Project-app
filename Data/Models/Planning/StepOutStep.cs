namespace Stronghold.EnterpriseEstimating.Data.Models.Planning;

public class StepOutStep
{
    public int StepId { get; set; }
    public int PlanId { get; set; }
    public StepOutPlan Plan { get; set; } = null!;

    // Decimal-style step numbering: "1", "1.2", "1.5", "2" — supports inserting steps between others
    public string StepCode { get; set; } = string.Empty;
    public decimal SortOrder { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public int? ParentStepId { get; set; }
    public StepOutStep? ParentStep { get; set; }

    public int? DurationMinutes { get; set; }
    public decimal? DurationHours { get; set; }
    public int RequiredPeople { get; set; } = 1;
    public string? CraftCode { get; set; }

    public bool IsParallel { get; set; } = false;
    public bool PermitRequired { get; set; } = false;
    public bool MaterialToolRequired { get; set; } = false;

    public string? Area { get; set; }

    // Status: Pending, InProgress, Complete, Blocked
    public string Status { get; set; } = "Pending";

    public DateTime? PlannedStart { get; set; }
    public DateTime? PlannedEnd { get; set; }
    public string? Notes { get; set; }

    public ICollection<StepOutStep> SubSteps { get; set; } = new List<StepOutStep>();
    public ICollection<StepOutSubStep> SubStepLeafs { get; set; } = new List<StepOutSubStep>();
    public ICollection<StepDependency> Dependencies { get; set; } = new List<StepDependency>();
    public ICollection<StepResourceReq> ResourceRequirements { get; set; } = new List<StepResourceReq>();
}
