namespace Stronghold.EnterpriseEstimating.Api.Contracts.Planning;

public record CreateStepOutPlanRequest(
    string Name,
    string? Client,
    string? Site,
    DateTime? PlannedStart,
    DateTime? PlannedEnd,
    string? Notes,
    int? WorkOrderId,
    string Status = "Draft");
