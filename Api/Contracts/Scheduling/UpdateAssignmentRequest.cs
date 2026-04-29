namespace Stronghold.EnterpriseEstimating.Api.Contracts.Scheduling;

public record UpdateAssignmentRequest(
    int ResourceId,
    string JobSourceType,
    int JobSourceId,
    string? JobName,
    string CraftCode,
    DateTime Start,
    DateTime End,
    string Shift,
    string Status);
