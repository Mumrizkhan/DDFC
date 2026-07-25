using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorkflowEngine.Domain.Entities;
using WorkflowEngine.Domain.Enums;
using WorkflowEngine.Infrastructure.Persistence;

namespace WorkflowEngine.Infrastructure.Data;

public static class DataSeeder
{
    public static async Task SeedSampleProcessAsync(IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();

        if (await db.Processes.AnyAsync()) return;
        #region commented out code
        //// =========================================================================
        //// PROCESS 1: Loan Approval with Configurable Action Completion
        //// =========================================================================

        //var process = new Process { Name = "Loan Approval with Configurable Action Completion" };

        //// Step 1: Submit Documents - ANY action mode (any upload method works)
        //var step1 = new ProcessStep 
        //{ 
        //    Name = "Submit Documents", 
        //    Order = 1,
        //    ActionCompletionMode = ActionCompletionMode.Any  // Any one document upload method is sufficient
        //};
        //step1.Actions.Add(new StepAction { Name = "Upload Document", ActionType = ActionType.Upload });
        //step1.Actions.Add(new StepAction { Name = "Verify Documents", ActionType = ActionType.Verify });

        //// Step 2: Credit Check - ALL actions mode (default)
        //var step2 = new ProcessStep 
        //{ 
        //    Name = "Credit Check", 
        //    Order = 2,
        //    ActionCompletionMode = ActionCompletionMode.All  // All checks must complete
        //};
        //step2.Actions.Add(new StepAction { Name = "Run Credit Report", ActionType = ActionType.Check });

        //// Parallel Step 3: Compliance Review - MINIMUM 1 out of 2 actions
        //var step3 = new ProcessStep 
        //{ 
        //    Name = "Compliance Review", 
        //    Order = 3,
        //    ActionCompletionMode = ActionCompletionMode.Minimum,
        //    MinimumActionCount = 1  // Need at least 1 out of 2 checks
        //};
        //step3.Actions.Add(new StepAction { Name = "Check Regulations", ActionType = ActionType.Review });
        //step3.Actions.Add(new StepAction { Name = "Verify Compliance", ActionType = ActionType.Verify });

        //// Parallel Step 4: Risk Assessment - ANY action mode
        //var step4 = new ProcessStep 
        //{ 
        //    Name = "Risk Assessment", 
        //    Order = 4,
        //    ActionCompletionMode = ActionCompletionMode.Any  // Any assessment method works
        //};
        //step4.Actions.Add(new StepAction { Name = "Assess Risk", ActionType = ActionType.Assessment });

        //// Parallel Step 5: Background Verification - MINIMUM 1 out of 2
        //var step5 = new ProcessStep 
        //{ 
        //    Name = "Background Verification", 
        //    Order = 5,
        //    ActionCompletionMode = ActionCompletionMode.Minimum,
        //    MinimumActionCount = 1  // At least 1 verification method
        //};
        //step5.Actions.Add(new StepAction { Name = "Verify Identity", ActionType = ActionType.Verify });
        //step5.Actions.Add(new StepAction { Name = "Check History", ActionType = ActionType.Check });

        //// Step 6: Manager Approval - ANY action (any manager can approve OR reject)
        //var step6 = new ProcessStep 
        //{ 
        //    Name = "Manager Approval", 
        //    Order = 6,
        //    ActionCompletionMode = ActionCompletionMode.Any  // First manager to decide wins
        //};
        //step6.Actions.Add(new StepAction 
        //{ 
        //    Name = "Approve Application", 
        //    ActionType = ActionType.Approval,
        //    IsRejectionAction = false  // Normal approval action
        //});
        //step6.Actions.Add(new StepAction 
        //{ 
        //    Name = "Reject Application", 
        //    ActionType = ActionType.Rejection,
        //    IsRejectionAction = true  // This will end the process as Rejected!
        //});

        //// Step 7: Final Processing - ALL actions (everything must complete)
        //var step7 = new ProcessStep 
        //{ 
        //    Name = "Final Processing", 
        //    Order = 7,
        //    ActionCompletionMode = ActionCompletionMode.All  // All final steps required
        //};
        //step7.Actions.Add(new StepAction { Name = "Generate Contract", ActionType = ActionType.Generate });

        //process.Steps.Add(step1);
        //process.Steps.Add(step2);
        //process.Steps.Add(step3);
        //process.Steps.Add(step4);
        //process.Steps.Add(step5);
        //process.Steps.Add(step6);
        //process.Steps.Add(step7);

        //db.Processes.Add(process);
        //await db.SaveChangesAsync();

        //// ----------------------------------------------------------------------------
        //// Transitions for Loan Approval Process
        //// ----------------------------------------------------------------------------

        //// SCENARIO 1: Very High Credit Score (≥750) - "ANY" Step Completion Mode
        //var transition1 = new StepTransition
        //{
        //    FromStepId = step2.Id,
        //    ToStepId = step3.Id,
        //    Condition = "credit_score>=750",
        //    Priority = 1,
        //    IsParallel = true,
        //    ParallelCompletionMode = ParallelCompletionMode.Any
        //};

        //var transition2 = new StepTransition
        //{
        //    FromStepId = step2.Id,
        //    ToStepId = step4.Id,
        //    Condition = "credit_score>=750",
        //    Priority = 1,
        //    IsParallel = true,
        //    ParallelCompletionMode = ParallelCompletionMode.Any
        //};

        //var transition3 = new StepTransition
        //{
        //    FromStepId = step2.Id,
        //    ToStepId = step5.Id,
        //    Condition = "credit_score>=750",
        //    Priority = 1,
        //    IsParallel = true,
        //    ParallelCompletionMode = ParallelCompletionMode.Any
        //};

        //// SCENARIO 2: Good Credit Score (700-749) - "MINIMUM 2" Step Completion Mode
        //var transition4 = new StepTransition
        //{
        //    FromStepId = step2.Id,
        //    ToStepId = step3.Id,
        //    Condition = "credit_score>=700",
        //    Priority = 2,
        //    IsParallel = true,
        //    ParallelCompletionMode = ParallelCompletionMode.Minimum,
        //    MinimumCompletionCount = 2
        //};

        //var transition5 = new StepTransition
        //{
        //    FromStepId = step2.Id,
        //    ToStepId = step4.Id,
        //    Condition = "credit_score>=700",
        //    Priority = 2,
        //    IsParallel = true,
        //    ParallelCompletionMode = ParallelCompletionMode.Minimum,
        //    MinimumCompletionCount = 2
        //};

        //var transition6 = new StepTransition
        //{
        //    FromStepId = step2.Id,
        //    ToStepId = step5.Id,
        //    Condition = "credit_score>=700",
        //    Priority = 2,
        //    IsParallel = true,
        //    ParallelCompletionMode = ParallelCompletionMode.Minimum,
        //    MinimumCompletionCount = 2
        //};

        //// SCENARIO 3: Low Credit Score (<700) - No Parallel Execution
        //var transition7 = new StepTransition
        //{
        //    FromStepId = step2.Id,
        //    ToStepId = step6.Id,
        //    Condition = "credit_score<700",
        //    Priority = 3,
        //    IsDefault = true,
        //    IsParallel = false
        //};

        //// Convergence transitions
        //var transition8 = new StepTransition
        //{
        //    FromStepId = step3.Id,
        //    ToStepId = step6.Id,
        //    IsDefault = true
        //};

        //var transition9 = new StepTransition
        //{
        //    FromStepId = step4.Id,
        //    ToStepId = step6.Id,
        //    IsDefault = true
        //};

        //var transition10 = new StepTransition
        //{
        //    FromStepId = step5.Id,
        //    ToStepId = step6.Id,
        //    IsDefault = true
        //};

        //var transition11 = new StepTransition
        //{
        //    FromStepId = step6.Id,
        //    ToStepId = step7.Id,
        //    Condition = "approval=approved",
        //    Priority = 1,
        //    IsDefault = true
        //};

        //db.StepTransitions.AddRange(
        //    transition1, transition2, transition3, transition4,
        //    transition5, transition6, transition7, transition8,
        //    transition9, transition10, transition11);

        //await db.SaveChangesAsync();
        #endregion

        // =========================================================================
        // PROCESS 2: Add Asset to Asset Bank
        // =========================================================================
        // Sequential approval workflow with rejection capabilities at each stage

        var assetProcess = new Process { Name = "Add Asset to Asset Bank" };
        
        // Step 1: School Planning - Verification step
        var assetStep1 = new ProcessStep
        {
            Name = "School Planning",
            Order = 1,
            ActionCompletionMode = ActionCompletionMode.All  // Must complete verification
        };
        assetStep1.Actions.Add(new StepAction 
        { 
            Name = "Verify Asset Details", 
            ActionType = ActionType.Verify 
        });

        // Step 2: Safety, Security, and Facilities - Approval/Rejection
        var assetStep2 = new ProcessStep
        {
            Name = "Safety, Security, and Facilities",
            Order = 2,
            ActionCompletionMode = ActionCompletionMode.Any  // Either approve or reject
        };
        assetStep2.Actions.Add(new StepAction 
        { 
            Name = "Approve", 
            ActionType = ActionType.Approval,
            IsRejectionAction = false
        });
        assetStep2.Actions.Add(new StepAction 
        { 
            Name = "Reject", 
            ActionType = ActionType.Rejection,
            IsRejectionAction = true  // Ends process if rejected
        });

        // Step 3: Investment and Partnerships - Approval/Rejection
        var assetStep3 = new ProcessStep
        {
            Name = "Investment and Partnerships",
            Order = 3,
            ActionCompletionMode = ActionCompletionMode.Any  // Either approve or reject
        };
        assetStep3.Actions.Add(new StepAction 
        { 
            Name = "Approve", 
            ActionType = ActionType.Approval,
            IsRejectionAction = false
        });
        assetStep3.Actions.Add(new StepAction 
        { 
            Name = "Reject", 
            ActionType = ActionType.Rejection,
            IsRejectionAction = true  // Ends process if rejected
        });

        // Step 4: Investment Agency - Approval/Rejection
        var assetStep4 = new ProcessStep
        {
            Name = "Investment Agency",
            Order = 4,
            ActionCompletionMode = ActionCompletionMode.Any  // Either approve or reject
        };
        assetStep4.Actions.Add(new StepAction 
        { 
            Name = "Approve", 
            ActionType = ActionType.Approval,
            IsRejectionAction = false
        });
        assetStep4.Actions.Add(new StepAction 
        { 
            Name = "Reject", 
            ActionType = ActionType.Rejection,
            IsRejectionAction = true  // Ends process if rejected
        });

        // Step 5: TBC - Approval/Rejection (Final approval)
        var assetStep5 = new ProcessStep
        {
            Name = "TBC",
            Order = 5,
            ActionCompletionMode = ActionCompletionMode.Any  // Either approve or reject
        };
        assetStep5.Actions.Add(new StepAction 
        { 
            Name = "Approve", 
            ActionType = ActionType.Approval,
            IsRejectionAction = false
        });
        assetStep5.Actions.Add(new StepAction 
        { 
            Name = "Reject", 
            ActionType = ActionType.Rejection,
            IsRejectionAction = true  // Ends process if rejected
        });

        // Add all steps to the process
        assetProcess.Steps.Add(assetStep1);
        assetProcess.Steps.Add(assetStep2);
        assetProcess.Steps.Add(assetStep3);
        assetProcess.Steps.Add(assetStep4);
        assetProcess.Steps.Add(assetStep5);

        db.Processes.Add(assetProcess);
        await db.SaveChangesAsync();

        // ----------------------------------------------------------------------------
        // Transitions for Asset Bank Process (Sequential Flow)
        // ----------------------------------------------------------------------------
        // These are default transitions that move sequentially through the steps
        // If any step is rejected, the process ends automatically (via IsRejectionAction)
        
        var assetTransition1 = new StepTransition
        {
            FromStepId = assetStep1.Id,
            ToStepId = assetStep2.Id,
            IsDefault = true  // Automatically move to next step after verification
        };

        var assetTransition2 = new StepTransition
        {
            FromStepId = assetStep2.Id,
            ToStepId = assetStep3.Id,
            IsDefault = true  // Move to next step if approved (rejection ends process)
        };

        var assetTransition3 = new StepTransition
        {
            FromStepId = assetStep3.Id,
            ToStepId = assetStep4.Id,
            IsDefault = true  // Move to next step if approved
        };

        var assetTransition4 = new StepTransition
        {
            FromStepId = assetStep4.Id,
            ToStepId = assetStep5.Id,
            IsDefault = true  // Move to final step if approved
        };

        // No transition from assetStep5 - process completes after final approval

        db.StepTransitions.AddRange(
            assetTransition1, assetTransition2, assetTransition3, assetTransition4);
        
        await db.SaveChangesAsync();

        // ============================================================================
        // SUMMARY
        // ============================================================================
        //
        // PROCESS 1: Loan Approval
        //   - Complex workflow with parallel steps and conditional routing
        //   - Multiple action completion modes (ALL, ANY, MINIMUM)
        //   - Parallel step completion modes
        //
        // PROCESS 2: Add Asset to Asset Bank
        //   - Sequential approval workflow
        //   - Each approval step can approve or reject
        //   - Rejection at any stage ends the process immediately
        //   - Steps flow sequentially: School Planning → Safety/Security → 
        //     Investment/Partnerships → Investment Agency → TBC
        //
        // ============================================================================
    }
}
