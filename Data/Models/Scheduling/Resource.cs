namespace Stronghold.EnterpriseEstimating.Data.Models.Scheduling;

public class Resource
{
    public int ResourceId { get; set; }
    public string CompanyCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CraftCode { get; set; } = string.Empty;
    public string? Branch { get; set; }
    public bool IsActive { get; set; } = true;

    public Craft? Craft { get; set; }
    public ICollection<Certification> Certifications { get; set; } = new List<Certification>();
    public ICollection<AvailabilityBlock> AvailabilityBlocks { get; set; } = new List<AvailabilityBlock>();
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
}
