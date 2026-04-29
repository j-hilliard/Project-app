namespace Stronghold.EnterpriseEstimating.Data.Models.Planning;

public class WorkOrder
{
    public int WorkOrderId { get; set; }
    public string CompanyCode { get; set; } = string.Empty;
    public string WorkOrderNumber { get; set; } = string.Empty;

    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public int EstimateId { get; set; }

    public int CommercialAuthorizationId { get; set; }
    public CommercialAuthorization CommercialAuthorization { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Scope { get; set; }
    public decimal AuthorizedValue { get; set; }

    public DateTime? PlannedStart { get; set; }
    public DateTime? PlannedEnd { get; set; }
    public DateTime? ActualStart { get; set; }
    public DateTime? ActualEnd { get; set; }
    public DateTime? ForecastEnd { get; set; }

    // Draft | Released | InProgress | Complete | Closed | Cancelled
    public string Status { get; set; } = "Draft";

    public string? ReleasedBy { get; set; }
    public DateTimeOffset? ReleasedAt { get; set; }

    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<StepOutPlan> StepOutPlans { get; set; } = new List<StepOutPlan>();
    public ICollection<WorkPackage> WorkPackages { get; set; } = new List<WorkPackage>();
}
