using Microsoft.EntityFrameworkCore;
using Stronghold.EnterpriseEstimating.Data;

namespace Stronghold.EnterpriseEstimating.Api.Services.Dev;

public static class DevResetOrchestrator
{
    public static async Task<object> ResetAsync(AppDbContext db, bool includeCostBooks = false, bool includePm = false)
    {

        if (includePm)
        {
            var demoEstIds = await db.Estimates
                .Where(e => db.Projects.Any(p => p.EstimateId == e.EstimateId && p.ProjectNumber.StartsWith("PRJ-DEMO-")))
                .Select(e => e.EstimateId).ToListAsync();
            var demoWoIds = await db.WorkOrders
                .Where(wo => wo.WorkOrderNumber.StartsWith("WO-DEMO-"))
                .Select(wo => wo.WorkOrderId).ToListAsync();
            var demoFcoIds = await db.FcoDocuments
                .Where(f => f.FcoNumber.StartsWith("FCO-DEMO-"))
                .Select(f => f.FcoDocumentId).ToListAsync();
            var demoTaskIds = await db.PlanTasks
                .Where(t => db.ProjectPhases.Any(ph => ph.ProjectId != 0 &&
                    db.Projects.Any(p => p.ProjectId == ph.ProjectId && p.ProjectNumber.StartsWith("PRJ-DEMO-")) &&
                    t.PhaseId == ph.PhaseId))
                .Select(t => t.TaskId).ToListAsync();
            var demoResIds = await db.Resources
                .Where(r => r.EmployeeId != null && r.EmployeeId.StartsWith("CSL-0"))
                .Select(r => r.ResourceId).ToListAsync();

            await db.TaskProgressSnapshots.Where(s => demoTaskIds.Contains(s.TaskId)).ExecuteDeleteAsync();
            await db.EstimateTaskLinks.Where(l => demoTaskIds.Contains(l.TaskId)).ExecuteDeleteAsync();
            await db.FcoTaskLinks.Where(l => demoFcoIds.Contains(l.FcoDocumentId)).ExecuteDeleteAsync();
            await db.ActualEntries.Where(a => demoWoIds.Contains(a.WorkOrderId)).ExecuteDeleteAsync();
            await db.FcoLaborLines.Where(l => demoFcoIds.Contains(l.FcoDocumentId)).ExecuteDeleteAsync();
            await db.FcoDocuments.Where(f => demoFcoIds.Contains(f.FcoDocumentId)).ExecuteDeleteAsync();
            await db.Assignments.Where(a => demoResIds.Contains(a.ResourceId)).ExecuteDeleteAsync();
            await db.AvailabilityBlocks.Where(b => demoResIds.Contains(b.ResourceId)).ExecuteDeleteAsync();
            await db.Certifications.Where(c => demoResIds.Contains(c.ResourceId)).ExecuteDeleteAsync();
            await db.Resources.Where(r => demoResIds.Contains(r.ResourceId)).ExecuteDeleteAsync();
            await db.WorkPackages.Where(wp => demoWoIds.Contains(wp.WorkOrderId!.Value)).ExecuteDeleteAsync();
            var demoStepPlanIds = await db.StepOutPlans.Where(s => demoWoIds.Contains(s.WorkOrderId!.Value)).Select(s => s.PlanId).ToListAsync();
            var demoStepIds = await db.StepOutSteps.Where(s => demoStepPlanIds.Contains(s.PlanId)).Select(s => s.StepId).ToListAsync();
            await db.StepOutSubSteps.Where(s => demoStepIds.Contains(s.StepId)).ExecuteDeleteAsync();
            await db.StepDependencies.Where(d => demoStepIds.Contains(d.StepId)).ExecuteDeleteAsync();
            await db.StepResourceReqs.Where(r => demoStepIds.Contains(r.StepId)).ExecuteDeleteAsync();
            await db.StepOutSteps.Where(s => demoStepPlanIds.Contains(s.PlanId)).ExecuteDeleteAsync();
            await db.StepOutPlans.Where(s => demoStepPlanIds.Contains(s.PlanId)).ExecuteDeleteAsync();
            await db.TaskDependencies.Where(d => demoTaskIds.Contains(d.SuccessorTaskId)).ExecuteDeleteAsync();
            await db.PlanTasks.Where(t => demoTaskIds.Contains(t.TaskId)).ExecuteDeleteAsync();
            var demoProjIds = await db.Projects.Where(p => p.ProjectNumber.StartsWith("PRJ-DEMO-")).Select(p => p.ProjectId).ToListAsync();
            await db.Milestones.Where(m => demoProjIds.Contains(m.ProjectId)).ExecuteDeleteAsync();
            await db.ProjectPhases.Where(ph => demoProjIds.Contains(ph.ProjectId)).ExecuteDeleteAsync();
            await db.TimelineBaselines.Where(b => demoProjIds.Contains(b.ProjectId)).ExecuteDeleteAsync();
            await db.WorkOrders.Where(wo => demoWoIds.Contains(wo.WorkOrderId)).ExecuteDeleteAsync();
            await db.Projects.Where(p => demoProjIds.Contains(p.ProjectId)).ExecuteDeleteAsync();
            var demoCommAuthIds = await db.CommercialAuthorizations.Where(ca => demoEstIds.Contains(ca.EstimateId)).Select(ca => ca.CommercialAuthorizationId).ToListAsync();
            await db.CommercialAuthorizations.Where(ca => demoCommAuthIds.Contains(ca.CommercialAuthorizationId)).ExecuteDeleteAsync();
            await db.EstimateSummaries.Where(s => demoEstIds.Contains(s.EstimateId)).ExecuteDeleteAsync();
            await db.Estimates.Where(e => demoEstIds.Contains(e.EstimateId)).ExecuteDeleteAsync();
        }

        // Break circular FK: Estimate.StaffingPlanId ↔ StaffingPlan.ConvertedEstimateId
        await db.Estimates.ExecuteUpdateAsync(s => s.SetProperty(e => e.StaffingPlanId, (int?)null));
        await db.StaffingPlans.ExecuteUpdateAsync(s => s.SetProperty(p => p.ConvertedEstimateId, (int?)null));

        db.FcoEntries.RemoveRange(db.FcoEntries);
        db.EstimateRevisions.RemoveRange(db.EstimateRevisions);
        db.EstimateSummaries.RemoveRange(db.EstimateSummaries);
        db.LaborRows.RemoveRange(db.LaborRows);
        db.EquipmentRows.RemoveRange(db.EquipmentRows);
        db.ExpenseRows.RemoveRange(db.ExpenseRows);
        db.Estimates.RemoveRange(db.Estimates);
        db.StaffingLaborRows.RemoveRange(db.StaffingLaborRows);
        db.StaffingPlans.RemoveRange(db.StaffingPlans);
        db.CrewTemplateRows.RemoveRange(db.CrewTemplateRows);
        db.CrewTemplates.RemoveRange(db.CrewTemplates);
        db.RateBookLaborRates.RemoveRange(db.RateBookLaborRates);
        db.RateBookEquipmentRates.RemoveRange(db.RateBookEquipmentRates);
        db.RateBookExpenseItems.RemoveRange(db.RateBookExpenseItems);
        db.RateBooks.RemoveRange(db.RateBooks);
        if (includeCostBooks)
        {
            db.CostBookLaborRates.RemoveRange(db.CostBookLaborRates);
            db.CostBookEquipmentRates.RemoveRange(db.CostBookEquipmentRates);
            db.CostBookExpenses.RemoveRange(db.CostBookExpenses);
            db.CostBookOverheadItems.RemoveRange(db.CostBookOverheadItems);
            db.CostBooks.RemoveRange(db.CostBooks);
        }

        db.EstimateSequences.RemoveRange(db.EstimateSequences);
        await db.SaveChangesAsync();

        return new
        {
            message = includeCostBooks
                ? "All estimating data, including cost books, cleared. Ready to re-seed."
                : "Estimating data cleared. Existing cost books were preserved.",
            costBooksPreserved = !includeCostBooks
        };
    }
}