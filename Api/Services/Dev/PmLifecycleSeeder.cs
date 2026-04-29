using Microsoft.EntityFrameworkCore;
using Stronghold.EnterpriseEstimating.Data;
using Stronghold.EnterpriseEstimating.Data.Models;
using Stronghold.EnterpriseEstimating.Data.Models.Planning;
using Stronghold.EnterpriseEstimating.Data.Models.Scheduling;

namespace Stronghold.EnterpriseEstimating.Api.Services.Dev;

public static class PmLifecycleSeeder
{
    public static async Task SeedPmLifecycle(AppDbContext db)
    {
        if (await db.Projects.AnyAsync(p => p.ProjectNumber == "PRJ-DEMO-A")) return;

        var today = DateTime.UtcNow.Date;

        // Look up comprehensive resources by EmployeeId
        var resMap = await db.Resources
            .Where(r => r.CompanyCode == "CSL" && r.EmployeeId != null)
            .ToDictionaryAsync(r => r.EmployeeId!, r => r.ResourceId);

        // ═══════════════════════════════════════════════════════════════════════
        // SCENARIO A — Shell Deer Park Unit 4 Piping Repair (Active / Behind)
        // ═══════════════════════════════════════════════════════════════════════

        var estA = new Estimate
        {
            CompanyCode="CSL", EstimateNumber="26-0050-SHELL",
            Name="Shell Deer Park - Unit 4 Piping Repair",
            Client="Shell Oil Company", ClientCode="SHELL", MsaNumber="MSA-SHELL-2024-01",
            JobType="Maintenance", Branch="Industrial", City="Deer Park", State="TX",
            Site="Shell Deer Park Refinery", Shift="Day", HoursPerShift=10,
            VP="Mark Ellis", Director="Rachel Kim", Region="Gulf",
            Days=74, StartDate=new DateTime(2026,2,15), EndDate=new DateTime(2026,4,30),
            OtMethod="daily8_weekly40", DtWeekends="none",
            Status="Awarded", ConfidencePct=100, IsScenario=false,
            CreatedBy="dev.seed", UpdatedBy="dev.seed",
            CreatedAt=DateTimeOffset.UtcNow.AddDays(-150), UpdatedAt=DateTimeOffset.UtcNow.AddDays(-120),
            Summary=new EstimateSummary
            {
                BillSubtotal=1_250_000m, GrandTotal=1_250_000m,
                InternalCostTotal=875_000m, GrossProfit=375_000m, GrossMarginPct=30.0m, DiscountType="None"
            }
        };
        db.Estimates.Add(estA);
        await db.SaveChangesAsync();

        var caA = new CommercialAuthorization
        {
            CompanyCode="CSL", EstimateId=estA.EstimateId,
            AuthorizationType="PO", AuthorizationNumber="PO-SHELL-2026-0441",
            AuthorizedValue=1_250_000m, AuthorizedBy="Shell Global Procurement",
            ReceivedDate=new DateTime(2026,1,20), EffectiveDate=new DateTime(2026,2,1),
            ExpirationDate=new DateTime(2026,12,31), Status="Active",
            Notes="Shell MSA maintenance PO for Unit 4 piping scope. Two approved FCOs on record.",
            CreatedBy="dev.seed"
        };
        db.CommercialAuthorizations.Add(caA);
        await db.SaveChangesAsync();

        var projA = new Project
        {
            CompanyCode="CSL", ProjectNumber="PRJ-DEMO-A",
            Name="Shell Deer Park Unit 4 Piping Repair",
            EstimateId=estA.EstimateId, CommercialAuthorizationId=caA.CommercialAuthorizationId,
            Client="Shell Oil Company", ClientCode="SHELL",
            Site="Shell Deer Park Refinery", City="Deer Park", State="TX", JobLetter="A",
            PlannedStart=new DateTime(2026,2,15), PlannedEnd=new DateTime(2026,4,30),
            ForecastEnd=new DateTime(2026,5,15),
            ActualStart=new DateTime(2026,2,17),
            Status="Active", AtRiskThresholdDays=5, OwnerUserId="estimator.csl",
            CreatedBy="dev.seed"
        };

        // Phase 1: Mobilization — Complete
        var phA1 = new ProjectPhase
        {
            Name="Mobilization", Status="Complete", SortOrder=1, Color="#22c55e",
            PlannedStart=new DateTime(2026,2,15), PlannedEnd=new DateTime(2026,2,20),
            ActualStart=new DateTime(2026,2,17), ActualEnd=new DateTime(2026,2,19)
        };
        phA1.Tasks.Add(new PlanTask { Title="Establish site camp and offices", TaskType="Task", Status="Complete", PercentComplete=100, DurationDays=2, SortOrder=1, CreatedBy="dev.seed", PlannedStart=new DateTime(2026,2,15), PlannedEnd=new DateTime(2026,2,17), ActualStart=new DateTime(2026,2,17), ActualEnd=new DateTime(2026,2,18) });
        phA1.Tasks.Add(new PlanTask { Title="Tool / material receiving and staging", TaskType="Task", Status="Complete", PercentComplete=100, DurationDays=2, SortOrder=2, CreatedBy="dev.seed", PlannedStart=new DateTime(2026,2,17), PlannedEnd=new DateTime(2026,2,19), ActualStart=new DateTime(2026,2,18), ActualEnd=new DateTime(2026,2,19) });

        // Phase 2: Isolation & Prep — Complete
        var phA2 = new ProjectPhase
        {
            Name="Unit 4 Isolation & Preparation", Status="Complete", SortOrder=2, Color="#22c55e",
            PlannedStart=new DateTime(2026,2,20), PlannedEnd=new DateTime(2026,3,10),
            ActualStart=new DateTime(2026,2,20), ActualEnd=new DateTime(2026,3,8)
        };
        phA2.Tasks.Add(new PlanTask { Title="Develop isolation plan and LOTO procedures", TaskType="Task", Status="Complete", PercentComplete=100, DurationDays=2, SortOrder=1, CreatedBy="dev.seed", PlannedStart=new DateTime(2026,2,20), PlannedEnd=new DateTime(2026,2,22), ActualStart=new DateTime(2026,2,20), ActualEnd=new DateTime(2026,2,22) });
        phA2.Tasks.Add(new PlanTask { Title="Lock out / tag out — all Unit 4 isolation points", TaskType="Gate", Status="Complete", PercentComplete=100, DurationDays=1, SortOrder=2, CreatedBy="dev.seed", PlannedStart=new DateTime(2026,2,23), PlannedEnd=new DateTime(2026,2,23), ActualStart=new DateTime(2026,2,23), ActualEnd=new DateTime(2026,2,23) });
        phA2.Tasks.Add(new PlanTask { Title="Establish hot work permit area at Unit 4", TaskType="Task", Status="Complete", PercentComplete=100, DurationDays=5, SortOrder=3, CreatedBy="dev.seed", PlannedStart=new DateTime(2026,2,24), PlannedEnd=new DateTime(2026,3,1), ActualStart=new DateTime(2026,2,24), ActualEnd=new DateTime(2026,3,1) });
        phA2.Tasks.Add(new PlanTask { Title="Scaffold erection — Spools A–C access", TaskType="Task", Status="Complete", PercentComplete=100, DurationDays=7, SortOrder=4, CreatedBy="dev.seed", PlannedStart=new DateTime(2026,3,1), PlannedEnd=new DateTime(2026,3,8), ActualStart=new DateTime(2026,3,1), ActualEnd=new DateTime(2026,3,8) });

        // Phase 3: Piping Removal & Install — Active / Behind
        var phA3 = new ProjectPhase
        {
            Name="Piping Removal & Installation", Status="Active", SortOrder=3, Color="#3b82f6",
            PlannedStart=new DateTime(2026,3,9), PlannedEnd=new DateTime(2026,4,15),
            ForecastEnd=new DateTime(2026,5,1), ActualStart=new DateTime(2026,3,9)
        };
        phA3.Tasks.Add(new PlanTask { Title="Cut and remove existing flanges — V-401, V-402", TaskType="Task", Status="Complete", PercentComplete=100, DurationDays=11, SortOrder=1, CreatedBy="dev.seed", PlannedStart=new DateTime(2026,3,9), PlannedEnd=new DateTime(2026,3,20), ActualStart=new DateTime(2026,3,9), ActualEnd=new DateTime(2026,3,22) });
        phA3.Tasks.Add(new PlanTask { Title="Remove 6\" CS piping runs — Spools A through C", TaskType="Task", Status="Complete", PercentComplete=100, DurationDays=12, SortOrder=2, CreatedBy="dev.seed", PlannedStart=new DateTime(2026,3,20), PlannedEnd=new DateTime(2026,4,1), ActualStart=new DateTime(2026,3,22), ActualEnd=new DateTime(2026,4,5) });
        phA3.Tasks.Add(new PlanTask { Title="Fit-up and alignment — Spool A new piping", TaskType="Task", Status="InProgress", PercentComplete=75, DurationDays=19, SortOrder=3, CreatedBy="dev.seed", PlannedStart=new DateTime(2026,4,1), PlannedEnd=new DateTime(2026,4,20), ForecastEnd=new DateTime(2026,4,30), ActualStart=new DateTime(2026,4,5) });
        phA3.Tasks.Add(new PlanTask { Title="Full penetration welding — Spool A (P91 spec)", TaskType="Task", Status="InProgress", PercentComplete=60, DurationDays=20, SortOrder=4, CreatedBy="dev.seed", PlannedStart=new DateTime(2026,4,10), PlannedEnd=new DateTime(2026,4,30), ForecastEnd=new DateTime(2026,5,10), ActualStart=new DateTime(2026,4,12) });
        phA3.Tasks.Add(new PlanTask { Title="PWHT per P91 heat treatment schedule", TaskType="Task", Status="Pending", PercentComplete=0, DurationDays=10, SortOrder=5, CreatedBy="dev.seed", PlannedStart=new DateTime(2026,4,20), PlannedEnd=new DateTime(2026,4,30), ForecastEnd=new DateTime(2026,5,12) });

        // Phase 4: Inspection — Planning
        var phA4 = new ProjectPhase
        {
            Name="Inspection & Pressure Testing", Status="Planning", SortOrder=4,
            PlannedStart=new DateTime(2026,4,25), PlannedEnd=new DateTime(2026,4,30),
            ForecastEnd=new DateTime(2026,5,13)
        };
        phA4.Tasks.Add(new PlanTask { Title="RT / UT examination — 100% weld coverage", TaskType="Gate", Status="Pending", PercentComplete=0, DurationDays=2, SortOrder=1, CreatedBy="dev.seed", PlannedStart=new DateTime(2026,4,25), PlannedEnd=new DateTime(2026,4,27), ForecastEnd=new DateTime(2026,5,13) });
        phA4.Tasks.Add(new PlanTask { Title="Pressure test per SP-2024-04 procedure", TaskType="Task", Status="Pending", PercentComplete=0, DurationDays=2, SortOrder=2, CreatedBy="dev.seed", PlannedStart=new DateTime(2026,4,28), PlannedEnd=new DateTime(2026,4,30), ForecastEnd=new DateTime(2026,5,14) });

        // Phase 5: Demob — Planning
        var phA5 = new ProjectPhase
        {
            Name="Demobilization", Status="Planning", SortOrder=5,
            PlannedStart=new DateTime(2026,5,1), PlannedEnd=new DateTime(2026,5,5),
            ForecastEnd=new DateTime(2026,5,16)
        };
        phA5.Tasks.Add(new PlanTask { Title="Scaffold removal and site cleanup", TaskType="Task", Status="Pending", PercentComplete=0, DurationDays=3, SortOrder=1, CreatedBy="dev.seed", PlannedStart=new DateTime(2026,5,1), PlannedEnd=new DateTime(2026,5,3), ForecastEnd=new DateTime(2026,5,15) });
        phA5.Tasks.Add(new PlanTask { Title="Punch list walkdown and client sign-off", TaskType="Gate", Status="Pending", PercentComplete=0, DurationDays=1, SortOrder=2, CreatedBy="dev.seed", PlannedStart=new DateTime(2026,5,4), PlannedEnd=new DateTime(2026,5,5), ForecastEnd=new DateTime(2026,5,16) });

        projA.Phases.Add(phA1); projA.Phases.Add(phA2); projA.Phases.Add(phA3); projA.Phases.Add(phA4); projA.Phases.Add(phA5);
        projA.Milestones.Add(new Milestone { Name="Unit 4 Isolation Complete", PlannedDate=new DateTime(2026,3,10), BaselineDate=new DateTime(2026,3,10), ActualDate=new DateTime(2026,3,8), Status="Achieved", CreatedBy="dev.seed" });
        projA.Milestones.Add(new Milestone { Name="Piping Removal Complete", PlannedDate=new DateTime(2026,4,1), BaselineDate=new DateTime(2026,4,1), ActualDate=new DateTime(2026,4,5), Status="Missed", IsCritical=true, CreatedBy="dev.seed" });
        projA.Milestones.Add(new Milestone { Name="Hot Work Complete", PlannedDate=new DateTime(2026,4,30), BaselineDate=new DateTime(2026,4,30), Status="Pending", IsCritical=true, IsDeadline=true, CreatedBy="dev.seed" });
        projA.Milestones.Add(new Milestone { Name="Client Handover — Shell Deer Park", PlannedDate=new DateTime(2026,5,5), Status="Pending", IsCritical=true, IsDeadline=true, CreatedBy="dev.seed" });
        db.Projects.Add(projA);
        await db.SaveChangesAsync();

        var woA = new WorkOrder
        {
            CompanyCode="CSL", WorkOrderNumber="WO-DEMO-A-001",
            ProjectId=projA.ProjectId, EstimateId=estA.EstimateId, CommercialAuthorizationId=caA.CommercialAuthorizationId,
            Title="Unit 4 Piping Repair — Main Execution Scope",
            Description="Full replacement of Unit 4 crude transfer piping, Spools A–C.",
            Scope="Replace ~240 LF of 6\" CS piping per IFC drawing SP-2026-U4-001 Rev 2. Includes fit-up, welding, PWHT, and RT/UT examination per ASME B31.3. FCO-DEMO-A-001 adds sleeve repair at V-401.",
            AuthorizedValue=900_000m,
            PlannedStart=new DateTime(2026,2,15), PlannedEnd=new DateTime(2026,4,30),
            ForecastEnd=new DateTime(2026,5,15), ActualStart=new DateTime(2026,2,17),
            Status="InProgress", ReleasedBy="estimator.csl",
            ReleasedAt=new DateTimeOffset(new DateTime(2026,2,14), TimeSpan.Zero),
            CreatedBy="dev.seed"
        };
        db.WorkOrders.Add(woA);
        await db.SaveChangesAsync();

        // Step-Out Plan A
        var sopA = new StepOutPlan
        {
            CompanyCode="CSL", Name="Unit 4 Piping Replacement — Execution SOP",
            SourceType="Estimate", LinkedEstimateId=estA.EstimateId,
            Client="Shell Oil Company", Site="Shell Deer Park Refinery",
            PlannedStart=new DateTime(2026,2,15), PlannedEnd=new DateTime(2026,4,30),
            Status="Active", WorkOrderId=woA.WorkOrderId, CreatedBy="dev.seed"
        };

        var sA1 = new StepOutStep { SortOrder=1, StepCode="1", Title="Isolate Unit 4", Status="Complete", CraftCode="SUP", RequiredPeople=2, DurationHours=8, PlannedStart=new DateTime(2026,2,17), PlannedEnd=new DateTime(2026,2,17) };
        sA1.SubStepLeafs.Add(new StepOutSubStep { SubStepCode="1.1", SortOrder=1, Title="Lock out / tag out all isolation points", Status="Complete", CraftCode="SUP", RequiredPeople=2, DurationHours=4, ActualStart=new DateTime(2026,2,17), ActualEnd=new DateTime(2026,2,17), ActualDurationHours=4 });
        sA1.SubStepLeafs.Add(new StepOutSubStep { SubStepCode="1.2", SortOrder=2, Title="Pressure relief verification and bleed-down", Status="Complete", CraftCode="PF", RequiredPeople=2, DurationHours=4, ActualStart=new DateTime(2026,2,17), ActualEnd=new DateTime(2026,2,17), ActualDurationHours=3.5m });

        var sA2 = new StepOutStep { SortOrder=2, StepCode="2", Title="Remove Existing Piping", Status="Complete", CraftCode="PF", RequiredPeople=5, DurationHours=120, PlannedStart=new DateTime(2026,3,9), PlannedEnd=new DateTime(2026,4,1) };
        sA2.SubStepLeafs.Add(new StepOutSubStep { SubStepCode="2.1", SortOrder=1, Title="Cut flanges at V-401 and V-402", Status="Complete", CraftCode="PF", RequiredPeople=3, DurationHours=40, ActualStart=new DateTime(2026,3,9), ActualEnd=new DateTime(2026,3,20), ActualDurationHours=44 });
        sA2.SubStepLeafs.Add(new StepOutSubStep { SubStepCode="2.2", SortOrder=2, Title="Remove 6\" CS piping runs — Spools A, B, C", Status="Complete", CraftCode="PF", RequiredPeople=5, DurationHours=60, ActualStart=new DateTime(2026,3,20), ActualEnd=new DateTime(2026,4,5), ActualDurationHours=68 });
        sA2.SubStepLeafs.Add(new StepOutSubStep { SubStepCode="2.3", SortOrder=3, Title="Clean and bevel weld ends", Status="Complete", CraftCode="PF", RequiredPeople=3, DurationHours=20, ActualStart=new DateTime(2026,4,2), ActualEnd=new DateTime(2026,4,5), ActualDurationHours=22 });

        var sA3 = new StepOutStep { SortOrder=3, StepCode="3", Title="Install New Piping", Status="InProgress", CraftCode="PF", RequiredPeople=5, DurationHours=200, PlannedStart=new DateTime(2026,4,1), PlannedEnd=new DateTime(2026,4,25) };
        sA3.SubStepLeafs.Add(new StepOutSubStep { SubStepCode="3.1", SortOrder=1, Title="Fit-up 6\" CS pipe segments per IFC drawing", Status="InProgress", CraftCode="PF", RequiredPeople=5, DurationHours=80, ActualStart=new DateTime(2026,4,5) });
        sA3.SubStepLeafs.Add(new StepOutSubStep { SubStepCode="3.2", SortOrder=2, Title="Tack weld and alignment verification", Status="Pending", CraftCode="WD", RequiredPeople=3, DurationHours=40 });
        sA3.SubStepLeafs.Add(new StepOutSubStep { SubStepCode="3.3", SortOrder=3, Title="Full penetration weld per P91 specification", Status="Pending", CraftCode="WD", RequiredPeople=3, DurationHours=80 });

        var sA4 = new StepOutStep { SortOrder=4, StepCode="4", Title="PWHT and NDT Inspection", Status="Pending", CraftCode="WD", RequiredPeople=2, DurationHours=60, PlannedStart=new DateTime(2026,4,20), PlannedEnd=new DateTime(2026,4,30) };
        sA4.SubStepLeafs.Add(new StepOutSubStep { SubStepCode="4.1", SortOrder=1, Title="PWHT per P91 heat treatment schedule", Status="Pending", CraftCode="WD", RequiredPeople=2, DurationHours=40 });
        sA4.SubStepLeafs.Add(new StepOutSubStep { SubStepCode="4.2", SortOrder=2, Title="RT / UT examination — 100% weld coverage", Status="Pending", CraftCode="NDT", RequiredPeople=2, DurationHours=20 });

        var sA5 = new StepOutStep { SortOrder=5, StepCode="5", Title="Restore and Pressure Test", Status="Pending", CraftCode="PF", RequiredPeople=3, DurationHours=24, PlannedStart=new DateTime(2026,4,28), PlannedEnd=new DateTime(2026,4,30) };
        sA5.SubStepLeafs.Add(new StepOutSubStep { SubStepCode="5.1", SortOrder=1, Title="Pressure test per procedure SP-2024-04", Status="Pending", CraftCode="PF", RequiredPeople=3, DurationHours=12 });
        sA5.SubStepLeafs.Add(new StepOutSubStep { SubStepCode="5.2", SortOrder=2, Title="Restore insulation and cladding", Status="Pending", CraftCode="PF", RequiredPeople=3, DurationHours=12 });

        sopA.Steps.Add(sA1); sopA.Steps.Add(sA2); sopA.Steps.Add(sA3); sopA.Steps.Add(sA4); sopA.Steps.Add(sA5);

        // Work Packages A (Released demand)
        var wpA1 = new WorkPackage { CompanyCode="CSL", PlanId=null, WorkOrderId=woA.WorkOrderId, SourceType="StepOutPlan", Title="Piping Crew — Spool A/B/C Installation", CraftCode="PF", RequiredPeople=5, PlannedStart=new DateTime(2026,2,15), PlannedEnd=new DateTime(2026,5,15), Status="Scheduled", ReadyForScheduling=true, CreatedBy="dev.seed" };
        var wpA2 = new WorkPackage { CompanyCode="CSL", PlanId=null, WorkOrderId=woA.WorkOrderId, SourceType="StepOutPlan", Title="Welding Crew — P91 Weld Scope", CraftCode="WD", RequiredPeople=3, PlannedStart=new DateTime(2026,4,10), PlannedEnd=new DateTime(2026,5,12), Status="Scheduled", ReadyForScheduling=true, CreatedBy="dev.seed" };
        var wpA3 = new WorkPackage { CompanyCode="CSL", PlanId=null, WorkOrderId=woA.WorkOrderId, SourceType="StepOutPlan", Title="NDT Inspection — RT/UT Coverage", CraftCode="NDT", RequiredPeople=2, PlannedStart=new DateTime(2026,4,25), PlannedEnd=new DateTime(2026,5,13), Status="Scheduled", ReadyForScheduling=true, CreatedBy="dev.seed" };
        var wpA4 = new WorkPackage { CompanyCode="CSL", PlanId=null, WorkOrderId=woA.WorkOrderId, SourceType="StepOutPlan", Title="Boilermaker Support — Heavy Lift Assist", CraftCode="BM", RequiredPeople=2, PlannedStart=new DateTime(2026,3,15), PlannedEnd=new DateTime(2026,4,20), Status="Draft", ReadyForScheduling=false, Notes="Waiting on crane schedule confirmation.", CreatedBy="dev.seed" };

        sopA.WorkPackages.Add(wpA1); sopA.WorkPackages.Add(wpA2); sopA.WorkPackages.Add(wpA3); sopA.WorkPackages.Add(wpA4);
        db.StepOutPlans.Add(sopA);
        await db.SaveChangesAsync();

        // FCO A-001
        var fcoA = new FcoDocument
        {
            CompanyCode="CSL", LinkedEstimateId=estA.EstimateId, LinkedWorkOrderId=woA.WorkOrderId,
            FcoNumber="FCO-DEMO-A-001", Title="Additional Sleeve Repair at V-401",
            Date=new DateTime(2026,3,25),
            RequestedBy="James Hartley", PreparedBy="estimator.csl",
            ClientName="Shell Oil Company", ContractorName="CSL Industrial Services",
            ProjectName="Shell Deer Park Unit 4 Piping Repair",
            ScopeDescription="Additional 18\" sleeve repair required at valve V-401 due to pitting corrosion discovered during inspection. Not included in original scope.",
            Reason="Hidden defect — not visible during pre-job inspection.",
            ScheduleImpactDays=5, RevisedCompletionDate=new DateTime(2026,5,5),
            MarkupPct=0.12m, TaxPct=0m, TotalFcoAmount=85_000m,
            UpdatedContractValue=1_335_000m,
            Status="Approved",
            ClientApprovalName="Alex Porter", ClientApprovalDate=new DateTime(2026,3,28),
            ContractorApprovalName="Mark Ellis", ContractorApprovalDate=new DateTime(2026,3,27),
            CreatedBy="dev.seed"
        };
        db.FcoDocuments.Add(fcoA);
        await db.SaveChangesAsync();

        db.FcoLaborLines.AddRange(
            new FcoLaborLine { FcoDocumentId=fcoA.FcoDocumentId, Position="Pipefitter Journeyman", LaborType="Direct", CraftCode="PF", NavCode="PF001", StHours=80, OtHours=20, DtHours=0, BillStRate=78m, BillOtRate=117m, BillDtRate=156m, Subtotal=80*78m+20*117m, SortOrder=1 },
            new FcoLaborLine { FcoDocumentId=fcoA.FcoDocumentId, Position="Welder Journeyman",     LaborType="Direct", CraftCode="WD", NavCode="WD001", StHours=120, OtHours=40, DtHours=0, BillStRate=85m, BillOtRate=127.5m, BillDtRate=170m, Subtotal=120*85m+40*127.5m, SortOrder=2 },
            new FcoLaborLine { FcoDocumentId=fcoA.FcoDocumentId, Position="General Foreman",       LaborType="Indirect", CraftCode="SUP", NavCode="GF001", StHours=40, OtHours=10, DtHours=0, BillStRate=98m, BillOtRate=147m, BillDtRate=196m, Subtotal=40*98m+10*147m, SortOrder=3 }
        );

        // Actual Entries A
        var actualDates = new[] { new DateTime(2026,3,1), new DateTime(2026,3,10), new DateTime(2026,3,20), new DateTime(2026,4,1), new DateTime(2026,4,10) };
        foreach (var d in actualDates)
        {
            db.ActualEntries.Add(new ActualEntry { CompanyCode="CSL", WorkOrderId=woA.WorkOrderId, ActualType="Labor", ActualDate=d, Position="Pipefitter Journeyman", CraftCode="PF", StHours=5*8m, OtHours=5*4m, DtHours=0, CostAmount=5*8m*47m+5*4m*70.5m, BillableAmount=5*8m*78m+5*4m*117m, BilledAmount=5*8m*78m+5*4m*117m, IsConfirmed=true, EnteredBy="dev.seed" });
            db.ActualEntries.Add(new ActualEntry { CompanyCode="CSL", WorkOrderId=woA.WorkOrderId, ActualType="Labor", ActualDate=d, Position="Welder Journeyman", CraftCode="WD", StHours=3*8m, OtHours=3*4m, DtHours=0, CostAmount=3*8m*51m+3*4m*76.5m, BillableAmount=3*8m*85m+3*4m*127.5m, BilledAmount=3*8m*85m+3*4m*127.5m, IsConfirmed=d < today.AddDays(-7), EnteredBy="dev.seed" });
        }

        // Assignments A (against Released WPs)
        if (resMap.Count > 0)
        {
            void AddAssignment(string empId, int wpId, string wpName, string craft, DateTime start, DateTime end)
            {
                if (!resMap.TryGetValue(empId, out var rid)) return;
                db.Assignments.Add(new Assignment { CompanyCode="CSL", ResourceId=rid, JobSourceType="WorkPackage", JobSourceId=wpId, JobName=wpName, CraftCode=craft, Start=start, End=end, Shift="Day", Status="Confirmed", CreatedBy="dev.seed" });
            }
            var aEnd = new DateTime(2026, 5, 15);
            AddAssignment("CSL-001", wpA1.PackageId, wpA1.Title, "SUP", new DateTime(2026,2,15), aEnd);
            AddAssignment("CSL-002", wpA1.PackageId, wpA1.Title, "PF",  new DateTime(2026,2,15), aEnd);
            AddAssignment("CSL-003", wpA1.PackageId, wpA1.Title, "PF",  new DateTime(2026,2,15), aEnd);
            AddAssignment("CSL-004", wpA1.PackageId, wpA1.Title, "PF",  new DateTime(2026,2,15), aEnd);
            AddAssignment("CSL-005", wpA2.PackageId, wpA2.Title, "WD",  new DateTime(2026,4,10), aEnd);
            AddAssignment("CSL-006", wpA2.PackageId, wpA2.Title, "WD",  new DateTime(2026,4,10), aEnd);
            AddAssignment("CSL-007", wpA1.PackageId, wpA1.Title, "BM",  new DateTime(2026,3,15), new DateTime(2026,4,20));
            AddAssignment("CSL-008", wpA1.PackageId, wpA1.Title, "RIG", new DateTime(2026,3,1),  new DateTime(2026,4,15));
            AddAssignment("CSL-009", wpA1.PackageId, wpA1.Title, "SAF", new DateTime(2026,2,15), aEnd);
            AddAssignment("CSL-010", wpA3.PackageId, wpA3.Title, "NDT", new DateTime(2026,4,25), aEnd);
        }

        await db.SaveChangesAsync();

        // ═══════════════════════════════════════════════════════════════════════
        // SCENARIO B — Cheniere LNG Terminal Expansion (Planning / Forecast)
        // ═══════════════════════════════════════════════════════════════════════

        var estB = new Estimate
        {
            CompanyCode="CSL", EstimateNumber="26-0051-CHEN",
            Name="Cheniere LNG - Terminal Expansion Piping",
            Client="Cheniere Energy", ClientCode="CHEN",
            JobType="Construction", Branch="Industrial", City="Sabine Pass", State="TX",
            Site="Sabine Pass LNG Terminal", Shift="Day", HoursPerShift=10,
            VP="Mark Ellis", Director="Rachel Kim", Region="Gulf",
            Days=61, StartDate=new DateTime(2026,8,1), EndDate=new DateTime(2026,9,30),
            OtMethod="daily8_weekly40", DtWeekends="none",
            Status="Awarded", ConfidencePct=100, IsScenario=false,
            CreatedBy="dev.seed", UpdatedBy="dev.seed",
            CreatedAt=DateTimeOffset.UtcNow.AddDays(-45), UpdatedAt=DateTimeOffset.UtcNow.AddDays(-10),
            Summary=new EstimateSummary { BillSubtotal=2_400_000m, GrandTotal=2_400_000m, InternalCostTotal=1_680_000m, GrossProfit=720_000m, GrossMarginPct=30.0m, DiscountType="None" }
        };
        db.Estimates.Add(estB);
        await db.SaveChangesAsync();

        var caB = new CommercialAuthorization
        {
            CompanyCode="CSL", EstimateId=estB.EstimateId,
            AuthorizationType="NTP", AuthorizationNumber="NTP-CHEN-2026-0112",
            AuthorizedValue=2_400_000m, AuthorizedBy="Cheniere EPC Procurement",
            ReceivedDate=new DateTime(2026,4,15), EffectiveDate=new DateTime(2026,5,1),
            ExpirationDate=new DateTime(2026,12,31), Status="Active",
            Notes="NTP issued for Sabine Pass LNG terminal expansion piping scope. Mobilization target Aug 1.",
            CreatedBy="dev.seed"
        };
        db.CommercialAuthorizations.Add(caB);
        await db.SaveChangesAsync();

        var projB = new Project
        {
            CompanyCode="CSL", ProjectNumber="PRJ-DEMO-B",
            Name="Cheniere LNG Terminal Expansion Piping",
            EstimateId=estB.EstimateId, CommercialAuthorizationId=caB.CommercialAuthorizationId,
            Client="Cheniere Energy", ClientCode="CHEN",
            Site="Sabine Pass LNG Terminal", City="Sabine Pass", State="TX", JobLetter="B",
            PlannedStart=new DateTime(2026,8,1), PlannedEnd=new DateTime(2026,9,30),
            Status="Planning", AtRiskThresholdDays=7,
            CreatedBy="dev.seed"
        };

        var phB1 = new ProjectPhase { Name="Pre-Mobilization", Status="Planning", SortOrder=1, PlannedStart=new DateTime(2026,7,1), PlannedEnd=new DateTime(2026,7,31) };
        phB1.Tasks.Add(new PlanTask { Title="Finalize subcontractor scope packages", TaskType="Task", Status="Pending", PercentComplete=0, DurationDays=10, SortOrder=1, CreatedBy="dev.seed", PlannedStart=new DateTime(2026,7,1), PlannedEnd=new DateTime(2026,7,10) });
        phB1.Tasks.Add(new PlanTask { Title="Procure long-lead piping materials", TaskType="Task", Status="Pending", PercentComplete=0, DurationDays=21, SortOrder=2, CreatedBy="dev.seed", PlannedStart=new DateTime(2026,7,1), PlannedEnd=new DateTime(2026,7,22) });

        var phB2 = new ProjectPhase { Name="LNG Header Piping Installation", Status="Planning", SortOrder=2, PlannedStart=new DateTime(2026,8,1), PlannedEnd=new DateTime(2026,9,15) };
        phB2.Tasks.Add(new PlanTask { Title="Mobilize crew and equipment to Sabine Pass", TaskType="Task", Status="Pending", PercentComplete=0, DurationDays=5, SortOrder=1, CreatedBy="dev.seed", PlannedStart=new DateTime(2026,8,1), PlannedEnd=new DateTime(2026,8,5) });
        phB2.Tasks.Add(new PlanTask { Title="Install primary LNG header spools — Trains 1–3", TaskType="Task", Status="Pending", PercentComplete=0, DurationDays=30, SortOrder=2, CreatedBy="dev.seed", PlannedStart=new DateTime(2026,8,8), PlannedEnd=new DateTime(2026,9,7) });
        phB2.Tasks.Add(new PlanTask { Title="Major equipment lift — LNG compressor skid", TaskType="Milestone", Status="Pending", PercentComplete=0, DurationDays=1, SortOrder=3, IsMilestone=true, CreatedBy="dev.seed", PlannedStart=new DateTime(2026,8,20), PlannedEnd=new DateTime(2026,8,20) });

        var phB3 = new ProjectPhase { Name="Testing & Commissioning", Status="Planning", SortOrder=3, PlannedStart=new DateTime(2026,9,15), PlannedEnd=new DateTime(2026,9,30) };

        projB.Phases.Add(phB1); projB.Phases.Add(phB2); projB.Phases.Add(phB3);
        projB.Milestones.Add(new Milestone { Name="Crew Mobilization", PlannedDate=new DateTime(2026,8,1), Status="Pending", IsDeadline=true, CreatedBy="dev.seed" });
        projB.Milestones.Add(new Milestone { Name="Major Equipment Lift", PlannedDate=new DateTime(2026,8,20), Status="Pending", IsCritical=true, CreatedBy="dev.seed" });
        projB.Milestones.Add(new Milestone { Name="Piping Installation Complete", PlannedDate=new DateTime(2026,9,15), Status="Pending", IsCritical=true, CreatedBy="dev.seed" });
        db.Projects.Add(projB);
        await db.SaveChangesAsync();

        var woB = new WorkOrder
        {
            CompanyCode="CSL", WorkOrderNumber="WO-DEMO-B-001",
            ProjectId=projB.ProjectId, EstimateId=estB.EstimateId, CommercialAuthorizationId=caB.CommercialAuthorizationId,
            Title="LNG Terminal Expansion — Main Piping Scope",
            Description="Install primary LNG header spools and tie-in piping for Trains 1–3 expansion.",
            AuthorizedValue=0m,
            PlannedStart=new DateTime(2026,8,1), PlannedEnd=new DateTime(2026,9,30),
            Status="Draft", CreatedBy="dev.seed"
        };
        db.WorkOrders.Add(woB);
        await db.SaveChangesAsync();

        // Forecast WorkPackages B — NOT released, NOT assignable
        var sopB = new StepOutPlan { CompanyCode="CSL", Name="LNG Terminal Expansion — Draft SOP", SourceType="Estimate", LinkedEstimateId=estB.EstimateId, Client="Cheniere Energy", Site="Sabine Pass LNG Terminal", PlannedStart=new DateTime(2026,8,1), PlannedEnd=new DateTime(2026,9,30), Status="Draft", WorkOrderId=woB.WorkOrderId, CreatedBy="dev.seed" };
        var sB1 = new StepOutStep { SortOrder=1, StepCode="1", Title="Mobilize and establish site", Status="Pending", CraftCode="SUP", RequiredPeople=3, DurationHours=40 };
        var sB2 = new StepOutStep { SortOrder=2, StepCode="2", Title="Install LNG header spools — Trains 1–3", Status="Pending", CraftCode="PF", RequiredPeople=8, DurationHours=480 };
        var sB3 = new StepOutStep { SortOrder=3, StepCode="3", Title="Welding and inspection", Status="Pending", CraftCode="WD", RequiredPeople=4, DurationHours=240 };
        sopB.Steps.Add(sB1); sopB.Steps.Add(sB2); sopB.Steps.Add(sB3);
        sopB.WorkPackages.Add(new WorkPackage { CompanyCode="CSL", WorkOrderId=woB.WorkOrderId, SourceType="StepOutPlan", Title="Piping Crew — LNG Header Trains 1–3", CraftCode="PF", RequiredPeople=8, PlannedStart=new DateTime(2026,8,1), PlannedEnd=new DateTime(2026,9,15), Status="Draft", ReadyForScheduling=false, CreatedBy="dev.seed" });
        sopB.WorkPackages.Add(new WorkPackage { CompanyCode="CSL", WorkOrderId=woB.WorkOrderId, SourceType="StepOutPlan", Title="Welding Crew — LNG Spool Welds", CraftCode="WD", RequiredPeople=4, PlannedStart=new DateTime(2026,8,15), PlannedEnd=new DateTime(2026,9,10), Status="Draft", ReadyForScheduling=false, CreatedBy="dev.seed" });
        sopB.WorkPackages.Add(new WorkPackage { CompanyCode="CSL", WorkOrderId=woB.WorkOrderId, SourceType="StepOutPlan", Title="Boilermaker Crew — Heavy Component Support", CraftCode="BM", RequiredPeople=4, PlannedStart=new DateTime(2026,8,8), PlannedEnd=new DateTime(2026,9,5), Status="Draft", ReadyForScheduling=false, CreatedBy="dev.seed" });
        db.StepOutPlans.Add(sopB);
        await db.SaveChangesAsync();

        // ═══════════════════════════════════════════════════════════════════════
        // SCENARIO C — BP Texas City Exchanger Bundle Pull (Closed / Historical)
        // ═══════════════════════════════════════════════════════════════════════

        var estC = new Estimate
        {
            CompanyCode="CSL", EstimateNumber="26-0052-BP",
            Name="BP Texas City — Exchanger Bundle Pull & Repair",
            Client="British Petroleum", ClientCode="BP", MsaNumber="MSA-BP-2024-01",
            JobType="Turnaround", Branch="Industrial", City="Texas City", State="TX",
            Site="BP Texas City Refinery", Shift="Day", HoursPerShift=10,
            VP="David Torres", Director="Rachel Kim", Region="Gulf",
            Days=49, StartDate=new DateTime(2025,1,15), EndDate=new DateTime(2025,3,5),
            OtMethod="daily8_weekly40", DtWeekends="none",
            Status="Awarded", ConfidencePct=100, IsScenario=false,
            CreatedBy="dev.seed", UpdatedBy="dev.seed",
            CreatedAt=DateTimeOffset.UtcNow.AddDays(-480), UpdatedAt=DateTimeOffset.UtcNow.AddDays(-400),
            Summary=new EstimateSummary { BillSubtotal=780_000m, GrandTotal=780_000m, InternalCostTotal=554_100m, GrossProfit=225_900m, GrossMarginPct=28.96m, DiscountType="None" }
        };
        db.Estimates.Add(estC);
        await db.SaveChangesAsync();

        var caC = new CommercialAuthorization
        {
            CompanyCode="CSL", EstimateId=estC.EstimateId,
            AuthorizationType="PO", AuthorizationNumber="PO-BP-2025-0087",
            AuthorizedValue=780_000m, AuthorizedBy="BP Procurement — Texas City",
            ReceivedDate=new DateTime(2024,12,20), EffectiveDate=new DateTime(2025,1,1),
            ExpirationDate=new DateTime(2025,6,30), Status="Closed",
            Notes="Closed after project completion Mar 2025. Two FCOs approved during execution.",
            CreatedBy="dev.seed"
        };
        db.CommercialAuthorizations.Add(caC);
        await db.SaveChangesAsync();

        var projC = new Project
        {
            CompanyCode="CSL", ProjectNumber="PRJ-DEMO-C",
            Name="BP Texas City Exchanger Bundle Pull & Repair",
            EstimateId=estC.EstimateId, CommercialAuthorizationId=caC.CommercialAuthorizationId,
            Client="British Petroleum", ClientCode="BP",
            Site="BP Texas City Refinery", City="Texas City", State="TX", JobLetter="C",
            PlannedStart=new DateTime(2025,1,15), PlannedEnd=new DateTime(2025,2,28),
            ActualStart=new DateTime(2025,1,15), ActualEnd=new DateTime(2025,3,5),
            Status="Closed", AtRiskThresholdDays=3,
            LessonsLearnedNotes="Unit 5 exchanger shell had additional fouling not visible during pre-job inspection. Recommend pre-TA borescope on all like units. FCO process was smooth — Shell template used. Budget 3 additional days for similar bundle scopes as standard practice.",
            CreatedBy="dev.seed"
        };

        var phC1 = new ProjectPhase { Name="Mobilization", Status="Complete", SortOrder=1, Color="#22c55e", PlannedStart=new DateTime(2025,1,15), PlannedEnd=new DateTime(2025,1,18), ActualStart=new DateTime(2025,1,15), ActualEnd=new DateTime(2025,1,18) };
        phC1.Tasks.Add(new PlanTask { Title="Mobilize crew and rigging equipment", TaskType="Task", Status="Complete", PercentComplete=100, DurationDays=3, SortOrder=1, CreatedBy="dev.seed", PlannedStart=new DateTime(2025,1,15), PlannedEnd=new DateTime(2025,1,18), ActualStart=new DateTime(2025,1,15), ActualEnd=new DateTime(2025,1,18) });

        var phC2 = new ProjectPhase { Name="Bundle Pull & Inspection", Status="Complete", SortOrder=2, Color="#22c55e", PlannedStart=new DateTime(2025,1,19), PlannedEnd=new DateTime(2025,2,10), ActualStart=new DateTime(2025,1,19), ActualEnd=new DateTime(2025,2,12) };
        phC2.Tasks.Add(new PlanTask { Title="Prepare and rig exchanger head", TaskType="Task", Status="Complete", PercentComplete=100, DurationDays=5, SortOrder=1, CreatedBy="dev.seed", PlannedStart=new DateTime(2025,1,19), PlannedEnd=new DateTime(2025,1,24), ActualStart=new DateTime(2025,1,19), ActualEnd=new DateTime(2025,1,24) });
        phC2.Tasks.Add(new PlanTask { Title="Extract bundle — Unit 5 exchanger", TaskType="Task", Status="Complete", PercentComplete=100, DurationDays=7, SortOrder=2, CreatedBy="dev.seed", PlannedStart=new DateTime(2025,1,25), PlannedEnd=new DateTime(2025,2,1), ActualStart=new DateTime(2025,1,25), ActualEnd=new DateTime(2025,2,3) });
        phC2.Tasks.Add(new PlanTask { Title="Shell and tube inspection", TaskType="Task", Status="Complete", PercentComplete=100, DurationDays=8, SortOrder=3, CreatedBy="dev.seed", PlannedStart=new DateTime(2025,2,2), PlannedEnd=new DateTime(2025,2,10), ActualStart=new DateTime(2025,2,4), ActualEnd=new DateTime(2025,2,12) });

        var phC3 = new ProjectPhase { Name="Repair & Cleaning", Status="Complete", SortOrder=3, Color="#22c55e", PlannedStart=new DateTime(2025,2,11), PlannedEnd=new DateTime(2025,2,21), ActualStart=new DateTime(2025,2,13), ActualEnd=new DateTime(2025,2,22) };
        phC3.Tasks.Add(new PlanTask { Title="High-pressure water jet cleaning", TaskType="Task", Status="Complete", PercentComplete=100, DurationDays=5, SortOrder=1, CreatedBy="dev.seed", PlannedStart=new DateTime(2025,2,11), PlannedEnd=new DateTime(2025,2,15), ActualStart=new DateTime(2025,2,13), ActualEnd=new DateTime(2025,2,17) });
        phC3.Tasks.Add(new PlanTask { Title="Shell repair — tube sheet weld repair (FCO scope)", TaskType="Task", Status="Complete", PercentComplete=100, DurationDays=6, SortOrder=2, CreatedBy="dev.seed", PlannedStart=new DateTime(2025,2,15), PlannedEnd=new DateTime(2025,2,21), ActualStart=new DateTime(2025,2,18), ActualEnd=new DateTime(2025,2,22) });

        var phC4 = new ProjectPhase { Name="Reinstall & Test", Status="Complete", SortOrder=4, Color="#22c55e", PlannedStart=new DateTime(2025,2,22), PlannedEnd=new DateTime(2025,2,28), ActualStart=new DateTime(2025,2,23), ActualEnd=new DateTime(2025,3,5) };
        phC4.Tasks.Add(new PlanTask { Title="Bundle reinstall and alignment", TaskType="Task", Status="Complete", PercentComplete=100, DurationDays=4, SortOrder=1, CreatedBy="dev.seed", PlannedStart=new DateTime(2025,2,22), PlannedEnd=new DateTime(2025,2,25), ActualStart=new DateTime(2025,2,23), ActualEnd=new DateTime(2025,2,28) });
        phC4.Tasks.Add(new PlanTask { Title="Pressure test and leak check", TaskType="Task", Status="Complete", PercentComplete=100, DurationDays=2, SortOrder=2, CreatedBy="dev.seed", PlannedStart=new DateTime(2025,2,26), PlannedEnd=new DateTime(2025,2,28), ActualStart=new DateTime(2025,3,1), ActualEnd=new DateTime(2025,3,3) });
        phC4.Tasks.Add(new PlanTask { Title="Client walkdown and sign-off", TaskType="Gate", Status="Complete", PercentComplete=100, DurationDays=1, SortOrder=3, CreatedBy="dev.seed", PlannedStart=new DateTime(2025,2,28), PlannedEnd=new DateTime(2025,2,28), ActualStart=new DateTime(2025,3,5), ActualEnd=new DateTime(2025,3,5) });

        projC.Phases.Add(phC1); projC.Phases.Add(phC2); projC.Phases.Add(phC3); projC.Phases.Add(phC4);
        projC.Milestones.Add(new Milestone { Name="Bundle Extracted", PlannedDate=new DateTime(2025,2,1), ActualDate=new DateTime(2025,2,3), Status="Achieved", CreatedBy="dev.seed" });
        projC.Milestones.Add(new Milestone { Name="Inspection Complete", PlannedDate=new DateTime(2025,2,10), ActualDate=new DateTime(2025,2,12), Status="Achieved", CreatedBy="dev.seed" });
        projC.Milestones.Add(new Milestone { Name="Bundle Reinstalled", PlannedDate=new DateTime(2025,2,28), ActualDate=new DateTime(2025,3,3), Status="Achieved", CreatedBy="dev.seed" });
        projC.Milestones.Add(new Milestone { Name="Client Sign-Off", PlannedDate=new DateTime(2025,2,28), ActualDate=new DateTime(2025,3,5), Status="Achieved", IsDeadline=true, CreatedBy="dev.seed" });
        db.Projects.Add(projC);
        await db.SaveChangesAsync();

        var woC = new WorkOrder
        {
            CompanyCode="CSL", WorkOrderNumber="WO-DEMO-C-001",
            ProjectId=projC.ProjectId, EstimateId=estC.EstimateId, CommercialAuthorizationId=caC.CommercialAuthorizationId,
            Title="Exchanger Bundle Pull & Repair — Complete Scope",
            Description="Complete exchanger bundle pull, inspection, tube sheet repair, and reinstall for Unit 5.",
            AuthorizedValue=823_000m,
            PlannedStart=new DateTime(2025,1,15), PlannedEnd=new DateTime(2025,2,28),
            ActualStart=new DateTime(2025,1,15), ActualEnd=new DateTime(2025,3,5),
            Status="Closed", ReleasedBy="estimator.csl",
            ReleasedAt=new DateTimeOffset(new DateTime(2025,1,14), TimeSpan.Zero),
            CreatedBy="dev.seed"
        };
        db.WorkOrders.Add(woC);
        await db.SaveChangesAsync();

        var sopC = new StepOutPlan { CompanyCode="CSL", Name="Exchanger Bundle Pull & Repair — Execution SOP", SourceType="Estimate", LinkedEstimateId=estC.EstimateId, Client="British Petroleum", Site="BP Texas City Refinery", PlannedStart=new DateTime(2025,1,15), PlannedEnd=new DateTime(2025,2,28), Status="Complete", WorkOrderId=woC.WorkOrderId, CreatedBy="dev.seed" };
        var sC1 = new StepOutStep { SortOrder=1, StepCode="1", Title="Rig and extract exchanger bundle", Status="Complete", CraftCode="RIG", RequiredPeople=4, DurationHours=56 };
        sC1.SubStepLeafs.Add(new StepOutSubStep { SubStepCode="1.1", SortOrder=1, Title="Rig crane and prepare lift plan", Status="Complete", CraftCode="RIG", RequiredPeople=2, DurationHours=8, ActualDurationHours=8 });
        sC1.SubStepLeafs.Add(new StepOutSubStep { SubStepCode="1.2", SortOrder=2, Title="Extract bundle and stage for inspection", Status="Complete", CraftCode="RIG", RequiredPeople=4, DurationHours=48, ActualDurationHours=52 });
        var sC2 = new StepOutStep { SortOrder=2, StepCode="2", Title="Inspect, clean, and repair", Status="Complete", CraftCode="BM", RequiredPeople=4, DurationHours=120 };
        sC2.SubStepLeafs.Add(new StepOutSubStep { SubStepCode="2.1", SortOrder=1, Title="High-pressure water jet cleaning", Status="Complete", CraftCode="BM", RequiredPeople=3, DurationHours=40, ActualDurationHours=40 });
        sC2.SubStepLeafs.Add(new StepOutSubStep { SubStepCode="2.2", SortOrder=2, Title="Tube sheet and shell weld repairs (FCO scope)", Status="Complete", CraftCode="WD", RequiredPeople=2, DurationHours=80, ActualDurationHours=96 });
        var sC3 = new StepOutStep { SortOrder=3, StepCode="3", Title="Reinstall and pressure test", Status="Complete", CraftCode="PF", RequiredPeople=4, DurationHours=48 };
        sC3.SubStepLeafs.Add(new StepOutSubStep { SubStepCode="3.1", SortOrder=1, Title="Reinstall bundle and align", Status="Complete", CraftCode="PF", RequiredPeople=4, DurationHours=32, ActualDurationHours=36 });
        sC3.SubStepLeafs.Add(new StepOutSubStep { SubStepCode="3.2", SortOrder=2, Title="Pressure test and client sign-off", Status="Complete", CraftCode="PF", RequiredPeople=2, DurationHours=16, ActualDurationHours=16 });
        sopC.Steps.Add(sC1); sopC.Steps.Add(sC2); sopC.Steps.Add(sC3);
        sopC.WorkPackages.Add(new WorkPackage { CompanyCode="CSL", WorkOrderId=woC.WorkOrderId, SourceType="StepOutPlan", Title="Bundle Pull / Rigging Crew", CraftCode="RIG", RequiredPeople=4, PlannedStart=new DateTime(2025,1,19), PlannedEnd=new DateTime(2025,2,5), Status="Complete", ReadyForScheduling=false, CreatedBy="dev.seed" });
        sopC.WorkPackages.Add(new WorkPackage { CompanyCode="CSL", WorkOrderId=woC.WorkOrderId, SourceType="StepOutPlan", Title="Boilermaker / Welder Repair Crew", CraftCode="BM", RequiredPeople=4, PlannedStart=new DateTime(2025,2,13), PlannedEnd=new DateTime(2025,3,3), Status="Complete", ReadyForScheduling=false, CreatedBy="dev.seed" });
        db.StepOutPlans.Add(sopC);
        await db.SaveChangesAsync();

        // FCOs for C
        var fcoC1 = new FcoDocument
        {
            CompanyCode="CSL", LinkedEstimateId=estC.EstimateId, LinkedWorkOrderId=woC.WorkOrderId,
            FcoNumber="FCO-DEMO-C-001", Title="Tube Sheet Weld Repair — Additional Scope",
            Date=new DateTime(2025,2,12), RequestedBy="Project Manager", PreparedBy="estimator.csl",
            ClientName="British Petroleum", ContractorName="CSL Industrial Services",
            ProjectName="BP Texas City Exchanger Bundle Pull",
            ScopeDescription="Tube sheet had unexpected pitting corrosion requiring full weld overlay. Not visible during pre-job scope development.",
            Reason="Hidden defect discovered during inspection phase.",
            ScheduleImpactDays=3, MarkupPct=0.10m, TaxPct=0m, TotalFcoAmount=45_000m,
            UpdatedContractValue=825_000m, Status="Signed",
            ClientApprovalName="BP Plant Manager", ClientApprovalDate=new DateTime(2025,2,14),
            ContractorApprovalName="David Torres", ContractorApprovalDate=new DateTime(2025,2,13),
            CreatedBy="dev.seed"
        };
        var fcoC2 = new FcoDocument
        {
            CompanyCode="CSL", LinkedEstimateId=estC.EstimateId, LinkedWorkOrderId=woC.WorkOrderId,
            FcoNumber="FCO-DEMO-C-002", Title="Additional Insulation Restoration",
            Date=new DateTime(2025,2,25), RequestedBy="Project Manager", PreparedBy="estimator.csl",
            ClientName="British Petroleum", ContractorName="CSL Industrial Services",
            ProjectName="BP Texas City Exchanger Bundle Pull",
            ScopeDescription="Insulation removal required for full shell inspection was larger than estimated. Restoring all removed insulation added to scope.",
            Reason="Scope growth from expanded inspection access.",
            ScheduleImpactDays=2, MarkupPct=0.10m, TaxPct=0m, TotalFcoAmount=28_000m,
            UpdatedContractValue=853_000m, Status="Signed",
            ClientApprovalName="BP Plant Manager", ClientApprovalDate=new DateTime(2025,2,27),
            ContractorApprovalName="David Torres", ContractorApprovalDate=new DateTime(2025,2,26),
            CreatedBy="dev.seed"
        };
        db.FcoDocuments.AddRange(fcoC1, fcoC2);
        await db.SaveChangesAsync();

        db.FcoLaborLines.AddRange(
            new FcoLaborLine { FcoDocumentId=fcoC1.FcoDocumentId, Position="Welder Journeyman", LaborType="Direct", CraftCode="WD", NavCode="WD001", StHours=160, OtHours=40, DtHours=0, BillStRate=85m, BillOtRate=127.5m, BillDtRate=170m, Subtotal=160*85m+40*127.5m, SortOrder=1 },
            new FcoLaborLine { FcoDocumentId=fcoC1.FcoDocumentId, Position="Boilermaker Journeyman", LaborType="Direct", CraftCode="BM", NavCode="BM001", StHours=80, OtHours=20, DtHours=0, BillStRate=82m, BillOtRate=123m, BillDtRate=164m, Subtotal=80*82m+20*123m, SortOrder=2 },
            new FcoLaborLine { FcoDocumentId=fcoC2.FcoDocumentId, Position="Pipefitter Journeyman", LaborType="Direct", CraftCode="PF", NavCode="PF001", StHours=80, OtHours=20, DtHours=0, BillStRate=78m, BillOtRate=117m, BillDtRate=156m, Subtotal=80*78m+20*117m, SortOrder=1 }
        );

        // Fully confirmed actuals for C
        var cActuals = new[]
        {
            (new DateTime(2025,1,19), "Rigger",              "RIG", 4*8m,  4*4m,  4*40m,  4*60m),
            (new DateTime(2025,2,1),  "Rigger",              "RIG", 4*8m,  4*4m,  4*40m,  4*60m),
            (new DateTime(2025,2,13), "Boilermaker",         "BM",  4*8m,  4*4m,  4*49m,  4*73.5m),
            (new DateTime(2025,2,18), "Welder Journeyman",   "WD",  2*8m,  2*4m,  2*51m,  2*76.5m),
            (new DateTime(2025,2,20), "Welder Journeyman",   "WD",  2*8m,  2*4m,  2*51m,  2*76.5m),
            (new DateTime(2025,2,23), "Pipefitter Journeyman","PF", 4*8m,  4*4m,  4*47m,  4*70.5m),
            (new DateTime(2025,3,1),  "Pipefitter Journeyman","PF", 4*8m,  4*4m,  4*47m,  4*70.5m),
            (new DateTime(2025,3,3),  "Boilermaker",         "BM",  2*8m,  2*4m,  2*49m,  2*73.5m),
        };
        foreach (var (d, pos, cc, stH, otH, costSt, billSt) in cActuals)
            db.ActualEntries.Add(new ActualEntry { CompanyCode="CSL", WorkOrderId=woC.WorkOrderId, ActualType="Labor", ActualDate=d, Position=pos, CraftCode=cc, StHours=stH, OtHours=otH, DtHours=0, CostAmount=stH*costSt/8m+otH*(costSt*1.5m)/8m, BillableAmount=stH*billSt/8m+otH*(billSt*1.5m)/8m, BilledAmount=stH*billSt/8m+otH*(billSt*1.5m)/8m, IsConfirmed=true, EnteredBy="dev.seed" });

        // Baseline snapshot for C
        db.TimelineBaselines.Add(new TimelineBaseline { ProjectId=projC.ProjectId, EntityType="Project", EntityId=projC.ProjectId, SnapshotDate=new DateTime(2025,1,14), PlannedStart=new DateTime(2025,1,15), PlannedEnd=new DateTime(2025,2,28), BaselineLabel="Initial Baseline", BaselineReason="Locked at project kickoff.", LockedBy="estimator.csl", LockedAt=new DateTimeOffset(new DateTime(2025,1,14), TimeSpan.Zero) });
        await db.SaveChangesAsync();

        // ═══════════════════════════════════════════════════════════════════════
        // SCENARIO D — Valero Port Arthur (Upcoming Released)
        // ═══════════════════════════════════════════════════════════════════════

        var estD = new Estimate
        {
            CompanyCode="CSL", EstimateNumber="26-0053-VLO",
            Name="Valero Port Arthur — Unit 8 Turnaround Piping",
            Client="Valero Energy", ClientCode="VLO",
            JobType="Turnaround", Branch="Industrial", City="Port Arthur", State="TX",
            Site="Valero Port Arthur Refinery", Shift="Day", HoursPerShift=10,
            VP="Mark Ellis", Director="Rachel Kim", Region="Gulf",
            Days=44, StartDate=new DateTime(2026,9,1), EndDate=new DateTime(2026,10,15),
            OtMethod="daily8_weekly40", DtWeekends="none",
            Status="Awarded", ConfidencePct=100, IsScenario=false,
            CreatedBy="dev.seed", UpdatedBy="dev.seed",
            CreatedAt=DateTimeOffset.UtcNow.AddDays(-30), UpdatedAt=DateTimeOffset.UtcNow.AddDays(-5),
            Summary=new EstimateSummary { BillSubtotal=650_000m, GrandTotal=650_000m, InternalCostTotal=455_000m, GrossProfit=195_000m, GrossMarginPct=30.0m, DiscountType="None" }
        };
        db.Estimates.Add(estD);
        await db.SaveChangesAsync();

        var caD = new CommercialAuthorization
        {
            CompanyCode="CSL", EstimateId=estD.EstimateId,
            AuthorizationType="WorkAuthorization", AuthorizationNumber="WA-VLO-2026-0559",
            AuthorizedValue=650_000m, AuthorizedBy="Valero Turnaround Procurement",
            ReceivedDate=new DateTime(2026,4,20), EffectiveDate=new DateTime(2026,5,1),
            ExpirationDate=new DateTime(2026,12,31), Status="Active",
            Notes="Work authorization for Valero PA Unit 8 piping scope. Mobilization Sep 1.",
            CreatedBy="dev.seed"
        };
        db.CommercialAuthorizations.Add(caD);
        await db.SaveChangesAsync();

        var projD = new Project
        {
            CompanyCode="CSL", ProjectNumber="PRJ-DEMO-D",
            Name="Valero Port Arthur Unit 8 Turnaround Piping",
            EstimateId=estD.EstimateId, CommercialAuthorizationId=caD.CommercialAuthorizationId,
            Client="Valero Energy", ClientCode="VLO",
            Site="Valero Port Arthur Refinery", City="Port Arthur", State="TX", JobLetter="D",
            PlannedStart=new DateTime(2026,9,1), PlannedEnd=new DateTime(2026,10,15),
            Status="Active", AtRiskThresholdDays=5,
            CreatedBy="dev.seed"
        };

        var phD1 = new ProjectPhase { Name="Mobilization", Status="Planning", SortOrder=1, PlannedStart=new DateTime(2026,9,1), PlannedEnd=new DateTime(2026,9,5) };
        phD1.Tasks.Add(new PlanTask { Title="Mobilize crew and equipment", TaskType="Task", Status="Pending", PercentComplete=0, DurationDays=4, SortOrder=1, CreatedBy="dev.seed", PlannedStart=new DateTime(2026,9,1), PlannedEnd=new DateTime(2026,9,4) });
        var phD2 = new ProjectPhase { Name="Unit 8 Piping Execution", Status="Planning", SortOrder=2, PlannedStart=new DateTime(2026,9,5), PlannedEnd=new DateTime(2026,10,10) };
        phD2.Tasks.Add(new PlanTask { Title="Unit 8 piping replacement — Spools 1–6", TaskType="Task", Status="Pending", PercentComplete=0, DurationDays=30, SortOrder=1, CreatedBy="dev.seed", PlannedStart=new DateTime(2026,9,5), PlannedEnd=new DateTime(2026,10,5) });
        phD2.Tasks.Add(new PlanTask { Title="Welding and PWHT", TaskType="Task", Status="Pending", PercentComplete=0, DurationDays=10, SortOrder=2, CreatedBy="dev.seed", PlannedStart=new DateTime(2026,9,20), PlannedEnd=new DateTime(2026,10,10) });
        var phD3 = new ProjectPhase { Name="Inspection & Demob", Status="Planning", SortOrder=3, PlannedStart=new DateTime(2026,10,10), PlannedEnd=new DateTime(2026,10,15) };

        projD.Phases.Add(phD1); projD.Phases.Add(phD2); projD.Phases.Add(phD3);
        projD.Milestones.Add(new Milestone { Name="Mobilization", PlannedDate=new DateTime(2026,9,1), Status="Pending", IsDeadline=true, CreatedBy="dev.seed" });
        projD.Milestones.Add(new Milestone { Name="Unit 8 Piping Complete", PlannedDate=new DateTime(2026,10,10), Status="Pending", IsCritical=true, CreatedBy="dev.seed" });
        db.Projects.Add(projD);
        await db.SaveChangesAsync();

        var woD = new WorkOrder
        {
            CompanyCode="CSL", WorkOrderNumber="WO-DEMO-D-001",
            ProjectId=projD.ProjectId, EstimateId=estD.EstimateId, CommercialAuthorizationId=caD.CommercialAuthorizationId,
            Title="Valero PA Unit 8 Turnaround — Main Scope",
            Description="Replace Unit 8 piping spools 1–6 and PWHT per Valero spec.",
            AuthorizedValue=620_000m,
            PlannedStart=new DateTime(2026,9,1), PlannedEnd=new DateTime(2026,10,15),
            Status="Released", ReleasedBy="estimator.csl",
            ReleasedAt=DateTimeOffset.UtcNow,
            CreatedBy="dev.seed"
        };
        db.WorkOrders.Add(woD);
        await db.SaveChangesAsync();

        var sopD = new StepOutPlan { CompanyCode="CSL", Name="Valero PA Unit 8 Piping — Execution SOP", SourceType="Estimate", LinkedEstimateId=estD.EstimateId, Client="Valero Energy", Site="Valero Port Arthur Refinery", PlannedStart=new DateTime(2026,9,1), PlannedEnd=new DateTime(2026,10,15), Status="Active", WorkOrderId=woD.WorkOrderId, CreatedBy="dev.seed" };
        var sD1 = new StepOutStep { SortOrder=1, StepCode="1", Title="Mobilize and set up", Status="Pending", CraftCode="SUP", RequiredPeople=2, DurationHours=32 };
        var sD2 = new StepOutStep { SortOrder=2, StepCode="2", Title="Unit 8 piping removal and replacement", Status="Pending", CraftCode="PF", RequiredPeople=5, DurationHours=320 };
        sD2.SubStepLeafs.Add(new StepOutSubStep { SubStepCode="2.1", SortOrder=1, Title="Remove existing spools 1–6", Status="Pending", CraftCode="PF", RequiredPeople=5, DurationHours=120 });
        sD2.SubStepLeafs.Add(new StepOutSubStep { SubStepCode="2.2", SortOrder=2, Title="Install new piping and fit-up", Status="Pending", CraftCode="PF", RequiredPeople=5, DurationHours=120 });
        sD2.SubStepLeafs.Add(new StepOutSubStep { SubStepCode="2.3", SortOrder=3, Title="Welding and PWHT", Status="Pending", CraftCode="WD", RequiredPeople=2, DurationHours=80 });
        sopD.Steps.Add(sD1); sopD.Steps.Add(sD2);

        var wpD1 = new WorkPackage { CompanyCode="CSL", WorkOrderId=woD.WorkOrderId, SourceType="StepOutPlan", Title="Piping Crew — Unit 8 Spool Replacement", CraftCode="PF", RequiredPeople=5, PlannedStart=new DateTime(2026,9,1), PlannedEnd=new DateTime(2026,10,10), Status="Scheduled", ReadyForScheduling=true, CreatedBy="dev.seed" };
        var wpD2 = new WorkPackage { CompanyCode="CSL", WorkOrderId=woD.WorkOrderId, SourceType="StepOutPlan", Title="Welding Crew — PWHT Scope", CraftCode="WD", RequiredPeople=2, PlannedStart=new DateTime(2026,9,20), PlannedEnd=new DateTime(2026,10,12), Status="Scheduled", ReadyForScheduling=true, CreatedBy="dev.seed" };
        sopD.WorkPackages.Add(wpD1); sopD.WorkPackages.Add(wpD2);
        db.StepOutPlans.Add(sopD);
        await db.SaveChangesAsync();

        // Scenario D assignments — people rolling off Scenario A available Sep 1
        if (resMap.Count > 0)
        {
            void AddAssignD(string empId, int wpId, string wpName, string craft)
            {
                if (!resMap.TryGetValue(empId, out var rid)) return;
                db.Assignments.Add(new Assignment { CompanyCode="CSL", ResourceId=rid, JobSourceType="WorkPackage", JobSourceId=wpId, JobName=wpName, CraftCode=craft, Start=new DateTime(2026,9,1), End=new DateTime(2026,10,15), Shift="Day", Status="Planned", CreatedBy="dev.seed" });
            }
            AddAssignD("CSL-017", wpD1.PackageId, wpD1.Title, "SUP");
            AddAssignD("CSL-018", wpD2.PackageId, wpD2.Title, "WD");
        }

        await db.SaveChangesAsync();
    }

}