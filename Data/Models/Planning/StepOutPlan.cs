namespace Stronghold.EnterpriseEstimating.Data.Models.Planning;

public class StepOutPlan
{
    public int PlanId { get; set; }
    public string CompanyCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    // Source link — which estimate/staffing plan/FCO this plan executes
    public string? SourceType { get; set; }  // "Estimate", "StaffingPlan", "FcoDocument"
    public int? LinkedEstimateId { get; set; }
    public int? LinkedStaffingPlanId { get; set; }
    public int? LinkedFcoDocumentId { get; set; }

    public string? Client { get; set; }
    public string? Site { get; set; }
    public DateTime? PlannedStart { get; set; }
    public DateTime? PlannedEnd { get; set; }

    // Status: Draft, Active, Complete, Archived
    public string Status { get; set; } = "Draft";
    public string? Notes { get; set; }

    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<StepOutStep> Steps { get; set; } = new List<StepOutStep>();
    public ICollection<WorkPackage> WorkPackages { get; set; } = new List<WorkPackage>();
}
