namespace Stronghold.EnterpriseEstimating.Api.Contracts.Planning;

public record CreateStepOutStepRequest(
    string StepCode,
    decimal SortOrder,
    string Title,
    string? Description,
    string? CraftCode,
    int RequiredPeople,
    int? DurationMinutes,
    bool IsParallel,
    bool PermitRequired,
    bool MaterialToolRequired,
    string? Area,
    string Status,
    DateTime? PlannedStart,
    DateTime? PlannedEnd,
    string? Notes);
