namespace Stronghold.EnterpriseEstimating.Data.Models.Scheduling;

public class Certification
{
    public int CertId { get; set; }
    public int ResourceId { get; set; }
    public Resource Resource { get; set; } = null!;

    // e.g. OSHA-10, OSHA-30, H2S, CPR, Rigging, Crane-Operator
    public string Type { get; set; } = string.Empty;
    public DateTime? ExpirationDate { get; set; }
}
