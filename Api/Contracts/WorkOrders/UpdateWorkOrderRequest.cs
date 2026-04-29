namespace Stronghold.EnterpriseEstimating.Api.Contracts.WorkOrders;

public record UpdateWorkOrderRequest(
    string Title,
    string? Description,
    string? Scope,
    decimal AuthorizedValue,
    DateTime? PlannedStart,
    DateTime? PlannedEnd,
    DateTime? ForecastEnd);
