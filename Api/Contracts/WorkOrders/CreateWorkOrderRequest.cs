namespace Stronghold.EnterpriseEstimating.Api.Contracts.WorkOrders;

public record CreateWorkOrderRequest(
    int ProjectId,
    int CommercialAuthorizationId,
    int EstimateId,
    string WorkOrderNumber,
    string Title,
    string? Description,
    string? Scope,
    decimal AuthorizedValue,
    DateTime? PlannedStart,
    DateTime? PlannedEnd);
