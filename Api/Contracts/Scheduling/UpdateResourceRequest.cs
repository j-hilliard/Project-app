namespace Stronghold.EnterpriseEstimating.Api.Contracts.Scheduling;

public record UpdateResourceRequest(
    string FirstName,
    string LastName,
    string? EmployeeId,
    string CraftCode,
    string? Region,
    string? Branch,
    string EmploymentStatus,
    bool IsActive,
    string ShiftEligibility,
    string? Phone,
    string? Email,
    string? Notes);
