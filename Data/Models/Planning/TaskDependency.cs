namespace Stronghold.EnterpriseEstimating.Data.Models.Planning;

public class TaskDependency
{
    public int DependencyId { get; set; }
    public int SuccessorTaskId { get; set; }
    public PlanTask SuccessorTask { get; set; } = null!;

    public int PredecessorTaskId { get; set; }
    public PlanTask PredecessorTask { get; set; } = null!;

    // FinishToStart | StartToStart | FinishToFinish | StartToFinish
    public string DependencyType { get; set; } = "FinishToStart";
    public int LagDays { get; set; }
}
