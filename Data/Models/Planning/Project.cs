namespace Stronghold.EnterpriseEstimating.Data.Models.Planning;

public class Project
{
    public int ProjectId { get; set; }
    public string CompanyCode { get; set; } = string.Empty;
    public string ProjectNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public int EstimateId { get; set; }
    public int? CommercialAuthorizationId { get; set; }
    public CommercialAuthorization? CommercialAuthorization { get; set; }

    public string? Client { get; set; }
    public string? ClientCode { get; set; }
    public string? Site { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? JobLetter { get; set; }

    public DateTime? PlannedStart { get; set; }
    public DateTime? PlannedEnd { get; set; }
    public DateTime? BaselineStart { get; set; }
    public DateTime? BaselineEnd { get; set; }
    public DateTime? ActualStart { get; set; }
    public DateTime? ActualEnd { get; set; }
    public DateTime? ForecastEnd { get; set; }

    // Initiating | Planning | Active | Monitoring | Closing | Closed | OnHold | Cancelled
    public string Status { get; set; } = "Initiating";

    public int AtRiskThresholdDays { get; set; } = 5;
    public string? OwnerUserId { get; set; }
    public string? LessonsLearnedNotes { get; set; }

    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();
    public ICollection<ProjectPhase> Phases { get; set; } = new List<ProjectPhase>();
    public ICollection<Milestone> Milestones { get; set; } = new List<Milestone>();
}
