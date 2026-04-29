namespace Stronghold.EnterpriseEstimating.Api.Contracts.Scheduling;

public record CreateAvailabilityBlockRequest(
    int ResourceId,
    DateTime Start,
    DateTime End,
    string? Reason);
