namespace Stronghold.EnterpriseEstimating.Api.Contracts.Planning;

public record UpdateFcoDocumentRequest(
    string Title,
    string? ScopeDescription,
    string? Reason,
    int ScheduleImpactDays,
    DateTime? RevisedCompletionDate,
    string? LaborBreakdownJson,
    string? MaterialBreakdownJson,
    string? EquipmentBreakdownJson,
    decimal MarkupPct,
    decimal? TaxPct,
    decimal TotalFcoAmount,
    decimal? UpdatedContractValue,
    string Status,
    string? ApprovalNotes,
    string? ClientApprovalName,
    DateTime? ClientApprovalDate,
    string? ContractorApprovalName,
    DateTime? ContractorApprovalDate,
    string? RevisionHistory);
