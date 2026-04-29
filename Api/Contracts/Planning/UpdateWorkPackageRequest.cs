namespace Stronghold.EnterpriseEstimating.Api.Contracts.Planning;

public record UpdateWorkPackageRequest(
    string Title,
    string? CraftCode,
    int RequiredPeople,
    DateTime? PlannedStart,
    DateTime? PlannedEnd,
    string Status,
    bool ReadyForScheduling,
    string? Area,
    string? Location,
    bool PermitRequired,
    string? PermitNumber,
    string? PermitStatus,
    bool JsaRequired,
    string? JsaStatus,
    string? Notes);
