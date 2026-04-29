namespace Stronghold.EnterpriseEstimating.Api.Contracts.Scheduling;

public record CreateCertificationRequest(
    int ResourceId,
    string Type,
    DateTime? ExpirationDate);
