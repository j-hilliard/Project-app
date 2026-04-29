namespace Stronghold.EnterpriseEstimating.Data.Models.Planning;

public class FcoDocument
{
    public int FcoDocumentId { get; set; }
    public string CompanyCode { get; set; } = string.Empty;

    public int? LinkedEstimateId { get; set; }
    public int? LinkedWorkOrderId { get; set; }
    public WorkOrder? LinkedWorkOrder { get; set; }

    public string FcoNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.UtcNow.Date;

    public string? RequestedBy { get; set; }
    public string? PreparedBy { get; set; }
    public string? ClientName { get; set; }
    public string? ClientContact { get; set; }
    public string? ContractorName { get; set; }
    public string? ContractorContact { get; set; }
    public string? ProjectName { get; set; }
    public string? ProjectAddress { get; set; }

    public string? ScopeDescription { get; set; }
    public string? Reason { get; set; }
    public int ScheduleImpactDays { get; set; } = 0;
    public DateTime? RevisedCompletionDate { get; set; }

    // JSON-serialized breakdowns for flexibility
    public string? LaborBreakdownJson { get; set; }
    public string? MaterialBreakdownJson { get; set; }
    public string? EquipmentBreakdownJson { get; set; }

    public decimal MarkupPct { get; set; } = 0;
    public decimal? TaxPct { get; set; }
    public decimal TotalFcoAmount { get; set; } = 0;
    public decimal? UpdatedContractValue { get; set; }

    // Status: Draft, Submitted, Approved, Rejected, Signed
    public string Status { get; set; } = "Draft";
    public string? ApprovalNotes { get; set; }

    // Signature placeholders (names + dates captured; actual e-sig out of scope for Phase 3)
    public string? ClientApprovalName { get; set; }
    public DateTime? ClientApprovalDate { get; set; }
    public string? ContractorApprovalName { get; set; }
    public DateTime? ContractorApprovalDate { get; set; }

    public string? RevisionHistory { get; set; }

    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
