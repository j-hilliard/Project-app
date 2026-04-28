namespace Stronghold.EnterpriseEstimating.Data.Models.Scheduling;

public class Craft
{
    public string CraftCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool IsDirect { get; set; } = true;

    public ICollection<Resource> Resources { get; set; } = new List<Resource>();
}
