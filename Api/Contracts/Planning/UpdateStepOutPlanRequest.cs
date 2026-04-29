namespace Stronghold.EnterpriseEstimating.Api.Contracts.Planning;

public record UpdateStepOutPlanRequest(
    string Name,
    string Status,
    string? Client,
    string? Site,
    DateTime? PlannedStart,
    DateTime? PlannedEnd,
    string? Notes);
