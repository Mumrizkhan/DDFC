
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkflowEngine.Application.Interfaces;
using WorkflowEngine.Application.Services;
using WorkflowEngine.Domain.Enums;
using WorkflowEngine.Infrastructure.Data;
using WorkflowEngine.Infrastructure.Persistence;
using WorkflowEngine.Infrastructure.Repositories;

namespace WorkflowEngine;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Setup DI and EF InMemory for demo
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        services.AddDbContext<WorkflowDbContext>(opts => opts.UseInMemoryDatabase("workflow_demo"));
        services.AddScoped<IWorkflowRepository, WorkflowRepository>();
        services.AddScoped<IWorkflowEngine, WorkflowEngineService>();

        var provider = services.BuildServiceProvider();

        // Seed a sample process with configurable action completion
        await DataSeeder.SeedSampleProcessAsync(provider);

        var engine = provider.GetRequiredService<IWorkflowEngine>();

     
    
        #region Commented Out Scenarios
        // Get the Asset Bank process directly from DbContext

        //Console.WriteLine("=== Workflow Engine: Configurable Action Completion Modes ===\n");
        //Console.WriteLine("✨ NEW: Each step can configure how many actions must complete");
        //Console.WriteLine("    - ALL: All actions must complete (traditional)");
        //Console.WriteLine("    - ANY: Any single action with data completes the step");
        //Console.WriteLine("    - MINIMUM: N actions must complete before step completes\n");
       

        //// Scenario 1: High credit score with ANY/MINIMUM action modes
        //Console.WriteLine("--- Scenario 1: Configurable Action Completion ---\n");
   
        //var request1 = await engine.StartRequestAsync(new Guid());
        //Console.WriteLine($"Started Request Id: {request1.Id}\n");
        // =========================================================================
        // SCENARIO 1: Successful Asset Addition (All Approvals)
        // =========================================================================

        //// Step 1: Submit Documents (ANY action mode - complete any one)
        //request1 = await engine.GetRequestAsync(request1.Id);
        //var step1 = request1.Steps.FirstOrDefault(s => s.Status == RequestStepStatus.Active);
        //if (step1 != null)
        //{
        //    Console.WriteLine($"Step: {step1.ProcessStep.Name} [Action Mode: {step1.ActionCompletionMode}]");
        //    Console.WriteLine($"  Actions available: {step1.Actions.Count}");
        //    foreach (var a in step1.Actions)
        //    {
        //        Console.WriteLine($"    - {a.StepAction.Name}");
        //    }
     
        //    // Complete ONLY the first action with data - step completes due to ANY mode
        //    var firstAction = step1.Actions.First();
        //    await engine.CompleteActionAsync(firstAction.Id, "user1", "{\"documents_verified\": true}");
        //    Console.WriteLine($"  ✓ Completed ONLY '{firstAction.StepAction.Name}' with data");
        //    Console.WriteLine("  → Step completed (ANY mode), other action auto-skipped!\n");
        //}
       
        //// Step 2: Credit Check (ALL actions mode - must complete all)
        //request1 = await engine.GetRequestAsync(request1.Id);
        //var step2 = request1.Steps.FirstOrDefault(s => s.Status == RequestStepStatus.Active);
        //if (step2 != null)
        //{
        //    Console.WriteLine($"Step: {step2.ProcessStep.Name} [Action Mode: {step2.ActionCompletionMode}]");
        //    Console.WriteLine($"  Actions: {step2.Actions.Count}");
        //    var firstAction = step2.Actions.First();
        //    await engine.CompleteActionAsync(firstAction.Id, "system", "{\"credit_score\": 780}");
        //    Console.WriteLine($"  ✓ Completed all actions (ALL mode required)\n");
        //}
        // Step 1: School Planning - Verify Asset Details
     
            //// Should have 3 parallel active steps with ANY parallel completion mode
            //request1 = await engine.GetRequestAsync(request1.Id);
            //var activeSteps = request1.Steps.Where(s => s.Status == RequestStepStatus.Active).ToList();
            //Console.WriteLine($"⚡ {activeSteps.Count} Parallel Steps Activated:");
            //foreach (var step in activeSteps)
            //{
            //    Console.WriteLine($"   - {step.ProcessStep.Name} [Action Mode: {step.ActionCompletionMode}]");
            //    Console.WriteLine($"     Has {step.Actions.Count} actions, Mode: {step.ActionCompletionMode}");
            //}
            //Console.WriteLine();
        
            //// Complete Compliance Review (MINIMUM 1 action out of 2)
            //var complianceStep = activeSteps.FirstOrDefault(s => s.ProcessStep.Name == "Compliance Review");
            //if (complianceStep != null)
            //{
            //    Console.WriteLine($"Completing {complianceStep.ProcessStep.Name}:");
            //    Console.WriteLine($"  Action Mode: {complianceStep.ActionCompletionMode}");
            //    Console.WriteLine($"  Minimum Required: {complianceStep.MinimumActionCount} out of {complianceStep.Actions.Count}");
          

        //    // Complete ONLY first action
        //    var firstAction = complianceStep.Actions.First();
        //    await engine.CompleteActionAsync(firstAction.Id, "compliance_officer", "{\"compliance_passed\": true}");
        //    Console.WriteLine($"  ✓ Completed '{firstAction.StepAction.Name}'");
        //    Console.WriteLine($"  → Minimum reached (1/2), other action auto-skipped");
        //    Console.WriteLine($"  → This step completes first, triggering ANY parallel mode");
        //    Console.WriteLine($"  → Other parallel steps (Risk, Background) auto-skipped\n");
        //}
        // Step 2: Safety, Security, and Facilities - Approve
      

            //// Should now be at Manager Approval
            //request1 = await engine.GetRequestAsync(request1.Id);
            //var currentStep = request1.Steps.FirstOrDefault(s => s.Status == RequestStepStatus.Active);
            //Console.WriteLine($"Current Step: {currentStep?.ProcessStep.Name} [Action Mode: {currentStep?.ActionCompletionMode}]\n");
     
                //// Complete Manager Approval (ANY action mode)
                //if (currentStep != null)
                //{
                //    Console.WriteLine($"Completing Manager Approval:");
                //    Console.WriteLine($"  Action Mode: {currentStep.ActionCompletionMode} (first manager to approve wins)");
                //    Console.WriteLine($"  Available actions: {currentStep.Actions.Count}");
                //    foreach (var a in currentStep.Actions)
                //    {
                //        Console.WriteLine($"    - {a.StepAction.Name}");
                //    }
              
        //    // Complete first action - due to ANY mode, second action gets skipped
        //    var firstAction = currentStep.Actions.First();
        //    await engine.CompleteActionAsync(firstAction.Id, "manager1", "{\"approval\": \"approved\"}");
        //    Console.WriteLine($"  ✓ Manager 1 approved first");
        //    Console.WriteLine($"  → Step completed (ANY mode), other manager action auto-skipped\n");
        //}
        // Step 3: Investment and Partnerships - Approve
  
            //// Complete Final Processing (ALL actions mode)
            //request1 = await engine.GetRequestAsync(request1.Id);
            //var finalStep1 = request1.Steps.FirstOrDefault(s => s.Status == RequestStepStatus.Active);
            //if (finalStep1 != null)
            //{
            //    Console.WriteLine($"Step: {finalStep1.ProcessStep.Name} [Action Mode: {finalStep1.ActionCompletionMode}]");
            //    var firstAction = finalStep1.Actions.First();
            //    await engine.CompleteActionAsync(firstAction.Id, "system", "{\"contract_generated\": true}");
            //    Console.WriteLine($"  ✓ Completed all required actions (ALL mode)\n");
            //}
      

        //Console.WriteLine("===========================================\n");
        // Step 4: Investment Agency - Approve
     
            //// Scenario 2: Show MINIMUM action mode with parallel MINIMUM step mode
            //Console.WriteLine("--- Scenario 2: MINIMUM Action + MINIMUM Step Modes ---\n");
     

        //// Complete Submit Documents
        //request2 = await engine.GetRequestAsync(request2.Id);
        //step1 = request2.Steps.FirstOrDefault(s => s.Status == RequestStepStatus.Active);
        //if (step1 != null)
        //{
        //    var firstAction = step1.Actions.First();
        //    await engine.CompleteActionAsync(firstAction.Id, "user2", "{\"docs\": \"uploaded\"}");
        //    Console.WriteLine($"✓ {step1.ProcessStep.Name} completed (ANY action mode)\n");
        //}
        // Step 5: TBC (Final Approval)
    
            //// Complete Credit Check
            //request2 = await engine.GetRequestAsync(request2.Id);
            //step2 = request2.Steps.FirstOrDefault(s => s.Status == RequestStepStatus.Active);
            //if (step2 != null)
            //{
            //    var firstAction = step2.Actions.First();
            //    await engine.CompleteActionAsync(firstAction.Id, "system", "{\"credit_score\": 720}");
            //    Console.WriteLine($"✓ {step2.ProcessStep.Name} completed\n");
            //}
       
                //// Complete MINIMUM step mode parallel steps (need 2 out of 3 steps)
                //request2 = await engine.GetRequestAsync(request2.Id);
                //activeSteps = request2.Steps.Where(s => s.Status == RequestStepStatus.Active).ToList();
                //Console.WriteLine($"⚡ {activeSteps.Count} Parallel Steps Activated (MINIMUM 2 mode):");
                //foreach (var step in activeSteps)
                //{
                //    Console.WriteLine($"   - {step.ProcessStep.Name}");
                //    Console.WriteLine($"     Actions: {step.Actions.Count}, Mode: {step.ActionCompletionMode}");
                //}
                //Console.WriteLine();
         

        //// Complete Compliance Review (MINIMUM 1 action)
        //complianceStep = activeSteps.FirstOrDefault(s => s.ProcessStep.Name == "Compliance Review");
        //if (complianceStep != null)
        //{
        //    var firstAction = complianceStep.Actions.First();
        //    await engine.CompleteActionAsync(firstAction.Id, "officer", "{\"result\": \"pass\"}");
        //    Console.WriteLine($"✓ Compliance Review: Completed 1/{complianceStep.Actions.Count} actions (minimum met)");
        //    Console.WriteLine($"  → 1/2 parallel steps done, waiting for one more...\n");
        //}
      
        // =========================================================================
        // SCENARIO 2: Rejection at Safety, Security, and Facilities
        // =========================================================================

      
        // Step 1: School Planning - Verify
     

        // Step 2: Safety, Security, and Facilities - REJECT
     

            //// Complete Risk Assessment (ANY action) - satisfies parallel MINIMUM!
            //request2 = await engine.GetRequestAsync(request2.Id);
            //var riskStep2 = request2.Steps.FirstOrDefault(s => 
            //    s.ProcessStep.Name == "Risk Assessment" && s.Status == RequestStepStatus.Active);
            //if (riskStep2 != null)
            //{
            //    var firstAction = riskStep2.Actions.First();
            //    await engine.CompleteActionAsync(firstAction.Id, "analyst", "{\"risk\": \"low\"}");
            //    Console.WriteLine($"✓ Risk Assessment: Completed 1/{riskStep2.Actions.Count} actions (ANY mode)");
            //    Console.WriteLine($"  → 2/2 parallel steps done (MINIMUM 2 satisfied!)");
            //    Console.WriteLine($"  → Background Verification auto-skipped\n");
            //}
      
                //// Complete remaining steps
                //request2 = await engine.GetRequestAsync(request2.Id);
                //currentStep = request2.Steps.FirstOrDefault(s => s.Status == RequestStepStatus.Active);
                //if (currentStep != null)
                //{
                //    var firstAction = currentStep.Actions.First();
                //    await engine.CompleteActionAsync(firstAction.Id, "manager", "{\"approval\": \"approved\"}");
                //    Console.WriteLine($"✓ {currentStep.ProcessStep.Name} completed (ANY action mode)\n");
                //}
        

        //request2 = await engine.GetRequestAsync(request2.Id);
        //var finalStep2 = request2.Steps.FirstOrDefault(s => s.Status == RequestStepStatus.Active);
        //if (finalStep2 != null)
        //{
        //    var firstAction = finalStep2.Actions.First();
        //    await engine.CompleteActionAsync(firstAction.Id, "system", "{\"done\": true}");
        //    Console.WriteLine($"✓ {finalStep2.ProcessStep.Name} completed\n");
        //}
 
        //request2 = await engine.GetRequestAsync(request2.Id);
        //Console.WriteLine($"Final Status: {request2.Status}\n");
       

        //Console.WriteLine("===========================================");
        //Console.WriteLine("✨ Summary: Dual-Level Configurability");
        //Console.WriteLine("\n  ACTION LEVEL (within each step):");
        //Console.WriteLine("    - ALL: All actions must complete");
        //Console.WriteLine("    - ANY: Any single action completes the step");
        //Console.WriteLine("    - MINIMUM: N actions must complete");
        //Console.WriteLine("\n  STEP LEVEL (parallel steps):");
        //Console.WriteLine("    - ALL: All parallel steps must complete");
        //Console.WriteLine("    - ANY: Any single parallel step completes");
        //Console.WriteLine("    - MINIMUM: N parallel steps must complete");
        //Console.WriteLine("\n  🚀 Maximum efficiency with configurable completion at both levels!");
        // =========================================================================
        // SCENARIO 3: Rejection at Investment Agency (Late Stage)
        // =========================================================================

        

        // Scenario 3: Rejection Action Demo
        //Console.WriteLine("--- Scenario 3: Rejection Action (Process Ends Immediately) ---\n");
       


        //var request3 = await engine.StartRequestAsync(new Guid());
        //Console.WriteLine($"Started Request Id: {request3.Id}\n");
        // Complete Steps 1-3 successfully
      

        //// Complete Submit Documents
        //request3 = await engine.GetRequestAsync(request3.Id);
        //step1 = request3.Steps.FirstOrDefault(s => s.Status == RequestStepStatus.Active);
        //if (step1 != null)
        //{
        //    var firstAction = step1.Actions.First();
        //    await engine.CompleteActionAsync(firstAction.Id, "user3", "{\"docs\": \"uploaded\"}");
        //    Console.WriteLine($"✓ {step1.ProcessStep.Name} completed\n");
        //}
        // Step 1
     


        //// Complete Credit Check
        //request3 = await engine.GetRequestAsync(request3.Id);
        //step2 = request3.Steps.FirstOrDefault(s => s.Status == RequestStepStatus.Active);
        //if (step2 != null)
        //{
        //    var firstAction = step2.Actions.First();
        //    await engine.CompleteActionAsync(firstAction.Id, "system", "{\"credit_score\": 720}");
        //    Console.WriteLine($"✓ {step2.ProcessStep.Name} completed (Score: 720)\n");
        //}
        // Step 2



        //// Skip parallel steps by completing them quickly
        //request3 = await engine.GetRequestAsync(request3.Id);
        //activeSteps = request3.Steps.Where(s => s.Status == RequestStepStatus.Active).ToList();
        //Console.WriteLine($"⚡ {activeSteps.Count} Parallel Steps Activated\n");
        // Step 3
   


        //// Complete minimum required parallel steps
        //complianceStep = activeSteps.FirstOrDefault(s => s.ProcessStep.Name == "Compliance Review");
        //if (complianceStep != null)
        //{
        //    var firstAction = complianceStep.Actions.First();
        //    await engine.CompleteActionAsync(firstAction.Id, "officer", "{\"result\": \"pass\"}");
        //    Console.WriteLine($"✓ {complianceStep.ProcessStep.Name} completed (1/2 steps)\n");
        //}
        // Step 4: Investment Agency - REJECT
  


            //request3 = await engine.GetRequestAsync(request3.Id);
            //var riskStep3 = request3.Steps.FirstOrDefault(s => 
            //    s.ProcessStep.Name == "Risk Assessment" && s.Status == RequestStepStatus.Active);
            //if (riskStep3 != null)
            //{
            //    var firstAction = riskStep3.Actions.First();
            //    await engine.CompleteActionAsync(firstAction.Id, "analyst", "{\"risk\": \"acceptable\"}");
            //    Console.WriteLine($"✓ {riskStep3.ProcessStep.Name} completed (2/2 steps - minimum satisfied)\n");
            //}
        



        //if (currentStep != null)
        //{
        //    Console.WriteLine($"Current Step: {currentStep.ProcessStep.Name}");
        //    Console.WriteLine($"  Available actions: {currentStep.Actions.Count}");
        //    foreach (var a in currentStep.Actions)
        //    {
        //        Console.WriteLine($"    - {a.StepAction.Name} {(a.StepAction.IsRejectionAction ? "(REJECTION)" : "(APPROVAL)")}");
        //    }
        //    Console.WriteLine();
     


        //    // Find and complete the REJECTION action
        //    var rejectAction = currentStep.Actions.FirstOrDefault(a => a.StepAction.IsRejectionAction);
        //    if (rejectAction != null)
        //    {
        //        await engine.CompleteActionAsync(
        //            rejectAction.Id, 
        //            "manager", 
        //            "{\"rejection_reason\": \"Insufficient credit history\"}");
        // ==========================================================================
        // SUMMARY
        // ==========================================================================

        //        Console.WriteLine($"✗ REJECTED by manager");
        //        Console.WriteLine($"  → Request status changed to Rejected");
        //        Console.WriteLine($"  → All remaining steps auto-skipped");
        //        Console.WriteLine($"  → Process ended immediately\n");
        //    }
        //}
       


        //request3 = await engine.GetRequestAsync(request3.Id);
        //Console.WriteLine($"Final Status: {request3.Status}");
       


        //// Count skipped steps
        //var skippedSteps = request3.Steps.Count(s => s.Status == RequestStepStatus.Skipped);
        //var skippedActions = request3.Steps
        //    .SelectMany(s => s.Actions)
        //    .Count(a => a.Data?.Contains("[AUTO-SKIPPED] Request rejected") == true);
     


        //Console.WriteLine($"  Skipped Steps: {skippedSteps}");
        //Console.WriteLine($"  Skipped Actions: {skippedActions}\n");
  


        //Console.WriteLine("===========================================");
        //Console.WriteLine("✨ Summary: Complete Feature Set");
        //Console.WriteLine("\n  🎯 DUAL-LEVEL CONFIGURABILITY:");
        //Console.WriteLine("    - Action Level: ALL, ANY, MINIMUM");
        //Console.WriteLine("    - Step Level: ALL, ANY, MINIMUM");
        //Console.WriteLine("\n  🚫 REJECTION HANDLING:");
        //Console.WriteLine("    - Mark actions as rejection actions");
        //Console.WriteLine("    - Completing rejection action ends process");
        //Console.WriteLine("    - Status: Rejected (distinct from Failed/Cancelled)");
        //Console.WriteLine("    - All remaining steps/actions auto-skipped");
        //Console.WriteLine("\n  🚀 Maximum flexibility and efficiency!");
        #endregion
 



        // Get the Asset Bank process directly from DbContext
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();
        var assetProcess = await db.Processes.FirstOrDefaultAsync(p => p.Name == "Add Asset to Asset Bank");

        if (assetProcess == null)
        {
            Console.WriteLine("Error: 'Add Asset to Asset Bank' process not found!");
            return;
        }

        Console.WriteLine("===============================================================");
        Console.WriteLine("   WORKFLOW ENGINE: ADD ASSET TO ASSET BANK PROCESS DEMO");
        Console.WriteLine("===============================================================\n");
        Console.WriteLine("Process: Add Asset to Asset Bank");
        Console.WriteLine("Type: Sequential Approval Workflow");
        Console.WriteLine("Steps: School Planning → Safety/Security → Investment/Partnerships");
        Console.WriteLine("       → Investment Agency → TBC (Final Approval)");
        Console.WriteLine("\nFeatures:");
        Console.WriteLine("  ✓ Sequential step execution");
        Console.WriteLine("  ✓ Approve/Reject at each approval stage");
        Console.WriteLine("  ✓ Rejection ends process immediately");
        Console.WriteLine("  ✓ All remaining steps auto-skipped on rejection\n");
        Console.WriteLine("===============================================================\n");

        // =========================================================================
        // SCENARIO 1: Successful Asset Addition (All Approvals)
        // =========================================================================
        
        Console.WriteLine("┌─────────────────────────────────────────────────────────────┐");
        Console.WriteLine("│ SCENARIO 1: Successful Asset Addition (Full Approval Path) │");
        Console.WriteLine("└─────────────────────────────────────────────────────────────┘\n");

        var request1 = await engine.StartRequestAsync(assetProcess.Id);
        Console.WriteLine($"✓ Started Request: {request1.Id}");
        Console.WriteLine($"  Process: {assetProcess.Name}");
        Console.WriteLine($"  Status: {request1.Status}\n");

        // Step 1: School Planning - Verify Asset Details
        request1 = await engine.GetRequestAsync(request1.Id);
        var step1 = request1.Steps.FirstOrDefault(s => s.Status == RequestStepStatus.Active);
        if (step1 != null)
        {
            Console.WriteLine($"► Step 1: {step1.ProcessStep.Name}");
            Console.WriteLine($"  Action Mode: {step1.ActionCompletionMode}");
            Console.WriteLine($"  Actions Required: {step1.Actions.Count}");
            
            var verifyAction = step1.Actions.First();
            Console.WriteLine($"\n  Completing: {verifyAction.StepAction.Name}");
            await engine.CompleteActionAsync(
                verifyAction.Id, 
                "planning_officer", 
                "{\"asset_type\": \"school_building\", \"location\": \"District A\", \"size_sqm\": 5000, \"verified\": true}");
            
            Console.WriteLine($"  ✓ Asset details verified");
            Console.WriteLine($"  → Step completed, moving to next step\n");
        }

        // Step 2: Safety, Security, and Facilities - Approve
        request1 = await engine.GetRequestAsync(request1.Id);
        var step2 = request1.Steps.FirstOrDefault(s => s.Status == RequestStepStatus.Active);
        if (step2 != null)
        {
            Console.WriteLine($"► Step 2: {step2.ProcessStep.Name}");
            Console.WriteLine($"  Action Mode: {step2.ActionCompletionMode} (First action wins)");
            Console.WriteLine($"  Available Actions:");
            foreach (var a in step2.Actions)
            {
                Console.WriteLine($"    - {a.StepAction.Name} {(a.StepAction.IsRejectionAction ? "(REJECTION)" : "(APPROVAL)")}");
            }
            
            var approveAction = step2.Actions.FirstOrDefault(a => !a.StepAction.IsRejectionAction);
            if (approveAction != null)
            {
                Console.WriteLine($"\n  Completing: {approveAction.StepAction.Name}");
                await engine.CompleteActionAsync(
                    approveAction.Id, 
                    "safety_director", 
                    "{\"safety_compliance\": true, \"security_measures\": \"adequate\", \"facilities_rating\": 4.5, \"approved\": true}");
                
                Console.WriteLine($"  ✓ Approved by Safety, Security, and Facilities");
                Console.WriteLine($"  → Moving to next approval stage\n");
            }
        }

        // Step 3: Investment and Partnerships - Approve
        request1 = await engine.GetRequestAsync(request1.Id);
        var step3 = request1.Steps.FirstOrDefault(s => s.Status == RequestStepStatus.Active);
        if (step3 != null)
        {
            Console.WriteLine($"► Step 3: {step3.ProcessStep.Name}");
            Console.WriteLine($"  Action Mode: {step3.ActionCompletionMode}");
            
            var approveAction = step3.Actions.FirstOrDefault(a => !a.StepAction.IsRejectionAction);
            if (approveAction != null)
            {
                Console.WriteLine($"\n  Completing: {approveAction.StepAction.Name}");
                await engine.CompleteActionAsync(
                    approveAction.Id, 
                    "investment_manager", 
                    "{\"financial_viability\": true, \"partnership_opportunities\": [\"private_investor_a\", \"ngo_b\"], \"approved\": true}");
                
                Console.WriteLine($"  ✓ Approved by Investment and Partnerships");
                Console.WriteLine($"  → Moving to next approval stage\n");
            }
        }

        // Step 4: Investment Agency - Approve
        request1 = await engine.GetRequestAsync(request1.Id);
        var step4 = request1.Steps.FirstOrDefault(s => s.Status == RequestStepStatus.Active);
        if (step4 != null)
        {
            Console.WriteLine($"► Step 4: {step4.ProcessStep.Name}");
            Console.WriteLine($"  Action Mode: {step4.ActionCompletionMode}");
            
            var approveAction = step4.Actions.FirstOrDefault(a => !a.StepAction.IsRejectionAction);
            if (approveAction != null)
            {
                Console.WriteLine($"\n  Completing: {approveAction.StepAction.Name}");
                await engine.CompleteActionAsync(
                    approveAction.Id, 
                    "agency_director", 
                    "{\"regulatory_compliance\": true, \"funding_allocated\": true, \"approved\": true}");
                
                Console.WriteLine($"  ✓ Approved by Investment Agency");
                Console.WriteLine($"  → Moving to final approval stage\n");
            }
        }

        // Step 5: TBC (Final Approval)
        request1 = await engine.GetRequestAsync(request1.Id);
        var step5 = request1.Steps.FirstOrDefault(s => s.Status == RequestStepStatus.Active);
        if (step5 != null)
        {
            Console.WriteLine($"► Step 5: {step5.ProcessStep.Name} (Final Approval)");
            Console.WriteLine($"  Action Mode: {step5.ActionCompletionMode}");
            
            var approveAction = step5.Actions.FirstOrDefault(a => !a.StepAction.IsRejectionAction);
            if (approveAction != null)
            {
                Console.WriteLine($"\n  Completing: {approveAction.StepAction.Name}");
                await engine.CompleteActionAsync(
                    approveAction.Id, 
                    "tbc_director", 
                    "{\"final_review\": \"passed\", \"asset_id\": \"AST-2024-001\", \"approved\": true}");
                
                Console.WriteLine($"  ✓ Final approval granted by TBC");
                Console.WriteLine($"  → Process completed successfully!\n");
            }
        }

        request1 = await engine.GetRequestAsync(request1.Id);
        Console.WriteLine($"═══════════════════════════════════════════");
        Console.WriteLine($"Final Request Status: {request1.Status}");
        Console.WriteLine($"Total Steps: {request1.Steps.Count}");
        Console.WriteLine($"Completed Steps: {request1.Steps.Count(s => s.Status == RequestStepStatus.Completed)}");
        Console.WriteLine($"✓ Asset successfully added to Asset Bank!");
        Console.WriteLine($"═══════════════════════════════════════════\n\n");

        // =========================================================================
        // SCENARIO 2: Rejection at Safety, Security, and Facilities
        // =========================================================================
        
        Console.WriteLine("┌──────────────────────────────────────────────────────────────┐");
        Console.WriteLine("│ SCENARIO 2: Early Rejection (Safety, Security, Facilities)  │");
        Console.WriteLine("└──────────────────────────────────────────────────────────────┘\n");

        var request2 = await engine.StartRequestAsync(assetProcess.Id);
        Console.WriteLine($"✓ Started Request: {request2.Id}");
        Console.WriteLine($"  Status: {request2.Status}\n");

        // Step 1: School Planning - Verify
        request2 = await engine.GetRequestAsync(request2.Id);
        step1 = request2.Steps.FirstOrDefault(s => s.Status == RequestStepStatus.Active);
        if (step1 != null)
        {
            Console.WriteLine($"► Step 1: {step1.ProcessStep.Name}");
            var verifyAction = step1.Actions.First();
            await engine.CompleteActionAsync(
                verifyAction.Id, 
                "planning_officer", 
                "{\"asset_type\": \"school_building\", \"location\": \"District B\", \"verified\": true}");
            Console.WriteLine($"  ✓ Asset details verified\n");
        }

        // Step 2: Safety, Security, and Facilities - REJECT
        request2 = await engine.GetRequestAsync(request2.Id);
        step2 = request2.Steps.FirstOrDefault(s => s.Status == RequestStepStatus.Active);
        if (step2 != null)
        {
            Console.WriteLine($"► Step 2: {step2.ProcessStep.Name}");
            Console.WriteLine($"  Available Actions:");
            foreach (var a in step2.Actions)
            {
                Console.WriteLine($"    - {a.StepAction.Name} {(a.StepAction.IsRejectionAction ? "(REJECTION)" : "(APPROVAL)")}");
            }
            
            var rejectAction = step2.Actions.FirstOrDefault(a => a.StepAction.IsRejectionAction);
            if (rejectAction != null)
            {
                Console.WriteLine($"\n  Completing: {rejectAction.StepAction.Name}");
                await engine.CompleteActionAsync(
                    rejectAction.Id, 
                    "safety_director", 
                    "{\"rejection_reason\": \"Building does not meet fire safety standards\", \"details\": \"Missing sprinkler system and emergency exits\"}");
                
                Console.WriteLine($"  ✗ REJECTED by Safety, Security, and Facilities");
                Console.WriteLine($"  → Request status changed to Rejected");
                Console.WriteLine($"  → All remaining steps auto-skipped");
                Console.WriteLine($"  → Process ended immediately\n");
            }
        }

        request2 = await engine.GetRequestAsync(request2.Id);
        Console.WriteLine($"═══════════════════════════════════════════");
        Console.WriteLine($"Final Request Status: {request2.Status}");
        Console.WriteLine($"Completed Steps: {request2.Steps.Count(s => s.Status == RequestStepStatus.Completed)}");
        Console.WriteLine($"Skipped Steps: {request2.Steps.Count(s => s.Status == RequestStepStatus.Skipped)}");
        
        var skippedSteps = request2.Steps.Where(s => s.Status == RequestStepStatus.Skipped).ToList();
        if (skippedSteps.Any())
        {
            Console.WriteLine($"\nSkipped Steps (due to rejection):");
            foreach (var s in skippedSteps)
            {
                Console.WriteLine($"  - {s.ProcessStep.Name}");
            }
        }
        Console.WriteLine($"═══════════════════════════════════════════\n\n");

        // =========================================================================
        // SCENARIO 3: Rejection at Investment Agency (Late Stage)
        // =========================================================================
        
        Console.WriteLine("┌─────────────────────────────────────────────────────────┐");
        Console.WriteLine("│ SCENARIO 3: Late-Stage Rejection (Investment Agency)   │");
        Console.WriteLine("└─────────────────────────────────────────────────────────┘\n");

        var request3 = await engine.StartRequestAsync(assetProcess.Id);
        Console.WriteLine($"✓ Started Request: {request3.Id}");
        Console.WriteLine($"  Status: {request3.Status}\n");

        // Complete Steps 1-3 successfully
        request3 = await engine.GetRequestAsync(request3.Id);
        var currentStep = request3.Steps.FirstOrDefault(s => s.Status == RequestStepStatus.Active);
        
        // Step 1
        if (currentStep != null && currentStep.ProcessStep.Name == "School Planning")
        {
            Console.WriteLine($"► Step 1: {currentStep.ProcessStep.Name}");
            var action = currentStep.Actions.First();
            await engine.CompleteActionAsync(action.Id, "planning_officer", "{\"verified\": true}");
            Console.WriteLine($"  ✓ Completed\n");
        }

        // Step 2
        request3 = await engine.GetRequestAsync(request3.Id);
        currentStep = request3.Steps.FirstOrDefault(s => s.Status == RequestStepStatus.Active);
        if (currentStep != null && currentStep.ProcessStep.Name == "Safety, Security, and Facilities")
        {
            Console.WriteLine($"► Step 2: {currentStep.ProcessStep.Name}");
            var approveAction = currentStep.Actions.FirstOrDefault(a => !a.StepAction.IsRejectionAction);
            if (approveAction != null)
            {
                await engine.CompleteActionAsync(approveAction.Id, "safety_director", "{\"approved\": true}");
                Console.WriteLine($"  ✓ Approved\n");
            }
        }

        // Step 3
        request3 = await engine.GetRequestAsync(request3.Id);
        currentStep = request3.Steps.FirstOrDefault(s => s.Status == RequestStepStatus.Active);
        if (currentStep != null && currentStep.ProcessStep.Name == "Investment and Partnerships")
        {
            Console.WriteLine($"► Step 3: {currentStep.ProcessStep.Name}");
            var approveAction = currentStep.Actions.FirstOrDefault(a => !a.StepAction.IsRejectionAction);
            if (approveAction != null)
            {
                await engine.CompleteActionAsync(approveAction.Id, "investment_manager", "{\"approved\": true}");
                Console.WriteLine($"  ✓ Approved\n");
            }
        }

        // Step 4: Investment Agency - REJECT
        request3 = await engine.GetRequestAsync(request3.Id);
        currentStep = request3.Steps.FirstOrDefault(s => s.Status == RequestStepStatus.Active);
        if (currentStep != null && currentStep.ProcessStep.Name == "Investment Agency")
        {
            Console.WriteLine($"► Step 4: {currentStep.ProcessStep.Name}");
            Console.WriteLine($"  Available Actions:");
            foreach (var a in currentStep.Actions)
            {
                Console.WriteLine($"    - {a.StepAction.Name} {(a.StepAction.IsRejectionAction ? "(REJECTION)" : "(APPROVAL)")}");
            }
            
            var rejectAction = currentStep.Actions.FirstOrDefault(a => a.StepAction.IsRejectionAction);
            if (rejectAction != null)
            {
                Console.WriteLine($"\n  Completing: {rejectAction.StepAction.Name}");
                await engine.CompleteActionAsync(
                    rejectAction.Id, 
                    "agency_director", 
                    "{\"rejection_reason\": \"Insufficient funding allocated for this fiscal year\", \"resubmit_date\": \"2025-Q1\"}");
                
                Console.WriteLine($"  ✗ REJECTED by Investment Agency");
                Console.WriteLine($"  → Request status changed to Rejected");
                Console.WriteLine($"  → Remaining step (TBC) auto-skipped");
                Console.WriteLine($"  → Process ended (asset NOT added to bank)\n");
            }
        }

        request3 = await engine.GetRequestAsync(request3.Id);
        Console.WriteLine($"═══════════════════════════════════════════");
        Console.WriteLine($"Final Request Status: {request3.Status}");
        Console.WriteLine($"Completed Steps: {request3.Steps.Count(s => s.Status == RequestStepStatus.Completed)}");
        Console.WriteLine($"Skipped Steps: {request3.Steps.Count(s => s.Status == RequestStepStatus.Skipped)}");
        Console.WriteLine($"═══════════════════════════════════════════\n\n");

        // ==========================================================================
        // SUMMARY
        // ==========================================================================
        
        Console.WriteLine("┌═══════════════════════════════════════════════════════════════┐");
        Console.WriteLine("│                   DEMONSTRATION SUMMARY                       │");
        Console.WriteLine("└═══════════════════════════════════════════════════════════════┘");
        Console.WriteLine("\n✨ Add Asset to Asset Bank Process Features:\n");
        Console.WriteLine("  📋 PROCESS STRUCTURE:");
        Console.WriteLine("     • Sequential workflow (no parallel steps)");
        Console.WriteLine("     • 5 stages: Planning → 4 Approval Stages");
        Console.WriteLine("     • Each approval stage has Approve/Reject options\n");
        
        Console.WriteLine("  🎯 ACTION COMPLETION MODES:");
        Console.WriteLine("     • School Planning: ALL (must complete verification)");
        Console.WriteLine("     • Approval Stages: ANY (approve OR reject wins)\n");
        
        Console.WriteLine("  🚫 REJECTION HANDLING:");
        Console.WriteLine("     • Rejection at ANY stage ends process immediately");
        Console.WriteLine("     • Status: Rejected (distinct from Failed/Cancelled)");
        Console.WriteLine("     • All remaining steps automatically skipped");
        Console.WriteLine("     • Audit trail preserved with rejection reason\n");
        
        Console.WriteLine("  📊 SCENARIOS DEMONSTRATED:");
        Console.WriteLine("     1. ✓ Full approval path (all 5 steps completed)");
        Console.WriteLine("     2. ✗ Early rejection (at step 2 - safety concerns)");
        Console.WriteLine("     3. ✗ Late rejection (at step 4 - funding issues)\n");
        
        Console.WriteLine("  🔄 WORKFLOW BENEFITS:");
        Console.WriteLine("     • Clear approval chain with accountability");
        Console.WriteLine("     • Efficient rejection mechanism (no wasted approvals)");
        Console.WriteLine("     • Flexible: Easy to add/remove approval stages");
        Console.WriteLine("     • Audit-ready: Full history of decisions\n");
        
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("  🚀 Ready for production use with any sequential approval");
        Console.WriteLine("     workflow requiring multi-stage sign-off!");
        Console.WriteLine("═══════════════════════════════════════════════════════════════\n");

        #region Commented Out Loan Approval Scenarios
        // ... existing code ...
        #endregion
    }
}

