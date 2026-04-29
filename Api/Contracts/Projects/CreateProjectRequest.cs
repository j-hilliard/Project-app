namespace Stronghold.EnterpriseEstimating.Api.Contracts.Projects;

public record CreateProjectRequest(
    int EstimateId,
    int? CommercialAuthorizationId,
    string ProjectNumber,
    string Name,
    string? Client,
    string? ClientCode,
    string? Site,
    string? City,
    string? State,
    string? JobLetter,
    DateTime? PlannedStart,
    DateTime? PlannedEnd);
