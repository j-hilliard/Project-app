using Microsoft.EntityFrameworkCore;
using Stronghold.EnterpriseEstimating.Data.Models;
using Stronghold.EnterpriseEstimating.Data.Models.Planning;
using Stronghold.EnterpriseEstimating.Data.Models.Scheduling;

namespace Stronghold.EnterpriseEstimating.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<Company> Companies { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<UserCompany> UserCompanies { get; set; } = null!;
    public DbSet<UserProfileSettings> Profiles { get; set; } = null!;
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<UserRole> UserRoles { get; set; } = null!;
    public DbSet<Permission> Permissions { get; set; } = null!;
    public DbSet<RolePermission> RolePermissions { get; set; } = null!;
    public DbSet<Settings> Settings { get; set; } = null!;

    // Estimating
    public DbSet<Estimate> Estimates { get; set; } = null!;
    public DbSet<EstimateRevision> EstimateRevisions { get; set; } = null!;
    public DbSet<EstimateSummary> EstimateSummaries { get; set; } = null!;
    public DbSet<LaborRow> LaborRows { get; set; } = null!;
    public DbSet<EquipmentRow> EquipmentRows { get; set; } = null!;
    public DbSet<ExpenseRow> ExpenseRows { get; set; } = null!;
    public DbSet<FcoEntry> FcoEntries { get; set; } = null!;
    public DbSet<EstimateSequence> EstimateSequences { get; set; } = null!;

    // Staffing Plans
    public DbSet<StaffingPlan> StaffingPlans { get; set; } = null!;
    public DbSet<StaffingLaborRow> StaffingLaborRows { get; set; } = null!;

    // Rate Books
    public DbSet<RateBook> RateBooks { get; set; } = null!;
    public DbSet<RateBookLaborRate> RateBookLaborRates { get; set; } = null!;
    public DbSet<RateBookEquipmentRate> RateBookEquipmentRates { get; set; } = null!;
    public DbSet<RateBookExpenseItem> RateBookExpenseItems { get; set; } = null!;
    public DbSet<CrewTemplate> CrewTemplates { get; set; } = null!;
    public DbSet<CrewTemplateRow> CrewTemplateRows { get; set; } = null!;

    // Planning
    public DbSet<CommercialAuthorization> CommercialAuthorizations { get; set; } = null!;
    public DbSet<Project> Projects { get; set; } = null!;
    public DbSet<WorkOrder> WorkOrders { get; set; } = null!;
    public DbSet<ProjectPhase> ProjectPhases { get; set; } = null!;
    public DbSet<PlanTask> PlanTasks { get; set; } = null!;
    public DbSet<TaskDependency> TaskDependencies { get; set; } = null!;
    public DbSet<Milestone> Milestones { get; set; } = null!;
    public DbSet<StepOutPlan> StepOutPlans { get; set; } = null!;
    public DbSet<StepOutStep> StepOutSteps { get; set; } = null!;
    public DbSet<StepOutSubStep> StepOutSubSteps { get; set; } = null!;
    public DbSet<StepDependency> StepDependencies { get; set; } = null!;
    public DbSet<StepResourceReq> StepResourceReqs { get; set; } = null!;
    public DbSet<WorkPackage> WorkPackages { get; set; } = null!;
    public DbSet<FcoDocument> FcoDocuments { get; set; } = null!;
    public DbSet<FcoLaborLine> FcoLaborLines { get; set; } = null!;
    public DbSet<ActualEntry> ActualEntries { get; set; } = null!;
    public DbSet<EstimateTaskLink> EstimateTaskLinks { get; set; } = null!;
    public DbSet<FcoTaskLink> FcoTaskLinks { get; set; } = null!;
    public DbSet<TimelineBaseline> TimelineBaselines { get; set; } = null!;
    public DbSet<TaskProgressSnapshot> TaskProgressSnapshots { get; set; } = null!;

    // Scheduling
    public DbSet<Craft> Crafts { get; set; } = null!;
    public DbSet<Resource> Resources { get; set; } = null!;
    public DbSet<Certification> Certifications { get; set; } = null!;
    public DbSet<AvailabilityBlock> AvailabilityBlocks { get; set; } = null!;
    public DbSet<Assignment> Assignments { get; set; } = null!;

    // Cost Books
    public DbSet<CostBook> CostBooks { get; set; } = null!;
    public DbSet<CostBookLaborRate> CostBookLaborRates { get; set; } = null!;
    public DbSet<CostBookEquipmentRate> CostBookEquipmentRates { get; set; } = null!;
    public DbSet<CostBookExpense> CostBookExpenses { get; set; } = null!;
    public DbSet<CostBookOverheadItem> CostBookOverheadItems { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    private void ApplyAuditInfo()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified))
        {
            if (entry.Entity is User u)
            {
                u.ModifiedOn = now;
                if (entry.State == EntityState.Added) u.CreatedOn = now;
            }
        }
    }

    public override int SaveChanges()
    {
        ApplyAuditInfo();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInfo();
        return base.SaveChangesAsync(cancellationToken);
    }
}
