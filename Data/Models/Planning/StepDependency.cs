namespace Stronghold.EnterpriseEstimating.Data.Models.Planning;

public class StepDependency
{
    public int DependencyId { get; set; }
    public int StepId { get; set; }
    public StepOutStep Step { get; set; } = null!;

    public int PredecessorStepId { get; set; }
    public StepOutStep PredecessorStep { get; set; } = null!;
}
