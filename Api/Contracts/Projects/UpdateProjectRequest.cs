namespace Stronghold.EnterpriseEstimating.Api.Contracts.Projects;

public record UpdateProjectRequest(
    string Name,
    string? Client,
    string? ClientCode,
    string? Site,
    string? City,
    string? State,
    string? JobLetter,
    DateTime? PlannedStart,
    DateTime? PlannedEnd,
    DateTime? ForecastEnd,
    int AtRiskThresholdDays,
    string? OwnerUserId,
    string? LessonsLearnedNotes);
