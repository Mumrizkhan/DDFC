using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorkflowEngine.Domain.Entities;
using WorkflowEngine.Domain.Enums;
using WorkflowEngine.Infrastructure.Persistence;

namespace DDFC.Infrastructure.Data;

/// <summary>
/// Seeds the As-Built Plan Workflow into the WorkflowEngine.
/// Simplified workflow: Submit → Initiate → Package Selection → Payment
/// → Principal Architect → Architect → (3D + Structure + MEP parallel)
/// → PA Design Review → Building Control → Final Approval → Delivered
/// </summary>
public static class AsBuiltPlanWorkflowSeeder
{
    public const string ProcessName = "DDFC As-Built Plan Workflow";

    public static async Task<Guid> SeedAsync(IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();

        // ── Clean up old process definition so structural changes take effect ──────
        var existing = await db.Processes
            .IgnoreQueryFilters()
            .Include(p => p.Steps)
            .FirstOrDefaultAsync(p => p.Name == ProcessName);

        if (existing != null)
        {
            var stepIds = existing.Steps.Select(s => s.Id).ToList();

            var requestIds = await db.Requests
                .IgnoreQueryFilters()
                .Where(r => r.ProcessId == existing.Id)
                .Select(r => r.Id)
                .ToListAsync();

            if (requestIds.Count > 0)
            {
                var requestStepIds = await db.RequestSteps
                    .IgnoreQueryFilters()
                    .Where(rs => requestIds.Contains(rs.RequestId))
                    .Select(rs => rs.Id)
                    .ToListAsync();

                await db.RequestActions.IgnoreQueryFilters()
                    .Where(ra => requestStepIds.Contains(ra.RequestStepId))
                    .ExecuteDeleteAsync();

                await db.RequestSteps.IgnoreQueryFilters()
                    .Where(rs => requestIds.Contains(rs.RequestId))
                    .ExecuteDeleteAsync();

                await db.Requests.IgnoreQueryFilters()
                    .Where(r => r.ProcessId == existing.Id)
                    .ExecuteDeleteAsync();
            }

            if (stepIds.Count > 0)
            {
                await db.StepTransitions.IgnoreQueryFilters()
                    .Where(t => stepIds.Contains(t.FromStepId) || stepIds.Contains(t.ToStepId))
                    .ExecuteDeleteAsync();
            }

            db.Processes.Remove(existing);
            await db.SaveChangesAsync();
        }

        // ── Build new process ──────────────────────────────────────────────────────
        var process = new Process { Name = ProcessName };

        // ---- Step 1: Reception – Submit ----
        var step1 = new ProcessStep
        {
            Name = "Reception \u2013 Submit NOC/NDC Request",
            Order = 1,
            ActionCompletionMode = ActionCompletionMode.All
        };
        step1.Actions.Add(new StepAction { Name = "Create Request (Form 1)", ActionType = ActionType.General });

        // ---- Step 2: Admin – Document Review ----
        var step2 = new ProcessStep
        {
            Name = "Admin \u2013 Document Review",
            Order = 2,
            ActionCompletionMode = ActionCompletionMode.Any
        };
        step2.Actions.Add(new StepAction { Name = "Admin Review \u2013 Initiate", ActionType = ActionType.Approval });
        step2.Actions.Add(new StepAction { Name = "Admin Review \u2013 Reject", ActionType = ActionType.Rejection, IsRejectionAction = true });

        // ---- Step 3: Package Selection ----
        var step3 = new ProcessStep
        {
            Name = "Reception \u2013 Package Selection",
            Order = 3,
            ActionCompletionMode = ActionCompletionMode.All
        };
        step3.Actions.Add(new StepAction { Name = "Select Design Package", ActionType = ActionType.General });

        // ---- Step 4: Payment Confirmation ----
        var step4 = new ProcessStep
        {
            Name = "Finance Branch \u2013 Payment Confirmation",
            Order = 4,
            ActionCompletionMode = ActionCompletionMode.All
        };
        step4.Actions.Add(new StepAction { Name = "Confirm Payment", ActionType = ActionType.Approval });

        // ---- Step 5: Principal Architect – Initial Review ----
        var step5 = new ProcessStep
        {
            Name = "Principal Architect \u2013 Initial Review",
            Order = 5,
            ActionCompletionMode = ActionCompletionMode.All
        };
        step5.Actions.Add(new StepAction { Name = "Upload Soil Test", ActionType = ActionType.Upload });
        step5.Actions.Add(new StepAction { Name = "Assign Architect", ActionType = ActionType.General });

        // ---- Step 6: Architect Department – House Plan Design ----
        var step6 = new ProcessStep
        {
            Name = "Architect Department \u2013 House Plan Design",
            Order = 6,
            ActionCompletionMode = ActionCompletionMode.All
        };
        step6.Actions.Add(new StepAction { Name = "Upload House Plan", ActionType = ActionType.Upload });
        step6.Actions.Add(new StepAction { Name = "Customer Approves Plan", ActionType = ActionType.Approval });

        // ---- Step 7: 3D Visualization (PARALLEL with Steps 8 & 9) ----
        var step7 = new ProcessStep
        {
            Name = "Architecture Department \u2013 3D Visualization",
            Order = 7,
            ActionCompletionMode = ActionCompletionMode.All
        };
        step7.Actions.Add(new StepAction { Name = "Upload 3D Visualization Files", ActionType = ActionType.Upload });

        // ---- Step 8: Structure Department (PARALLEL with Steps 7 & 9) ----
        var step8 = new ProcessStep
        {
            Name = "Structure Department \u2013 Structural Design",
            Order = 8,
            ActionCompletionMode = ActionCompletionMode.All
        };
        step8.Actions.Add(new StepAction { Name = "Upload Structural Design", ActionType = ActionType.Upload });
        step8.Actions.Add(new StepAction { Name = "Generate Structural Report", ActionType = ActionType.Generate });

        // ---- Step 9: MEP Department (PARALLEL with Steps 7 & 8) ----
        var step9 = new ProcessStep
        {
            Name = "MEP Department \u2013 MEP Design",
            Order = 9,
            ActionCompletionMode = ActionCompletionMode.All
        };
        step9.Actions.Add(new StepAction { Name = "Upload MEP Design", ActionType = ActionType.Upload });
        step9.Actions.Add(new StepAction { Name = "Generate MEP Report", ActionType = ActionType.Generate });

        // ---- Step 10: Principal Architect – Design Review ----
        var step10 = new ProcessStep
        {
            Name = "Principal Architect \u2013 Design Review",
            Order = 10,
            ActionCompletionMode = ActionCompletionMode.Any
        };
        step10.Actions.Add(new StepAction { Name = "Approve Designs", ActionType = ActionType.Approval });
        step10.Actions.Add(new StepAction { Name = "Send Back for Revision", ActionType = ActionType.General });

        // ---- Step 11: Building Control – Physical Survey ----
        var step11 = new ProcessStep
        {
            Name = "Building Control \u2013 Physical Survey",
            Order = 11,
            ActionCompletionMode = ActionCompletionMode.All
        };
        step11.Actions.Add(new StepAction { Name = "Conduct Physical Survey", ActionType = ActionType.General });
        step11.Actions.Add(new StepAction { Name = "Upload Soil Test Report (PDF)", ActionType = ActionType.Upload });
        step11.Actions.Add(new StepAction { Name = "Conduct Building Control Survey", ActionType = ActionType.General });

        // ---- Step 12: DHA Design Head – Final Approval ----
        var step12 = new ProcessStep
        {
            Name = "DHA Design Head \u2013 Final Approval",
            Order = 12,
            ActionCompletionMode = ActionCompletionMode.Any
        };
        step12.Actions.Add(new StepAction { Name = "Final Approve", ActionType = ActionType.Approval });
        step12.Actions.Add(new StepAction { Name = "Reject Final", ActionType = ActionType.Rejection, IsRejectionAction = true });

        // ---- Step 13: Reception – Document Delivery ----
        var step13 = new ProcessStep
        {
            Name = "Reception \u2013 Document Delivery",
            Order = 13,
            ActionCompletionMode = ActionCompletionMode.All
        };
        step13.Actions.Add(new StepAction { Name = "Print & Hand Over Documents", ActionType = ActionType.General });

        process.Steps.AddRange(new[] {
            step1, step2, step3, step4, step5, step6,
            step7, step8, step9, step10, step11, step12, step13
        });

        db.Processes.Add(process);
        await db.SaveChangesAsync();

        // ── Define Transitions ─────────────────────────────────────────────────────

        // Step 1 → Step 2
        var t1_2 = new StepTransition { FromStepId = step1.Id, ToStepId = step2.Id, IsDefault = true };

        // Step 2 → Step 3 (on Initiate)
        var t2_3 = new StepTransition
        {
            FromStepId = step2.Id,
            ToStepId   = step3.Id,
            Condition  = "action=Admin Review \u2013 Initiate",
            IsDefault  = false,
            Priority   = 1
        };

        // Step 3 → Step 4
        var t3_4 = new StepTransition { FromStepId = step3.Id, ToStepId = step4.Id, IsDefault = true };

        // Step 4 → Step 5
        var t4_5 = new StepTransition { FromStepId = step4.Id, ToStepId = step5.Id, IsDefault = true };

        // Step 5 → Step 6
        var t5_6 = new StepTransition { FromStepId = step5.Id, ToStepId = step6.Id, IsDefault = true };

        // Step 6 → Steps 7, 8, 9 (ALL THREE PARALLEL)
        var t6_7 = new StepTransition
        {
            FromStepId = step6.Id, ToStepId = step7.Id,
            IsParallel = true, ParallelCompletionMode = ParallelCompletionMode.All, Priority = 1
        };
        var t6_8 = new StepTransition
        {
            FromStepId = step6.Id, ToStepId = step8.Id,
            IsParallel = true, ParallelCompletionMode = ParallelCompletionMode.All, Priority = 1
        };
        var t6_9 = new StepTransition
        {
            FromStepId = step6.Id, ToStepId = step9.Id,
            IsParallel = true, ParallelCompletionMode = ParallelCompletionMode.All, Priority = 1
        };

        // Steps 7, 8, 9 → Step 10 (parallel group converges)
        var t7_10  = new StepTransition { FromStepId = step7.Id,  ToStepId = step10.Id, IsDefault = true };
        var t8_10  = new StepTransition { FromStepId = step8.Id,  ToStepId = step10.Id, IsDefault = true };
        var t9_10  = new StepTransition { FromStepId = step9.Id,  ToStepId = step10.Id, IsDefault = true };

        // Step 10 → Step 11 (on approve)
        var t10_11 = new StepTransition
        {
            FromStepId = step10.Id, ToStepId = step11.Id,
            Condition  = "action=Approve Designs", IsDefault = false, Priority = 1
        };

        // Step 11 → Step 12
        var t11_12 = new StepTransition { FromStepId = step11.Id, ToStepId = step12.Id, IsDefault = true };

        // Step 12 → Step 13 (on Final Approve)
        var t12_13 = new StepTransition
        {
            FromStepId = step12.Id, ToStepId = step13.Id,
            Condition  = "action=Final Approve", IsDefault = false, Priority = 1
        };

        db.StepTransitions.AddRange(
            t1_2, t2_3, t3_4, t4_5, t5_6,
            t6_7, t6_8, t6_9,
            t7_10, t8_10, t9_10,
            t10_11, t11_12, t12_13
        );

        await db.SaveChangesAsync();
        return process.Id;
    }
}
