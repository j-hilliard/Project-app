namespace Stronghold.EnterpriseEstimating.Api.Contracts.Scheduling;

public record CreateAssignmentRequest(
    int ResourceId,
    string JobSourceType,
    int JobSourceId,
    string? JobName,
    string CraftCode,
    DateTime Start,
    DateTime End,
    string Shift,
    string Status);
