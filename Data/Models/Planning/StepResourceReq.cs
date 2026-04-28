namespace Stronghold.EnterpriseEstimating.Data.Models.Planning;

public class StepResourceReq
{
    public int ReqId { get; set; }
    public int StepId { get; set; }
    public StepOutStep Step { get; set; } = null!;

    public string CraftCode { get; set; } = string.Empty;
    public int RequiredCount { get; set; } = 1;
}
