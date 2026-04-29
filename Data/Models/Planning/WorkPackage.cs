namespace Stronghold.EnterpriseEstimating.Data.Models.Planning;

public class WorkPackage
{
    public int PackageId { get; set; }
    public string CompanyCode { get; set; } = string.Empty;

    public int? PlanId { get; set; }
    public StepOutPlan? Plan { get; set; }

    public int? WorkOrderId { get; set; }
    public WorkOrder? WorkOrder { get; set; }

    // Traceability back to the source (estimate, staffing plan, FCO, or step-out plan)
    public string? SourceType { get; set; }
    public int? SourceId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? CraftCode { get; set; }
    public int RequiredPeople { get; set; } = 1;

    public DateTime? PlannedStart { get; set; }
    public DateTime? PlannedEnd { get; set; }

    // Status: Draft, ReadyForScheduling, Scheduled, InProgress, Complete
    public string Status { get; set; } = "Draft";
    public bool ReadyForScheduling { get; set; } = false;

    // Location context
    public string? Area { get; set; }
    public string? Location { get; set; }

    // Permit / JSA readiness (field start gate)
    public bool PermitRequired { get; set; } = false;
    public string? PermitNumber { get; set; }
    // PermitStatus: NotRequired, Pending, Issued, Expired
    public string? PermitStatus { get; set; }
    public bool JsaRequired { get; set; } = false;
    // JsaStatus: NotRequired, Pending, Approved, Expired
    public string? JsaStatus { get; set; }

    public string? Notes { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
