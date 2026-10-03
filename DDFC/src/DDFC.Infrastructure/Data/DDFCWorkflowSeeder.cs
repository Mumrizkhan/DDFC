using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorkflowEngine.Domain.Entities;
using WorkflowEngine.Domain.Enums;
using WorkflowEngine.Infrastructure.Persistence;

namespace DDFC.Infrastructure.Data;

/// <summary>
/// Seeds the 21-step DDFC Possession &amp; House Design Workflow into the WorkflowEngine.
/// Branches:
///   Steps 3â†’4    â€” Transfer Branch then Finance Branch (sequential)
///   Step 5       â€” AD Coordinator Review (after Finance, before DDFC Admin signs)
///   Step 6       â€” BCD Upload Possession Letter
///   Step 9       â€” Admin Review (after Payment, before Soil Test)
///   Step 10      â€” Soil Test (before Principal Architect Initial Review)
///   Steps 13â†’14â†’15 â€” 3D Visualization, Architect drafter upload, then Structure
/// Existing process definitions are updated in place to preserve runtime requests.
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
            string[] orderedNames = [
                "Reception \u2013 Package Selection", "Reception \u2013 Payment Confirmation",
                "Admin \u2013 Post-Payment Review", "Soil Test", "Principal Architect \u2013 Initial Review",
                "Architect Department \u2013 House Plan Design", "Architecture Department \u2013 3D Visualization",
                "Architect \u2013 Assign 3D Drafter & Upload Draft", "Structure Department \u2013 Structural Design",
                "MEP Department \u2013 MEP Design", "Principal Architect \u2013 Design Review",
                "Building Control \u2013 Physical Survey", "DHA Design Head \u2013 Final Approval",
                "Reception \u2013 Document Delivery"
            ];
            for (var index = 0; index < orderedNames.Length; index++)
            {
                var step = existing.Steps.FirstOrDefault(item => item.Name == orderedNames[index]);
                if (step != null) step.Order = index + 8;
            }
            for (var index = 0; index < 3; index++)
            {
                var from = existing.Steps.FirstOrDefault(step => step.Name == orderedNames[index])
                    ?? throw new InvalidOperationException($"Workflow step '{orderedNames[index]}' is missing.");
                var to = existing.Steps.FirstOrDefault(step => step.Name == orderedNames[index + 1])
                    ?? throw new InvalidOperationException($"Workflow step '{orderedNames[index + 1]}' is missing.");
                var transition = await db.StepTransitions.FirstOrDefaultAsync(item => item.FromStepId == from.Id && item.IsDefault);
                if (transition == null)
                    db.StepTransitions.Add(new StepTransition { FromStepId = from.Id, ToStepId = to.Id, IsDefault = true });
                else
                    transition.ToStepId = to.Id;
            }
            await db.SaveChangesAsync();
            return existing.Id;
        }

        // â”€â”€ Build new process â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var process = new Process { Name = ProcessName };

        // ---- Step 1: Reception – Submit NOC/NDC Request ----
        var step1 = new ProcessStep
        {
            Name = "Reception \u2013 Submit NOC/NDC Request",
            Order = 1,
            ActionCompletionMode = ActionCompletionMode.All
        };
        step1.Actions.Add(new StepAction { Name = "Create Request (Form 1)", ActionType = ActionType.General });

        // ---- Step 2: Reception – Documents Verification ----
        var stepDocuments = new ProcessStep
        {
            Name = "Reception \u2013 Documents Verification",
            Order = 2,
            ActionCompletionMode = ActionCompletionMode.All
        };
        stepDocuments.Actions.Add(new StepAction { Name = "Verify Documents", ActionType = ActionType.Approval });

        // ---- Step 3: Transfer Branch (sequential, before Step 4) ----
        var step2 = new ProcessStep
        {
            Name = "Transfer Branch \u2013 NOC/NDC Review",
            Order = 3,
            ActionCompletionMode = ActionCompletionMode.Any
        };
        step2.Actions.Add(new StepAction { Name = "Approve Transfer", ActionType = ActionType.Approval });
        step2.Actions.Add(new StepAction { Name = "Reject Transfer", ActionType = ActionType.Rejection, IsRejectionAction = true });
        step2.Actions.Add(new StepAction { Name = "Request Clarification", ActionType = ActionType.General });

        // ---- Step 3: Finance Branch (sequential, after Step 2) ----
        var step3 = new ProcessStep
        {
            Name = "Finance Branch \u2013 Dues Clearance",
            Order = 4,
            ActionCompletionMode = ActionCompletionMode.Any
        };
        step3.Actions.Add(new StepAction { Name = "Approve Finance", ActionType = ActionType.Approval });
        step3.Actions.Add(new StepAction { Name = "Reject Finance", ActionType = ActionType.Rejection, IsRejectionAction = true });

        // ---- Step 4: AD Coordinator – Review ----
        var stepAdCoord = new ProcessStep
        {
            Name = "AD Coordinator \u2013 Review",
            Order = 5,
            ActionCompletionMode = ActionCompletionMode.Any
        };
        stepAdCoord.Actions.Add(new StepAction { Name = "Approve AD Coord", ActionType = ActionType.Approval });
        stepAdCoord.Actions.Add(new StepAction { Name = "Reject AD Coord", ActionType = ActionType.Rejection, IsRejectionAction = true });

        // ---- Step 5: BCD – Upload Possession Letter ----
        var stepBcd = new ProcessStep
        {
            Name = "BCD \u2013 Upload Possession Letter",
            Order = 6,
            ActionCompletionMode = ActionCompletionMode.All
        };
        stepBcd.Actions.Add(new StepAction { Name = "Upload Possession Letter", ActionType = ActionType.Upload });

        // ---- Step 6: DDFC Admin – Sign Possession Letter ----
        var stepDdfcAdmin = new ProcessStep
        {
            Name = "DDFC Admin \u2013 Sign Possession Letter",
            Order = 7,
            ActionCompletionMode = ActionCompletionMode.All
        };
        stepDdfcAdmin.Actions.Add(new StepAction { Name = "Sign Possession Letter", ActionType = ActionType.Approval });

        // ---- Step 7: Reception – Package Selection ----
        var step5 = new ProcessStep
        {
            Name = "Reception \u2013 Package Selection",
            Order = 8,
            ActionCompletionMode = ActionCompletionMode.All
        };
        step5.Actions.Add(new StepAction { Name = "Select Design Package", ActionType = ActionType.General });

        // ---- Step 8: Reception – Payment Confirmation ----
        var step6 = new ProcessStep
        {
            Name = "Reception \u2013 Payment Confirmation",
            Order = 9,
            ActionCompletionMode = ActionCompletionMode.All
        };
        step6.Actions.Add(new StepAction { Name = "Confirm Payment", ActionType = ActionType.Approval });

        // ---- Step 9: Admin – Post-Payment Review ----
        var stepAdmin = new ProcessStep
        {
            Name = "Admin \u2013 Post-Payment Review",
            Order = 10,
            ActionCompletionMode = ActionCompletionMode.Any
        };
        stepAdmin.Actions.Add(new StepAction { Name = "Approve", ActionType = ActionType.Approval });
        stepAdmin.Actions.Add(new StepAction { Name = "Reject", ActionType = ActionType.Rejection, IsRejectionAction = true });

        // ---- Step 10: Soil Test ----
        var stepSoilTest = new ProcessStep
        {
            Name = "Soil Test",
            Order = 11,
            ActionCompletionMode = ActionCompletionMode.All
        };
        stepSoilTest.Actions.Add(new StepAction { Name = "Upload Soil Test", ActionType = ActionType.Upload });

        // ---- Step 10: Principal Architect & Initial Review ----
        var stepPAInitial = new ProcessStep
        {
            Name = "Principal Architect \u2013 Initial Review",
            Order = 12,
            ActionCompletionMode = ActionCompletionMode.All
        };
        stepPAInitial.Actions.Add(new StepAction { Name = "Assign Architect", ActionType = ActionType.General });

        // ---- Step 11: Architect Department â€" House Plan Design ----
        var step7 = new ProcessStep
        {
            Name = "Architect Department \u2013 House Plan Design",
            Order = 13,
            ActionCompletionMode = ActionCompletionMode.All
        };
        step7.Actions.Add(new StepAction { Name = "Upload House Plan", ActionType = ActionType.Upload });
        step7.Actions.Add(new StepAction { Name = "Customer Approves Plan", ActionType = ActionType.Approval });

        // ---- Step 13: Architecture Department – 3D Visualization ----
        var step3D = new ProcessStep
        {
            Name = "Architecture Department \u2013 3D Visualization",
            Order = 14,
            ActionCompletionMode = ActionCompletionMode.All
        };
        step3D.Actions.Add(new StepAction { Name = "Upload 3D Visualization Files", ActionType = ActionType.Upload });

        // ---- Step 14: Architect – Assign Drafter & Upload 3D Draft ----
        var stepDraft = new ProcessStep
        {
            Name = "Architect \u2013 Assign 3D Drafter & Upload Draft",
            Order = 15,
            ActionCompletionMode = ActionCompletionMode.All
        };
        stepDraft.Actions.Add(new StepAction { Name = "Assign 3D Drafter", ActionType = ActionType.General });
        stepDraft.Actions.Add(new StepAction { Name = "Upload 3D Draft", ActionType = ActionType.Upload });

        // ---- Step 15: Structure Department ----
        var step8 = new ProcessStep
        {
            Name = "Structure Department \u2013 Structural Design",
            Order = 16,
            ActionCompletionMode = ActionCompletionMode.All
        };
        step8.Actions.Add(new StepAction { Name = "Upload Structural Design", ActionType = ActionType.Upload });
        step8.Actions.Add(new StepAction { Name = "Generate Structural Report", ActionType = ActionType.Generate });

        // ---- Step 16: MEP Department ----
        var step9 = new ProcessStep
        {
            Name = "MEP Department \u2013 MEP Design",
            Order = 17,
            ActionCompletionMode = ActionCompletionMode.All
        };
        step9.Actions.Add(new StepAction { Name = "Upload MEP Design", ActionType = ActionType.Upload });
        step9.Actions.Add(new StepAction { Name = "Generate MEP Report", ActionType = ActionType.Generate });

        // ---- Step 15: Principal Architect & Design Review ----
        var step10 = new ProcessStep
        {
            Name = "Principal Architect \u2013 Design Review",
            Order = 18,
            ActionCompletionMode = ActionCompletionMode.Any
        };
        step10.Actions.Add(new StepAction { Name = "Approve Designs", ActionType = ActionType.Approval });
        step10.Actions.Add(new StepAction { Name = "Send Back for Revision", ActionType = ActionType.General });

        // ---- Step 16: Building Control & Physical Survey ----
        var step11 = new ProcessStep
        {
            Name = "Building Control \u2013 Physical Survey",
            Order = 19,
            ActionCompletionMode = ActionCompletionMode.All
        };
        step11.Actions.Add(new StepAction { Name = "Conduct Physical Survey", ActionType = ActionType.General });
        step11.Actions.Add(new StepAction { Name = "Upload Soil Test Report (PDF)", ActionType = ActionType.Upload });
        step11.Actions.Add(new StepAction { Name = "Conduct Building Control Survey", ActionType = ActionType.General });

        // ---- Step 17: DHA Design Head & Final Approval ----
        var step13 = new ProcessStep
        {
            Name = "DHA Design Head \u2013 Final Approval",
            Order = 20,
            ActionCompletionMode = ActionCompletionMode.Any
        };
        step13.Actions.Add(new StepAction { Name = "Final Approve", ActionType = ActionType.Approval });
        step13.Actions.Add(new StepAction { Name = "Reject Final", ActionType = ActionType.Rejection, IsRejectionAction = true });

        // ---- Step 18: Reception & Document Delivery ----
        var step14 = new ProcessStep
        {
            Name = "Reception \u2013 Document Delivery",
            Order = 21,
            ActionCompletionMode = ActionCompletionMode.All
        };
        step14.Actions.Add(new StepAction { Name = "Print & Hand Over Documents", ActionType = ActionType.General });

        process.Steps.AddRange(new[] {
            step1, stepDocuments, step2, step3, stepAdCoord, stepBcd, stepDdfcAdmin,
            step5, step6, stepAdmin, stepSoilTest, stepPAInitial,
            step7, step3D, stepDraft, step8, step9,
            step10, step11, step13, step14
        });

        db.Processes.Add(process);
        await db.SaveChangesAsync();

        // â”€â”€ Define Transitions â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        // Step 1 â†’ Documents Verification
        var t1_documents = new StepTransition { FromStepId = step1.Id, ToStepId = stepDocuments.Id, IsDefault = true };

        // Documents Verification â†’ Transfer Branch (sequential)
        var t_documents_2 = new StepTransition { FromStepId = stepDocuments.Id, ToStepId = step2.Id, IsDefault = true };

        // Transfer Branch â†’ Finance Branch (sequential)
        var t2_3 = new StepTransition { FromStepId = step2.Id, ToStepId = step3.Id, IsDefault = true };

        // Finance Branch â†’ AD Coordinator
        var t3_adCoord = new StepTransition { FromStepId = step3.Id, ToStepId = stepAdCoord.Id, IsDefault = true };

        // AD Coordinator â†’ BCD
        var t_adCoord_bcd = new StepTransition { FromStepId = stepAdCoord.Id, ToStepId = stepBcd.Id, IsDefault = true };

        // BCD â†’ DDFC Admin
        var t_bcd_ddfcAdmin = new StepTransition { FromStepId = stepBcd.Id, ToStepId = stepDdfcAdmin.Id, IsDefault = true };

        // DDFC Admin -> Package Selection (undertaking handled as button in Architecture step)
        var t_ddfcAdmin_5 = new StepTransition { FromStepId = stepDdfcAdmin.Id, ToStepId = step5.Id, IsDefault = true };

        // Package â†’ Payment
        var t5_6 = new StepTransition { FromStepId = step5.Id, ToStepId = step6.Id, IsDefault = true };

        // Payment â†’ Admin Review
        var t6_admin = new StepTransition { FromStepId = step6.Id, ToStepId = stepAdmin.Id, IsDefault = true };

        // Admin Review â†’ Soil Test
        var t_admin_soil = new StepTransition { FromStepId = stepAdmin.Id, ToStepId = stepSoilTest.Id, IsDefault = true };

        // Soil Test â†’ PA Initial Review
        var t_soil_PA = new StepTransition { FromStepId = stepSoilTest.Id, ToStepId = stepPAInitial.Id, IsDefault = true };

        // PA Initial Review â†’ Architecture
        var t6_7 = new StepTransition { FromStepId = stepPAInitial.Id, ToStepId = step7.Id, IsDefault = true };

        // Architecture â†’ 3D Visualization
        var t7_3D = new StepTransition
        {
            FromStepId = step7.Id,
            ToStepId = step3D.Id,
            Condition = "action=Customer Approves Plan",
            IsDefault = false,
            Priority = 1
        };

        // 3D finalization â†’ Architect drafter assignment/upload
        var t3D_draft = new StepTransition { FromStepId = step3D.Id, ToStepId = stepDraft.Id, IsDefault = true };

        // Architect draft upload â†’ Structure
        var tDraft_8 = new StepTransition { FromStepId = stepDraft.Id, ToStepId = step8.Id, IsDefault = true };

        // Structure â†’ MEP
        var t8_9 = new StepTransition { FromStepId = step8.Id, ToStepId = step9.Id, IsDefault = true };

        // MEP â†’ PA Design Review
        var t9_10 = new StepTransition { FromStepId = step9.Id, ToStepId = step10.Id, IsDefault = true };

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
            t1_documents, t_documents_2, t2_3,
            t3_adCoord, t_adCoord_bcd, t_bcd_ddfcAdmin,
            t_ddfcAdmin_5,
            t5_6, t6_admin, t_admin_soil, t_soil_PA, t6_7,
            t7_3D, t3D_draft, tDraft_8, t8_9, t9_10,
            t10_11,
            t11_13,
            t13_14
        );

        await db.SaveChangesAsync();

        return process.Id;
    }
}


