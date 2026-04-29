namespace Stronghold.EnterpriseEstimating.Data.Models.Planning;

public class CommercialAuthorization
{
    public int CommercialAuthorizationId { get; set; }
    public string CompanyCode { get; set; } = string.Empty;

    public int EstimateId { get; set; }

    // PO | SignedProposal | Contract | NTP | WorkAuthorization | ReleaseOrder
    public string AuthorizationType { get; set; } = "PO";
    public string AuthorizationNumber { get; set; } = string.Empty;
    public decimal AuthorizedValue { get; set; }
    public string? AuthorizedBy { get; set; }
    public DateTime? ReceivedDate { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public DateTime? ExpirationDate { get; set; }

    // Draft | Submitted | Active | Superseded | Closed | Cancelled
    public string Status { get; set; } = "Draft";

    public string? Notes { get; set; }
    public string? DocumentReference { get; set; }

    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Project> Projects { get; set; } = new List<Project>();
    public ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();
}
