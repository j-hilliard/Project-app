namespace Stronghold.EnterpriseEstimating.Data.Models.Scheduling;

public class Resource
{
    public int ResourceId { get; set; }
    public string CompanyCode { get; set; } = string.Empty;

    // Identity
    public string? EmployeeId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    // Legacy display name — kept for backward compat; computed from First+Last on create/update
    public string Name { get; set; } = string.Empty;

    // Craft + Location
    public string CraftCode { get; set; } = string.Empty;
    public string? Region { get; set; }
    public string? Branch { get; set; }

    // Status
    // Active | Inactive | OnLeave | Terminated
    public string EmploymentStatus { get; set; } = "Active";
    public bool IsActive { get; set; } = true;

    // Scheduling preferences
    // Day | Night | Rotating | Any
    public string ShiftEligibility { get; set; } = "Any";

    // Contact
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Notes { get; set; }

    public Craft? Craft { get; set; }
    public ICollection<Certification> Certifications { get; set; } = new List<Certification>();
    public ICollection<AvailabilityBlock> AvailabilityBlocks { get; set; } = new List<AvailabilityBlock>();
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
}
