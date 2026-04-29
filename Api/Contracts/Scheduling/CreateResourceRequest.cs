namespace Stronghold.EnterpriseEstimating.Api.Contracts.Scheduling;

public record CreateResourceRequest(
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
