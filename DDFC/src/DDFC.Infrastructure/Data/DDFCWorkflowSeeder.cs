using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorkflowEngine.Domain.Entities;
using WorkflowEngine.Domain.Enums;
using WorkflowEngine.Infrastructure.Persistence;

namespace DDFC.Infrastructure.Data;

/// <summary>
/// Seeds the 17-step DDFC Possession &amp; House Design Workflow into the WorkflowEngine.
/// Parallel branches:
///   Steps 3+4    â€” Transfer Branch + Finance Branch
///   Steps 11+12+13 â€” 3D Visualization + Structure Dept + MEP Dept (all three in parallel)
/// Note: the seeder deletes and recreates the process definition on each startup
///       so that structural changes (e.g. new parallel branches) take effect automatically.
/// </summary>
public static class DDFCWorkflowSeeder
{
    public const string ProcessName = "DDFC Possession & House Design Workflow";

    public static async Task<Guid> SeedAsync(IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();

        // â”€â”€ Clean up old process definition so structural changes take effect â”€â”€â”€â”€â”€â”€
        var existing = await db.Processes
            .IgnoreQueryFilters()
            .Include(p => p.Steps)
            .FirstOrDefaultAsync(p => p.Name == ProcessName);

        if (existing != null)
        {
            var stepIds = existing.Steps.Select(s => s.Id).ToList();

            // Delete runtime request data (Restrict FKs require manual cleanup first)
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

            // Delete transitions (Restrict FKs)
            if (stepIds.Count > 0)
            {
                await db.StepTransitions.IgnoreQueryFilters()
                    .Where(t => stepIds.Contains(t.FromStepId) || stepIds.Contains(t.ToStepId))
                    .ExecuteDeleteAsync();
            }

            // Delete process (cascades to ProcessSteps â†’ StepActions)
            db.Processes.Remove(existing);
            await db.SaveChangesAsync();
        }

        // â”€â”€ Build new process â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var process = new Process { Name = ProcessName };

        // ---- Step 1: Reception â€“ Submit NOC/NDC Request ----
        var step1 = new ProcessStep
        {
            Name = "Reception â€“ Submit NOC/NDC Request",
            Order = 1,
            ActionCompletionMode = ActionCompletionMode.All
        };
        step1.Actions.Add(new StepAction { Name = "Create Request (Form 1)", ActionType = ActionType.General });

        // ---- Step 2: Admin â€“ Document Review ----
        var stepAdmin = new ProcessStep
        {
            Name = "Admin â€“ Document Review",
            Order = 2,
            ActionCompletionMode = ActionCompletionMode.Any
        };
        stepAdmin.Actions.Add(new StepAction { Name = "Admin Review â€“ Initiate", ActionType = ActionType.Approval });
        stepAdmin.Actions.Add(new StepAction { Name = "Admin Review â€“ Reject", ActionType = ActionType.Rejection, IsRejectionAction = true });

        // ---- Step 3: Transfer Branch (PARALLEL with Step 4) ----
        var step2 = new ProcessStep
        {
            Name = "Transfer Branch â€“ NOC/NDC Review",
            Order = 3,
            ActionCompletionMode = ActionCompletionMode.Any
        };
        step2.Actions.Add(new StepAction { Name = "Approve Transfer", ActionType = ActionType.Approval });
        step2.Actions.Add(new StepAction { Name = "Reject Transfer", ActionType = ActionType.Rejection, IsRejectionAction = true });
        step2.Actions.Add(new StepAction { Name = "Request Clarification", ActionType = ActionType.General });

        // ---- Step 4: Finance Branch (PARALLEL with Step 3) ----
        var step3 = new ProcessStep
        {
            Name = "Finance Branch â€“ Dues Clearance",
            Order = 4,
            ActionCompletionMode = ActionCompletionMode.Any
        };
        step3.Actions.Add(new StepAction { Name = "Approve Finance", ActionType = ActionType.Approval });
        step3.Actions.Add(new StepAction { Name = "Reject Finance", ActionType = ActionType.Rejection, IsRejectionAction = true });

        // ---- Step 5: DDFC Admin â€“ Sign Possession Letter ----
        var stepDdfcAdmin = new ProcessStep
        {
            Name = "DDFC Admin â€“ Sign Possession Letter",
            Order = 5,
            ActionCompletionMode = ActionCompletionMode.All
        };
        stepDdfcAdmin.Actions.Add(new StepAction { Name = "Sign Possession Letter", ActionType = ActionType.Approval });

        // ---- Step 6: Reception â€“ Package Selection ----
        var step5 = new ProcessStep
        {
            Name = "Reception â€“ Package Selection",
            Order = 7,
            ActionCompletionMode = ActionCompletionMode.All
        };
        step5.Actions.Add(new StepAction { Name = "Select Design Package", ActionType = ActionType.General });

        // ---- Step 8: Finance Branch â€“ Payment Confirmation ----
        var step6 = new ProcessStep
        {
            Name = "Finance Branch â€“ Payment Confirmation",
            Order = 8,
            ActionCompletionMode = ActionCompletionMode.All
        };
        step6.Actions.Add(new StepAction { Name = "Confirm Payment", ActionType = ActionType.Approval });

        // ---- Step 9: Principal Architect â€“ Initial Review ----
        var stepPAInitial = new ProcessStep
        {
            Name = "Principal Architect \u2013 Initial Review",
            Order = 9,
            ActionCompletionMode = ActionCompletionMode.All
        };
        stepPAInitial.Actions.Add(new StepAction { Name = "Upload Soil Test", ActionType = ActionType.Upload });
        stepPAInitial.Actions.Add(new StepAction { Name = "Assign Architect", ActionType = ActionType.General });

        // ---- Step 10: Architecture Department â€“ House Plan Design ----
        var step7 = new ProcessStep
        {
            Name = "Architecture Department \u2013 House Plan Design",
            Order = 10,
            ActionCompletionMode = ActionCompletionMode.All
        };
        step7.Actions.Add(new StepAction { Name = "Upload House Plan", ActionType = ActionType.Upload });
        step7.Actions.Add(new StepAction { Name = "Customer Approves Plan", ActionType = ActionType.Approval });

        // ---- Step 11: Architecture Department â€“ 3D Visualization (PARALLEL with Steps 12 & 13) ----
        var step3D = new ProcessStep
        {
            Name = "Architecture Department \u2013 3D Visualization",
            Order = 11,
            ActionCompletionMode = ActionCompletionMode.All
        };
        step3D.Actions.Add(new StepAction { Name = "Upload 3D Visualization Files", ActionType = ActionType.Upload });

        // ---- Step 12: Structure Department (PARALLEL with Steps 11 & 13) ----
        var step8 = new ProcessStep
        {
            Name = "Structure Department \u2013 Structural Design",
            Order = 12,
            ActionCompletionMode = ActionCompletionMode.All
        };
        step8.Actions.Add(new StepAction { Name = "Upload Structural Design", ActionType = ActionType.Upload });
        step8.Actions.Add(new StepAction { Name = "Generate Structural Report", ActionType = ActionType.Generate });

        // ---- Step 13: MEP Department (PARALLEL with Steps 11 & 12) ----
        var step9 = new ProcessStep
        {
            Name = "MEP Department \u2013 MEP Design",
            Order = 13,
            ActionCompletionMode = ActionCompletionMode.All
        };
        step9.Actions.Add(new StepAction { Name = "Upload MEP Design", ActionType = ActionType.Upload });
        step9.Actions.Add(new StepAction { Name = "Generate MEP Report", ActionType = ActionType.Generate });

        // ---- Step 14: Principal Architect â€“ Design Review ----
        var step10 = new ProcessStep
        {
            Name = "Principal Architect \u2013 Design Review",
            Order = 14,
            ActionCompletionMode = ActionCompletionMode.Any
        };
        step10.Actions.Add(new StepAction { Name = "Approve Designs", ActionType = ActionType.Approval });
        step10.Actions.Add(new StepAction { Name = "Send Back for Revision", ActionType = ActionType.General });

        // ---- Step 15: Building Control â€“ Physical Survey ----
        var step11 = new ProcessStep
        {
            Name = "Building Control \u2013 Physical Survey",
            Order = 15,
            ActionCompletionMode = ActionCompletionMode.All
        };
        step11.Actions.Add(new StepAction { Name = "Conduct Physical Survey", ActionType = ActionType.General });
        step11.Actions.Add(new StepAction { Name = "Upload Soil Test Report (PDF)", ActionType = ActionType.Upload });
        step11.Actions.Add(new StepAction { Name = "Conduct Building Control Survey", ActionType = ActionType.General });

        // ---- Step 16: DHA Design Head â€“ Final Approval ----
        var step13 = new ProcessStep
        {
            Name = "DHA Design Head \u2013 Final Approval",
            Order = 16,
            ActionCompletionMode = ActionCompletionMode.Any
        };
        step13.Actions.Add(new StepAction { Name = "Final Approve", ActionType = ActionType.Approval });
        step13.Actions.Add(new StepAction { Name = "Reject Final", ActionType = ActionType.Rejection, IsRejectionAction = true });

        // ---- Step 17: Reception â€“ Document Delivery ----
        var step14 = new ProcessStep
        {
            Name = "Reception \u2013 Document Delivery",
            Order = 17,
            ActionCompletionMode = ActionCompletionMode.All
        };
        step14.Actions.Add(new StepAction { Name = "Print & Hand Over Documents", ActionType = ActionType.General });

        process.Steps.AddRange(new[] {
            step1, stepAdmin, step2, step3, stepDdfcAdmin,
            step5, step6, stepPAInitial,
            step7, step3D, step8, step9,
            step10, step11, step13, step14
        });

        db.Processes.Add(process);
        await db.SaveChangesAsync();

        // â”€â”€ Define Transitions â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        // Step 1 â†’ Admin Review
        var t1_admin = new StepTransition { FromStepId = step1.Id, ToStepId = stepAdmin.Id, IsDefault = true };

        // Admin Review â†’ Transfer + Finance (PARALLEL, All completion mode)
        var t_admin_2 = new StepTransition
        {
            FromStepId = stepAdmin.Id,
            ToStepId = step2.Id,
            Condition = "action=Admin Review â€“ Initiate",
            IsParallel = true,
            ParallelCompletionMode = ParallelCompletionMode.All,
            Priority = 1
        };
        var t_admin_3 = new StepTransition
        {
            FromStepId = stepAdmin.Id,
            ToStepId = step3.Id,
            Condition = "action=Admin Review â€“ Initiate",
            IsParallel = true,
            ParallelCompletionMode = ParallelCompletionMode.All,
            Priority = 1
        };

        // Transfer + Finance each â†’ DDFC Admin (parallel group converges)
        var t2_ddfcAdmin = new StepTransition { FromStepId = step2.Id, ToStepId = stepDdfcAdmin.Id, IsDefault = true };
        var t3_ddfcAdmin = new StepTransition { FromStepId = step3.Id, ToStepId = stepDdfcAdmin.Id, IsDefault = true };

        // DDFC Admin -> Package Selection (undertaking handled as button in Architecture step)
        var t_ddfcAdmin_5 = new StepTransition { FromStepId = stepDdfcAdmin.Id, ToStepId = step5.Id, IsDefault = true };

        // Package â†’ Payment
        var t5_6 = new StepTransition { FromStepId = step5.Id, ToStepId = step6.Id, IsDefault = true };

        // Payment â†’ PA Initial Review
        var t6_PA = new StepTransition { FromStepId = step6.Id, ToStepId = stepPAInitial.Id, IsDefault = true };

        // PA Initial Review â†’ Architecture
        var t6_7 = new StepTransition { FromStepId = stepPAInitial.Id, ToStepId = step7.Id, IsDefault = true };

        // Architecture â†’ 3D + Structure + MEP (ALL THREE PARALLEL, All completion mode)
        var t7_3D = new StepTransition
        {
            FromStepId = step7.Id,
            ToStepId = step3D.Id,
            IsParallel = true,
            ParallelCompletionMode = ParallelCompletionMode.All,
            Priority = 1
        };
        var t7_8 = new StepTransition
        {
            FromStepId = step7.Id,
            ToStepId = step8.Id,
            IsParallel = true,
            ParallelCompletionMode = ParallelCompletionMode.All,
            Priority = 1
        };
        var t7_9 = new StepTransition
        {
            FromStepId = step7.Id,
            ToStepId = step9.Id,
            IsParallel = true,
            ParallelCompletionMode = ParallelCompletionMode.All,
            Priority = 1
        };

        // 3D, Structure, MEP each â†’ PA Design Review (parallel group converges)
        var t3D_10 = new StepTransition { FromStepId = step3D.Id, ToStepId = step10.Id, IsDefault = true };
        var t8_10  = new StepTransition { FromStepId = step8.Id,  ToStepId = step10.Id, IsDefault = true };
        var t9_10  = new StepTransition { FromStepId = step9.Id,  ToStepId = step10.Id, IsDefault = true };

        // PA Design Review â†’ Building Control (on approve)
        var t10_11 = new StepTransition
        {
            FromStepId = step10.Id,
            ToStepId = step11.Id,
            Condition = "action=Approve Designs",
            IsDefault = false,
            Priority = 1
        };

        // Building Control â†’ Final Approval
        var t11_13 = new StepTransition { FromStepId = step11.Id, ToStepId = step13.Id, IsDefault = true };

        // Final Approval â†’ Document Delivery (on approve)
        var t13_14 = new StepTransition
        {
            FromStepId = step13.Id,
            ToStepId = step14.Id,
            Condition = "action=Final Approve",
            IsDefault = false,
            Priority = 1
        };

        db.StepTransitions.AddRange(
            t1_admin,
            t_admin_2, t_admin_3,
            t2_ddfcAdmin, t3_ddfcAdmin,
            t_ddfcAdmin_5,
            t5_6, t6_PA, t6_7,
            t7_3D, t7_8, t7_9,
            t3D_10, t8_10, t9_10,
            t10_11,
            t11_13,
            t13_14
        );

        await db.SaveChangesAsync();

        return process.Id;
    }
}


