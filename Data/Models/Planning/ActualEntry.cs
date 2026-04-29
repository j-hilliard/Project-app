namespace Stronghold.EnterpriseEstimating.Data.Models.Planning;

public class ActualEntry
{
    public int ActualEntryId { get; set; }
    public string CompanyCode { get; set; } = string.Empty;

    public int WorkOrderId { get; set; }
    public WorkOrder WorkOrder { get; set; } = null!;

    public int? FcoDocumentId { get; set; }
    public FcoDocument? FcoDocument { get; set; }

    public int? PlanTaskId { get; set; }
    public PlanTask? PlanTask { get; set; }

    public int? StepOutStepId { get; set; }
    public StepOutStep? StepOutStep { get; set; }

    public int? StepOutSubStepId { get; set; }
    public StepOutSubStep? StepOutSubStep { get; set; }

    // Labor | Equipment | Material | Subcontract | Other
    public string ActualType { get; set; } = "Labor";

    public DateTime ActualDate { get; set; }
    public string? Description { get; set; }
    public string? Position { get; set; }
    public string? CraftCode { get; set; }

    public decimal StHours { get; set; }
    public decimal OtHours { get; set; }
    public decimal DtHours { get; set; }

    public decimal CostAmount { get; set; }
    public decimal BillableAmount { get; set; }
    public decimal BilledAmount { get; set; }

    public bool IsConfirmed { get; set; } = false;
    public string EnteredBy { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
