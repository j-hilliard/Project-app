namespace Stronghold.EnterpriseEstimating.Api.Contracts.Planning;

public record CreateFcoDocumentRequest(
    string FcoNumber,
    string Title,
    string? ScopeDescription,
    string? Reason,
    string? RequestedBy,
    string? PreparedBy,
    int ScheduleImpactDays,
    decimal? UpdatedContractValue,
    DateTime? Date,
    int? LinkedWorkOrderId);
