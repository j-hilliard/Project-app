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
        modelBuilder.Entity<Company>(b =>
        {
            b.HasKey(c => c.CompanyCode);
            b.Property(c => c.CompanyCode).IsRequired().HasMaxLength(10);
            b.Property(c => c.Name).IsRequired().HasMaxLength(200);
            b.Property(c => c.ShortName).IsRequired().HasMaxLength(50);
        });

        modelBuilder.Entity<UserCompany>(b =>
        {
            b.HasKey(uc => new { uc.UserId, uc.CompanyCode });
            b.HasOne(uc => uc.User)
                .WithMany(u => u.UserCompanies)
                .HasForeignKey(uc => uc.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(uc => uc.Company)
                .WithMany(c => c.UserCompanies)
                .HasForeignKey(uc => uc.CompanyCode)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<User>(b =>
        {
            b.HasKey(u => u.UserId);
            b.HasIndex(u => u.Username).IsUnique();
            b.Property(u => u.Username).IsRequired().HasMaxLength(100);
            b.Property(u => u.PasswordHash).IsRequired();
            b.Property(u => u.CompanyCode).IsRequired().HasMaxLength(10);
        });

        modelBuilder.Entity<UserProfileSettings>(b =>
        {
            b.ToTable("Profile");
            b.HasKey(p => p.ProfileId);
            b.HasIndex(p => p.UserId).IsUnique();
            b.HasOne(p => p.User)
                .WithOne(u => u.Profile)
                .HasForeignKey<UserProfileSettings>(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Role>(b =>
        {
            b.HasKey(r => r.RoleId);
            b.Property(r => r.Name).IsRequired();
            b.Property(r => r.Description).IsRequired();
        });

        modelBuilder.Entity<UserRole>(b =>
        {
            b.HasKey(ur => new { ur.UserId, ur.RoleId });
            b.HasOne(ur => ur.User).WithMany(u => u.UserRoles).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(ur => ur.Role).WithMany(r => r.UserRoles).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Permission>(b =>
        {
            b.HasKey(p => p.PermissionId);
            b.Property(p => p.Code).IsRequired();
            b.Property(p => p.Name).IsRequired();
        });

        modelBuilder.Entity<RolePermission>(b =>
        {
            b.HasKey(rp => new { rp.RoleId, rp.PermissionId });
            b.HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Settings>(b =>
        {
            b.HasKey(s => s.SettingsId);
        });

        // ── Estimates ──────────────────────────────────────────────────────
        modelBuilder.Entity<Estimate>(b =>
        {
            b.HasKey(e => e.EstimateId);
            b.HasIndex(e => new { e.CompanyCode, e.EstimateNumber }).IsUnique();
            b.Property(e => e.CompanyCode).IsRequired().HasMaxLength(10);
            b.Property(e => e.EstimateNumber).IsRequired().HasMaxLength(50);
            b.Property(e => e.Name).IsRequired().HasMaxLength(200);
            b.Property(e => e.Client).IsRequired().HasMaxLength(200);
            b.Property(e => e.Status).IsRequired().HasMaxLength(30);
            b.Property(e => e.Shift).IsRequired().HasMaxLength(10);
            b.Property(e => e.OtMethod).IsRequired().HasMaxLength(30);
            b.Property(e => e.ConfidencePct).HasPrecision(5, 2);
            b.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);

            b.HasOne(e => e.StaffingPlan)
                .WithMany(sp => sp.Estimates)
                .HasForeignKey(e => e.StaffingPlanId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<EstimateRevision>(b =>
        {
            b.HasKey(r => r.EstimateRevisionId);
            b.HasIndex(r => new { r.EstimateId, r.RevisionNumber }).IsUnique();
            b.Property(r => r.LaborTotal).HasPrecision(18, 2);
            b.Property(r => r.EquipTotal).HasPrecision(18, 2);
            b.Property(r => r.GrandTotal).HasPrecision(18, 2);
            b.HasOne(r => r.Estimate)
                .WithMany(e => e.Revisions)
                .HasForeignKey(r => r.EstimateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EstimateSummary>(b =>
        {
            b.HasKey(s => s.EstimateSummaryId);
            b.HasIndex(s => s.EstimateId).IsUnique();
            b.Property(s => s.BillSubtotal).HasPrecision(18, 2);
            b.Property(s => s.DiscountValue).HasPrecision(18, 4);
            b.Property(s => s.DiscountAmount).HasPrecision(18, 2);
            b.Property(s => s.TaxRate).HasPrecision(5, 4);
            b.Property(s => s.TaxAmount).HasPrecision(18, 2);
            b.Property(s => s.GrandTotal).HasPrecision(18, 2);
            b.Property(s => s.InternalCostTotal).HasPrecision(18, 2);
            b.Property(s => s.GrossProfit).HasPrecision(18, 2);
            b.Property(s => s.GrossMarginPct).HasPrecision(18, 4);
            b.HasOne(s => s.Estimate)
                .WithOne(e => e.Summary)
                .HasForeignKey<EstimateSummary>(s => s.EstimateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LaborRow>(b =>
        {
            b.HasKey(r => r.LaborRowId);
            b.Property(r => r.BillStRate).HasPrecision(18, 4);
            b.Property(r => r.BillOtRate).HasPrecision(18, 4);
            b.Property(r => r.BillDtRate).HasPrecision(18, 4);
            b.Property(r => r.StHours).HasPrecision(10, 2);
            b.Property(r => r.OtHours).HasPrecision(10, 2);
            b.Property(r => r.DtHours).HasPrecision(10, 2);
            b.Property(r => r.Subtotal).HasPrecision(18, 2);
            b.HasOne(r => r.Estimate)
                .WithMany(e => e.LaborRows)
                .HasForeignKey(r => r.EstimateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EquipmentRow>(b =>
        {
            b.HasKey(r => r.EquipmentRowId);
            b.Property(r => r.Rate).HasPrecision(18, 4);
            b.Property(r => r.Subtotal).HasPrecision(18, 2);
            b.HasOne(r => r.Estimate)
                .WithMany(e => e.EquipmentRows)
                .HasForeignKey(r => r.EstimateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExpenseRow>(b =>
        {
            b.HasKey(r => r.ExpenseRowId);
            b.Property(r => r.Rate).HasPrecision(18, 4);
            b.Property(r => r.Subtotal).HasPrecision(18, 2);
            b.HasOne(r => r.Estimate)
                .WithMany(e => e.ExpenseRows)
                .HasForeignKey(r => r.EstimateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FcoEntry>(b =>
        {
            b.HasKey(f => f.FcoEntryId);
            b.Property(f => f.DollarAdjustment).HasPrecision(18, 2);
            b.HasOne(f => f.Estimate)
                .WithMany(e => e.FcoEntries)
                .HasForeignKey(f => f.EstimateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EstimateSequence>(b =>
        {
            b.HasKey(s => s.EstimateSequenceId);
            b.HasIndex(s => new { s.CompanyCode, s.Year, s.SequenceType }).IsUnique();
        });

        // ── Staffing Plans ─────────────────────────────────────────────────
        modelBuilder.Entity<StaffingPlan>(b =>
        {
            b.HasKey(sp => sp.StaffingPlanId);
            b.HasIndex(sp => new { sp.CompanyCode, sp.StaffingPlanNumber }).IsUnique();
            b.Property(sp => sp.CompanyCode).IsRequired().HasMaxLength(10);
            b.Property(sp => sp.StaffingPlanNumber).IsRequired().HasMaxLength(50);
            b.Property(sp => sp.Name).IsRequired().HasMaxLength(200);
            b.Property(sp => sp.Client).IsRequired().HasMaxLength(200);
            b.Property(sp => sp.RoughLaborTotal).HasPrecision(18, 2);

            b.HasOne(sp => sp.ConvertedEstimate)
                .WithMany()
                .HasForeignKey(sp => sp.ConvertedEstimateId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<StaffingLaborRow>(b =>
        {
            b.HasKey(r => r.StaffingLaborRowId);
            b.Property(r => r.StRate).HasPrecision(18, 4);
            b.Property(r => r.OtRate).HasPrecision(18, 4);
            b.Property(r => r.DtRate).HasPrecision(18, 4);
            b.Property(r => r.StHours).HasPrecision(10, 2);
            b.Property(r => r.OtHours).HasPrecision(10, 2);
            b.Property(r => r.DtHours).HasPrecision(10, 2);
            b.Property(r => r.Subtotal).HasPrecision(18, 2);
            b.HasOne(r => r.StaffingPlan)
                .WithMany(sp => sp.LaborRows)
                .HasForeignKey(r => r.StaffingPlanId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Rate Books ─────────────────────────────────────────────────────
        modelBuilder.Entity<RateBook>(b =>
        {
            b.HasKey(rb => rb.RateBookId);
            b.Property(rb => rb.CompanyCode).IsRequired().HasMaxLength(10);
            b.Property(rb => rb.Name).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<RateBookLaborRate>(b =>
        {
            b.HasKey(r => r.RateBookLaborRateId);
            b.Property(r => r.StRate).HasPrecision(18, 4);
            b.Property(r => r.OtRate).HasPrecision(18, 4);
            b.Property(r => r.DtRate).HasPrecision(18, 4);
            b.HasOne(r => r.RateBook)
                .WithMany(rb => rb.LaborRates)
                .HasForeignKey(r => r.RateBookId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RateBookEquipmentRate>(b =>
        {
            b.HasKey(r => r.RateBookEquipmentRateId);
            b.Property(r => r.Hourly).HasPrecision(18, 4);
            b.Property(r => r.Daily).HasPrecision(18, 4);
            b.Property(r => r.Weekly).HasPrecision(18, 4);
            b.Property(r => r.Monthly).HasPrecision(18, 4);
            b.HasOne(r => r.RateBook)
                .WithMany(rb => rb.EquipmentRates)
                .HasForeignKey(r => r.RateBookId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RateBookExpenseItem>(b =>
        {
            b.HasKey(r => r.RateBookExpenseItemId);
            b.Property(r => r.Rate).HasPrecision(18, 4);
            b.HasOne(r => r.RateBook)
                .WithMany(rb => rb.ExpenseItems)
                .HasForeignKey(r => r.RateBookId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CrewTemplate>(b =>
        {
            b.HasKey(ct => ct.CrewTemplateId);
            b.Property(ct => ct.CompanyCode).IsRequired().HasMaxLength(10);
            b.Property(ct => ct.Name).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<CrewTemplateRow>(b =>
        {
            b.HasKey(r => r.CrewTemplateRowId);
            b.HasOne(r => r.CrewTemplate)
                .WithMany(ct => ct.Rows)
                .HasForeignKey(r => r.CrewTemplateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Cost Books ─────────────────────────────────────────────────────
        modelBuilder.Entity<CostBook>(b =>
        {
            b.HasKey(cb => cb.CostBookId);
            b.Property(cb => cb.CompanyCode).IsRequired().HasMaxLength(10);
            b.Property(cb => cb.Name).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<CostBookLaborRate>(b =>
        {
            b.HasKey(r => r.CostBookLaborRateId);
            b.Property(r => r.StRate).HasPrecision(18, 4);
            b.Property(r => r.OtRate).HasPrecision(18, 4);
            b.Property(r => r.DtRate).HasPrecision(18, 4);
            b.HasOne(r => r.CostBook)
                .WithMany(cb => cb.LaborRates)
                .HasForeignKey(r => r.CostBookId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CostBookEquipmentRate>(b =>
        {
            b.HasKey(r => r.CostBookEquipmentRateId);
            b.Property(r => r.Hourly).HasPrecision(18, 4);
            b.Property(r => r.Daily).HasPrecision(18, 4);
            b.Property(r => r.Weekly).HasPrecision(18, 4);
            b.Property(r => r.Monthly).HasPrecision(18, 4);
            b.HasOne(r => r.CostBook)
                .WithMany(cb => cb.EquipmentRates)
                .HasForeignKey(r => r.CostBookId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CostBookExpense>(b =>
        {
            b.HasKey(r => r.CostBookExpenseId);
            b.Property(r => r.Rate).HasPrecision(18, 4);
            b.HasOne(r => r.CostBook)
                .WithMany(cb => cb.Expenses)
                .HasForeignKey(r => r.CostBookId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CostBookOverheadItem>(b =>
        {
            b.HasKey(r => r.CostBookOverheadItemId);
            b.Property(r => r.Value).HasPrecision(10, 4);
            b.HasOne(r => r.CostBook)
                .WithMany(cb => cb.OverheadItems)
                .HasForeignKey(r => r.CostBookId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Planning — Lifecycle Models ────────────────────────────────────
        modelBuilder.Entity<CommercialAuthorization>(b =>
        {
            b.HasKey(ca => ca.CommercialAuthorizationId);
            b.HasIndex(ca => new { ca.CompanyCode, ca.AuthorizationNumber });
            b.Property(ca => ca.CompanyCode).IsRequired().HasMaxLength(10);
            b.Property(ca => ca.AuthorizationNumber).IsRequired().HasMaxLength(100);
            b.Property(ca => ca.AuthorizationType).IsRequired().HasMaxLength(50);
            b.Property(ca => ca.AuthorizedValue).HasPrecision(18, 2);
            b.Property(ca => ca.AuthorizedBy).HasMaxLength(200);
            b.Property(ca => ca.Status).IsRequired().HasMaxLength(30).HasDefaultValue("Draft");
            b.Property(ca => ca.DocumentReference).HasMaxLength(500);
            b.Property(ca => ca.CreatedBy).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<Project>(b =>
        {
            b.HasKey(p => p.ProjectId);
            b.HasIndex(p => new { p.CompanyCode, p.ProjectNumber }).IsUnique();
            b.Property(p => p.CompanyCode).IsRequired().HasMaxLength(10);
            b.Property(p => p.ProjectNumber).IsRequired().HasMaxLength(50);
            b.Property(p => p.Name).IsRequired().HasMaxLength(200);
            b.Property(p => p.Client).HasMaxLength(200);
            b.Property(p => p.ClientCode).HasMaxLength(50);
            b.Property(p => p.Site).HasMaxLength(200);
            b.Property(p => p.City).HasMaxLength(100);
            b.Property(p => p.State).HasMaxLength(50);
            b.Property(p => p.JobLetter).HasMaxLength(10);
            b.Property(p => p.Status).IsRequired().HasMaxLength(30).HasDefaultValue("Initiating");
            b.Property(p => p.AtRiskThresholdDays).HasDefaultValue(5);
            b.Property(p => p.OwnerUserId).HasMaxLength(100);
            b.Property(p => p.CreatedBy).IsRequired().HasMaxLength(100);
            b.HasOne(p => p.CommercialAuthorization)
                .WithMany(ca => ca.Projects)
                .HasForeignKey(p => p.CommercialAuthorizationId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<WorkOrder>(b =>
        {
            b.HasKey(wo => wo.WorkOrderId);
            b.HasIndex(wo => new { wo.CompanyCode, wo.WorkOrderNumber }).IsUnique();
            b.Property(wo => wo.CompanyCode).IsRequired().HasMaxLength(10);
            b.Property(wo => wo.WorkOrderNumber).IsRequired().HasMaxLength(50);
            b.Property(wo => wo.Title).IsRequired().HasMaxLength(200);
            b.Property(wo => wo.AuthorizedValue).HasPrecision(18, 2).HasDefaultValue(0m);
            b.Property(wo => wo.Status).IsRequired().HasMaxLength(30).HasDefaultValue("Draft");
            b.Property(wo => wo.ReleasedBy).HasMaxLength(200);
            b.Property(wo => wo.CreatedBy).IsRequired().HasMaxLength(100);
            b.HasOne(wo => wo.Project)
                .WithMany(p => p.WorkOrders)
                .HasForeignKey(wo => wo.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne(wo => wo.CommercialAuthorization)
                .WithMany(ca => ca.WorkOrders)
                .HasForeignKey(wo => wo.CommercialAuthorizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProjectPhase>(b =>
        {
            b.HasKey(ph => ph.PhaseId);
            b.Property(ph => ph.Name).IsRequired().HasMaxLength(200);
            b.Property(ph => ph.Status).IsRequired().HasMaxLength(30).HasDefaultValue("Planning");
            b.Property(ph => ph.Color).HasMaxLength(20);
            b.HasOne(ph => ph.Project)
                .WithMany(p => p.Phases)
                .HasForeignKey(ph => ph.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlanTask>(b =>
        {
            b.HasKey(t => t.TaskId);
            b.Property(t => t.Title).IsRequired().HasMaxLength(300);
            b.Property(t => t.TaskType).IsRequired().HasMaxLength(30).HasDefaultValue("Task");
            b.Property(t => t.Status).IsRequired().HasMaxLength(30).HasDefaultValue("Pending");
            b.Property(t => t.PercentComplete).HasPrecision(5, 2);
            b.Property(t => t.CraftCode).HasMaxLength(50);
            b.Property(t => t.AssignedTo).HasMaxLength(200);
            b.Property(t => t.OwnerUserId).HasMaxLength(100);
            b.Property(t => t.CreatedBy).IsRequired().HasMaxLength(100);
            b.HasOne(t => t.Phase)
                .WithMany(ph => ph.Tasks)
                .HasForeignKey(t => t.PhaseId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(t => t.ParentTask)
                .WithMany(t => t.SubTasks)
                .HasForeignKey(t => t.ParentTaskId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<TaskDependency>(b =>
        {
            b.HasKey(d => d.DependencyId);
            b.Property(d => d.DependencyType).IsRequired().HasMaxLength(30).HasDefaultValue("FinishToStart");
            b.HasOne(d => d.SuccessorTask)
                .WithMany(t => t.SuccessorDependencies)
                .HasForeignKey(d => d.SuccessorTaskId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(d => d.PredecessorTask)
                .WithMany(t => t.PredecessorDependencies)
                .HasForeignKey(d => d.PredecessorTaskId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Milestone>(b =>
        {
            b.HasKey(m => m.MilestoneId);
            b.Property(m => m.Name).IsRequired().HasMaxLength(200);
            b.Property(m => m.Status).IsRequired().HasMaxLength(30).HasDefaultValue("Pending");
            b.Property(m => m.Color).HasMaxLength(20);
            b.Property(m => m.CreatedBy).IsRequired().HasMaxLength(100);
            b.HasOne(m => m.Project)
                .WithMany(p => p.Milestones)
                .HasForeignKey(m => m.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(m => m.Phase)
                .WithMany(ph => ph.Milestones)
                .HasForeignKey(m => m.PhaseId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(m => m.Task)
                .WithMany(t => t.Milestones)
                .HasForeignKey(m => m.TaskId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ── Planning — Step-Out / Work Packages ────────────────────────────
        modelBuilder.Entity<StepOutPlan>(b =>
        {
            b.HasKey(p => p.PlanId);
            b.Property(p => p.CompanyCode).IsRequired().HasMaxLength(10);
            b.Property(p => p.Name).IsRequired().HasMaxLength(200);
            b.Property(p => p.Status).IsRequired().HasMaxLength(30);
            b.Property(p => p.CreatedBy).IsRequired().HasMaxLength(100);
            b.HasOne(p => p.WorkOrder)
                .WithMany(wo => wo.StepOutPlans)
                .HasForeignKey(p => p.WorkOrderId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<StepOutStep>(b =>
        {
            b.HasKey(s => s.StepId);
            b.Property(s => s.StepCode).IsRequired().HasMaxLength(20);
            b.Property(s => s.SortOrder).HasPrecision(10, 4);
            b.Property(s => s.Title).IsRequired().HasMaxLength(300);
            b.Property(s => s.Status).IsRequired().HasMaxLength(30);
            b.Property(s => s.DurationHours).HasPrecision(8, 2);
            b.HasOne(s => s.Plan)
                .WithMany(p => p.Steps)
                .HasForeignKey(s => s.PlanId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(s => s.ParentStep)
                .WithMany(s => s.SubSteps)
                .HasForeignKey(s => s.ParentStepId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<StepDependency>(b =>
        {
            b.HasKey(d => d.DependencyId);
            b.HasOne(d => d.Step)
                .WithMany(s => s.Dependencies)
                .HasForeignKey(d => d.StepId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(d => d.PredecessorStep)
                .WithMany()
                .HasForeignKey(d => d.PredecessorStepId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<StepResourceReq>(b =>
        {
            b.HasKey(r => r.ReqId);
            b.Property(r => r.CraftCode).IsRequired().HasMaxLength(50);
            b.HasOne(r => r.Step)
                .WithMany(s => s.ResourceRequirements)
                .HasForeignKey(r => r.StepId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkPackage>(b =>
        {
            b.HasKey(wp => wp.PackageId);
            b.Property(wp => wp.CompanyCode).IsRequired().HasMaxLength(10);
            b.Property(wp => wp.Title).IsRequired().HasMaxLength(200);
            b.Property(wp => wp.Status).IsRequired().HasMaxLength(30);
            b.Property(wp => wp.CreatedBy).IsRequired().HasMaxLength(100);
            b.HasOne(wp => wp.Plan)
                .WithMany(p => p.WorkPackages)
                .HasForeignKey(wp => wp.PlanId)
                .OnDelete(DeleteBehavior.SetNull);
            b.HasOne(wp => wp.WorkOrder)
                .WithMany(wo => wo.WorkPackages)
                .HasForeignKey(wp => wp.WorkOrderId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<FcoDocument>(b =>
        {
            b.HasKey(f => f.FcoDocumentId);
            b.Property(f => f.CompanyCode).IsRequired().HasMaxLength(10);
            b.Property(f => f.FcoNumber).IsRequired().HasMaxLength(50);
            b.Property(f => f.Title).IsRequired().HasMaxLength(200);
            b.Property(f => f.Status).IsRequired().HasMaxLength(30);
            b.Property(f => f.TotalFcoAmount).HasPrecision(18, 2);
            b.Property(f => f.MarkupPct).HasPrecision(5, 4);
            b.Property(f => f.UpdatedContractValue).HasPrecision(18, 2);
            b.Property(f => f.TaxPct).HasPrecision(5, 4);
            b.Property(f => f.CreatedBy).IsRequired().HasMaxLength(100);
            b.HasOne(f => f.LinkedWorkOrder)
                .WithMany()
                .HasForeignKey(f => f.LinkedWorkOrderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StepOutSubStep>(b =>
        {
            b.HasKey(ss => ss.SubStepId);
            b.Property(ss => ss.SubStepCode).IsRequired().HasMaxLength(20);
            b.Property(ss => ss.SortOrder).HasPrecision(10, 4);
            b.Property(ss => ss.Title).IsRequired().HasMaxLength(300);
            b.Property(ss => ss.Status).IsRequired().HasMaxLength(30).HasDefaultValue("Pending");
            b.Property(ss => ss.DurationHours).HasPrecision(8, 2);
            b.Property(ss => ss.ActualDurationHours).HasPrecision(8, 2);
            b.Property(ss => ss.CraftCode).HasMaxLength(50);
            b.HasOne(ss => ss.Step)
                .WithMany(s => s.SubStepLeafs)
                .HasForeignKey(ss => ss.StepId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FcoLaborLine>(b =>
        {
            b.HasKey(l => l.FcoLaborLineId);
            b.Property(l => l.Position).IsRequired().HasMaxLength(100);
            b.Property(l => l.LaborType).IsRequired().HasMaxLength(20).HasDefaultValue("Direct");
            b.Property(l => l.CraftCode).HasMaxLength(50);
            b.Property(l => l.NavCode).HasMaxLength(50);
            b.Property(l => l.StHours).HasPrecision(10, 2);
            b.Property(l => l.OtHours).HasPrecision(10, 2);
            b.Property(l => l.DtHours).HasPrecision(10, 2);
            b.Property(l => l.BillStRate).HasPrecision(18, 4);
            b.Property(l => l.BillOtRate).HasPrecision(18, 4);
            b.Property(l => l.BillDtRate).HasPrecision(18, 4);
            b.Property(l => l.Subtotal).HasPrecision(18, 2);
            b.HasOne(l => l.FcoDocument)
                .WithMany()
                .HasForeignKey(l => l.FcoDocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ActualEntry>(b =>
        {
            b.HasKey(a => a.ActualEntryId);
            b.Property(a => a.CompanyCode).IsRequired().HasMaxLength(10);
            b.Property(a => a.ActualType).IsRequired().HasMaxLength(30).HasDefaultValue("Labor");
            b.Property(a => a.Position).HasMaxLength(100);
            b.Property(a => a.CraftCode).HasMaxLength(50);
            b.Property(a => a.EnteredBy).IsRequired().HasMaxLength(100);
            b.Property(a => a.StHours).HasPrecision(10, 2);
            b.Property(a => a.OtHours).HasPrecision(10, 2);
            b.Property(a => a.DtHours).HasPrecision(10, 2);
            b.Property(a => a.CostAmount).HasPrecision(18, 2);
            b.Property(a => a.BillableAmount).HasPrecision(18, 2);
            b.Property(a => a.BilledAmount).HasPrecision(18, 2);
            b.HasOne(a => a.WorkOrder)
                .WithMany()
                .HasForeignKey(a => a.WorkOrderId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne(a => a.FcoDocument)
                .WithMany()
                .HasForeignKey(a => a.FcoDocumentId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(a => a.PlanTask)
                .WithMany()
                .HasForeignKey(a => a.PlanTaskId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(a => a.StepOutStep)
                .WithMany()
                .HasForeignKey(a => a.StepOutStepId)
                .OnDelete(DeleteBehavior.NoAction);
            b.HasOne(a => a.StepOutSubStep)
                .WithMany()
                .HasForeignKey(a => a.StepOutSubStepId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<EstimateTaskLink>(b =>
        {
            b.HasKey(l => l.LinkId);
            b.HasIndex(l => new { l.TaskId, l.EstimateId }).IsUnique();
            b.Property(l => l.LinkType).IsRequired().HasMaxLength(30).HasDefaultValue("Primary");
            b.HasOne(l => l.Task)
                .WithMany()
                .HasForeignKey(l => l.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FcoTaskLink>(b =>
        {
            b.HasKey(l => l.LinkId);
            b.HasIndex(l => new { l.TaskId, l.FcoDocumentId }).IsUnique();
            b.Property(l => l.LinkType).IsRequired().HasMaxLength(30).HasDefaultValue("ScopeAddition");
            b.HasOne(l => l.Task)
                .WithMany()
                .HasForeignKey(l => l.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(l => l.FcoDocument)
                .WithMany()
                .HasForeignKey(l => l.FcoDocumentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TimelineBaseline>(b =>
        {
            b.HasKey(tb => tb.BaselineId);
            b.Property(tb => tb.EntityType).IsRequired().HasMaxLength(20).HasDefaultValue("Project");
            b.Property(tb => tb.BaselineReason).HasMaxLength(500);
            b.Property(tb => tb.BaselineLabel).HasMaxLength(100);
            b.Property(tb => tb.LockedBy).IsRequired().HasMaxLength(100);
            b.HasOne(tb => tb.Project)
                .WithMany()
                .HasForeignKey(tb => tb.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TaskProgressSnapshot>(b =>
        {
            b.HasKey(s => s.SnapshotId);
            b.Property(s => s.Status).IsRequired().HasMaxLength(30);
            b.Property(s => s.ReportedBy).IsRequired().HasMaxLength(100);
            b.Property(s => s.PercentComplete).HasPrecision(5, 2);
            b.HasOne(s => s.Task)
                .WithMany()
                .HasForeignKey(s => s.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Scheduling ─────────────────────────────────────────────────────
        modelBuilder.Entity<Craft>(b =>
        {
            b.HasKey(c => c.CraftCode);
            b.Property(c => c.CraftCode).IsRequired().HasMaxLength(50);
            b.Property(c => c.Title).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<Resource>(b =>
        {
            b.HasKey(r => r.ResourceId);
            b.Property(r => r.CompanyCode).IsRequired().HasMaxLength(10);
            b.Property(r => r.Name).IsRequired().HasMaxLength(200);
            b.Property(r => r.EmployeeId).HasMaxLength(50);
            b.Property(r => r.FirstName).IsRequired().HasMaxLength(100);
            b.Property(r => r.LastName).IsRequired().HasMaxLength(100);
            b.Property(r => r.CraftCode).IsRequired().HasMaxLength(50);
            b.Property(r => r.Region).HasMaxLength(100);
            b.Property(r => r.Branch).HasMaxLength(100);
            b.Property(r => r.EmploymentStatus).IsRequired().HasMaxLength(20).HasDefaultValue("Active");
            b.Property(r => r.ShiftEligibility).IsRequired().HasMaxLength(20).HasDefaultValue("Any");
            b.Property(r => r.Phone).HasMaxLength(30);
            b.Property(r => r.Email).HasMaxLength(200);
            b.HasIndex(r => new { r.CompanyCode, r.EmployeeId });
            b.HasOne(r => r.Craft)
                .WithMany(c => c.Resources)
                .HasForeignKey(r => r.CraftCode)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Certification>(b =>
        {
            b.HasKey(c => c.CertId);
            b.Property(c => c.Type).IsRequired().HasMaxLength(50);
            b.HasOne(c => c.Resource)
                .WithMany(r => r.Certifications)
                .HasForeignKey(c => c.ResourceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AvailabilityBlock>(b =>
        {
            b.HasKey(ab => ab.BlockId);
            b.HasOne(ab => ab.Resource)
                .WithMany(r => r.AvailabilityBlocks)
                .HasForeignKey(ab => ab.ResourceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Assignment>(b =>
        {
            b.HasKey(a => a.AssignmentId);
            b.Property(a => a.CompanyCode).IsRequired().HasMaxLength(10);
            b.Property(a => a.CraftCode).IsRequired().HasMaxLength(50);
            b.Property(a => a.JobSourceType).IsRequired().HasMaxLength(30);
            b.Property(a => a.Shift).IsRequired().HasMaxLength(20);
            b.Property(a => a.Status).IsRequired().HasMaxLength(20);
            b.Property(a => a.CreatedBy).IsRequired().HasMaxLength(100);
            b.HasOne(a => a.Resource)
                .WithMany(r => r.Assignments)
                .HasForeignKey(a => a.ResourceId)
                .OnDelete(DeleteBehavior.Cascade);
        });
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
