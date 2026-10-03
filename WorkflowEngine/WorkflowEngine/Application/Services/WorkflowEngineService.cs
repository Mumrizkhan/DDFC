using Microsoft.Extensions.Logging;
using WorkflowEngine.Application.Interfaces;
using WorkflowEngine.Domain.Entities;
using WorkflowEngine.Domain.Enums;
using WorkflowEngine.Domain.Services;
using WorkflowEngine.Domain.StateMachines;

namespace WorkflowEngine.Application.Services;

public class WorkflowEngineService : IWorkflowEngine
{
    private readonly IWorkflowRepository _repo;
    private readonly ILogger<WorkflowEngineService> _logger;

    public WorkflowEngineService(IWorkflowRepository repo, ILogger<WorkflowEngineService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<Request> StartRequestAsync(Guid processId)
    {
        var request = await _repo.CreateRequestFromProcessAsync(processId);

        if (!RequestStateMachine.CanTransition(request.Status, RequestStatus.InProgress))
            throw new InvalidOperationException("Invalid request start transition");

        request.Status = RequestStatus.InProgress;
        await _repo.SaveChangesAsync();

        _logger.LogInformation("Request {RequestId} started", request.Id);

        await ActivateNextStepAsync(request);
        return request;
    }

    public async Task<Request> GetRequestAsync(Guid requestId)
    {
        var r = await _repo.GetRequestWithDetailsAsync(requestId);
        if (r == null) throw new InvalidOperationException("Request not found");
        return r;
    }

    public async Task ActivateNextStepAsync(Request request)
    {
        // reload with details to make sure we have fresh state
        request = await _repo.GetRequestWithDetailsAsync(request.Id);

        // Check if all steps in any parallel group are completed
        var activeParallelGroups = request.Steps
            .Where(s => s.ParallelGroupId.HasValue && s.Status == RequestStepStatus.Active)
            .Select(s => s.ParallelGroupId.Value)
            .Distinct()
            .ToList();

        if (activeParallelGroups.Any())
        {
            // There are active parallel steps, don't activate new steps yet
            _logger.LogInformation(
                "Request {RequestId} has {Count} active parallel step groups, waiting for completion",
                request.Id,
                activeParallelGroups.Count);
            return;
        }

        // Get the most recently completed step to check for transitions
        var completedStep = request.Steps
            .OrderByDescending(s => s.ProcessStep.Order)
            .FirstOrDefault(s => s.Status == RequestStepStatus.Completed);

        List<RequestStep> nextSteps = new();
        ParallelCompletionMode? completionMode = null;
        int? minimumCount = null;

        // If we have a completed step with transitions, evaluate them
        if (completedStep?.ProcessStep?.TransitionsFrom?.Count > 0)
        {
            var nextStepIds = TransitionEvaluator.EvaluateNextSteps(
                completedStep.ProcessStep.TransitionsFrom, 
                completedStep.Data);

            if (nextStepIds.Any())
            {
                // Find the next steps by ProcessStepId
                foreach (var nextStepId in nextStepIds)
                {
                    var step = request.Steps.FirstOrDefault(s => 
                        s.ProcessStepId == nextStepId && 
                        s.Status == RequestStepStatus.Pending);
                    
                    if (step != null)
                        nextSteps.Add(step);
                }

                if (nextSteps.Any())
                {
                    // Get completion mode from transitions (all should have same mode)
                    var transition = completedStep.ProcessStep.TransitionsFrom
                        .FirstOrDefault(t => t.ToStepId == nextSteps[0].ProcessStepId);
                    
                    if (transition != null)
                    {
                        completionMode = transition.ParallelCompletionMode;
                        minimumCount = transition.MinimumCompletionCount;
                    }

                    if (nextSteps.Count > 1)
                    {
                        _logger.LogInformation(
                            "Parallel transition from step {FromStepId} to {Count} steps: [{ToStepIds}] with {Mode} completion",
                            completedStep.ProcessStepId,
                            nextSteps.Count,
                            string.Join(", ", nextSteps.Select(s => s.ProcessStepId)),
                            completionMode ?? ParallelCompletionMode.All);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Conditional transition from step {FromStepId} to step {ToStepId}",
                            completedStep.ProcessStepId,
                            nextSteps[0].ProcessStepId);
                    }
                }
            }
        }

        // If no conditional transition, use sequential flow
        if (!nextSteps.Any())
        {
            var next = request.Steps
                .OrderBy(s => s.ProcessStep.Order)
                .FirstOrDefault(s => s.Status == RequestStepStatus.Pending);
            
            if (next != null)
                nextSteps.Add(next);
        }

        if (!nextSteps.Any())
        {
            // No more steps → complete request
            if (!RequestStateMachine.CanTransition(request.Status, RequestStatus.Completed))
                throw new InvalidOperationException("Invalid request complete transition");

            request.Status = RequestStatus.Completed;
            await _repo.SaveChangesAsync();
            _logger.LogInformation("Request {RequestId} completed", request.Id);
            return;
        }

        // Activate all next steps (could be parallel or single)
        Guid? parallelGroupId = nextSteps.Count > 1 ? Guid.NewGuid() : null;

        // Track the current active step to set as previous
        var currentActiveStep = request.Steps.FirstOrDefault(s => s.Status == RequestStepStatus.Active);
        var previousStepId = currentActiveStep?.Id;

        foreach (var step in nextSteps)
        {
            if (!StepStateMachine.CanTransition(step.Status, RequestStepStatus.Active))
                throw new InvalidOperationException($"Invalid step activation transition for step {step.Id}");

            step.Status = RequestStepStatus.Active;
            step.ParallelGroupId = parallelGroupId;
            step.ParallelCompletionMode = completionMode ?? ParallelCompletionMode.All;
            step.MinimumCompletionCount = minimumCount;
            step.ActivationCount++; // Increment activation counter
            step.PreviousStepId = previousStepId; // Track previous step
            
            // Copy action completion mode from ProcessStep
            step.ActionCompletionMode = step.ProcessStep.ActionCompletionMode;
            step.MinimumActionCount = step.ProcessStep.MinimumActionCount;
            
            // Initialize actions to Pending if not already
            foreach (var a in step.Actions)
                a.Status = RequestActionStatus.Pending;

            _logger.LogInformation(
                "Activated step {StepId} ({StepName}) of Request {RequestId}{Parallel}{ActionMode}",
                step.Id,
                step.ProcessStep.Name,
                request.Id,
                parallelGroupId.HasValue ? $" [Parallel Group: {parallelGroupId}, Mode: {step.ParallelCompletionMode}]" : "",
                $" [Action Mode: {step.ActionCompletionMode}]");
        }

        await _repo.SaveChangesAsync();
    }

    public async Task CompleteActionAsync(Guid requestActionId, string performedBy, string? data = null)
    {
        var action = await _repo.GetRequestActionAsync(requestActionId);
        if (action == null) throw new InvalidOperationException("Action not found");

        // Transition to InProgress first if Pending
        if (action.Status == RequestActionStatus.Pending)
        {
            if (!ActionStateMachine.CanTransition(action.Status, RequestActionStatus.InProgress))
                throw new InvalidOperationException("Invalid action transition to InProgress");
            
            action.Status = RequestActionStatus.InProgress;
            await _repo.SaveChangesAsync();
        }

        // Then transition to Completed
        if (!ActionStateMachine.CanTransition(action.Status, RequestActionStatus.Completed))
            throw new InvalidOperationException("Invalid action transition to Completed");

        action.Status = RequestActionStatus.Completed;
        action.PerformedBy = performedBy;
        action.PerformedAt = DateTime.UtcNow;
        action.Data = data;

        await _repo.SaveChangesAsync();

        _logger.LogInformation("Action {ActionId} ({ActionName}) completed by {User}", 
            action.Id, 
            action.StepAction?.Name ?? "Unknown",
            performedBy);

        // Check if this is a loopback action (step stays active, all actions reset for re-review)
        if (action.StepAction?.IsLoopbackAction == true)
        {
            var loopbackStep = action.RequestStep;
            _logger.LogInformation(
                "Loopback action {ActionId} ({ActionName}) completed for request {RequestId} - resetting step for re-review",
                action.Id, action.StepAction.Name, loopbackStep.Request.Id);

            foreach (var a in loopbackStep.Actions)
                a.Status = RequestActionStatus.Pending;

            await _repo.SaveChangesAsync();
            return; // Step remains Active
        }

        // Check if this is a rejection action
        if (action.StepAction?.IsRejectionAction == true)
        {
            var step = action.RequestStep;
            var request = step.Request;
            
            _logger.LogWarning(
                "Rejection action {ActionId} ({ActionName}) completed - rejecting request {RequestId}",
                action.Id,
                action.StepAction.Name,
                request.Id);

            // Mark step as completed with rejection data
            if (step.Status == RequestStepStatus.Active)
            {
                step.Status = RequestStepStatus.Completed;
                step.Data = data ?? "{\"rejected\": true}";
                await _repo.SaveChangesAsync();
            }

            // Reject the entire request
            await RejectRequestAsync(request.Id, $"Rejected by action: {action.StepAction.Name}");
            return;
        }

        // Check if step can complete based on ActionCompletionMode
        await TryCompleteStepAsync(action.RequestStepId, data);
    }

    private async Task CompleteStepWithActionDataAsync(Guid requestStepId, string actionData)
    {
        var step = await _repo.GetRequestStepAsync(requestStepId);
        if (step == null) throw new InvalidOperationException("RequestStep not found");

        if (step.Status != RequestStepStatus.Active)
            return; // Step already completed or skipped

        if (!StepStateMachine.CanTransition(step.Status, RequestStepStatus.Completed))
            throw new InvalidOperationException("Invalid step complete transition");

        // Skip all other pending/in-progress actions in this step
        foreach (var otherAction in step.Actions.Where(a => 
            a.Status != RequestActionStatus.Completed && 
            a.Status != RequestActionStatus.Failed))
        {
            // Mark as skipped (we'll use Failed status with special marker)
            otherAction.Status = RequestActionStatus.Failed;
            otherAction.Data = "[AUTO-SKIPPED] Step completed by another action";
            otherAction.PerformedAt = DateTime.UtcNow;
            
            _logger.LogInformation(
                "Action {ActionId} ({ActionName}) auto-skipped because step was completed with data",
                otherAction.Id,
                otherAction.StepAction?.Name ?? "Unknown");
        }

        step.Status = RequestStepStatus.Completed;
        step.Data = actionData; // Use action data for step data (for transition evaluation)
        await _repo.SaveChangesAsync();

        _logger.LogInformation(
            "Step {StepId} completed with action data for Request {RequestId}, other actions skipped",
            step.Id,
            step.RequestId);

        // Check if this was part of a parallel group
        if (step.ParallelGroupId.HasValue)
        {
            await TryCompleteParallelGroupAsync(step.Request, step.ParallelGroupId.Value);
        }
        else
        {
            // activate next step (will use transition logic)
            await ActivateNextStepAsync(step.Request);
        }
    }

    public async Task CompleteStepAsync(Guid requestStepId, string? stepData = null)
    {
        var step = await _repo.GetRequestStepAsync(requestStepId);
        if (step == null) throw new InvalidOperationException("RequestStep not found");

        if (step.Status != RequestStepStatus.Active)
            throw new InvalidOperationException("Step must be Active to complete");

        // Skip any remaining incomplete actions based on ActionCompletionMode
        if (step.ActionCompletionMode != ActionCompletionMode.All)
        {
            foreach (var action in step.Actions.Where(a => 
                a.Status != RequestActionStatus.Completed && 
                a.Status != RequestActionStatus.Failed))
            {
                action.Status = RequestActionStatus.Failed;
                action.Data = "[AUTO-SKIPPED] Step completed explicitly";
                action.PerformedAt = DateTime.UtcNow;
                
                _logger.LogInformation(
                    "Action {ActionId} ({ActionName}) auto-skipped when step was completed explicitly",
                    action.Id,
                    action.StepAction?.Name ?? "Unknown");
            }
        }

        if (!StepStateMachine.CanTransition(step.Status, RequestStepStatus.Completed))
            throw new InvalidOperationException("Invalid step complete transition");

        step.Status = RequestStepStatus.Completed;
        step.Data = stepData; // Store data for transition evaluation
        await _repo.SaveChangesAsync();

        _logger.LogInformation("Step {StepId} completed explicitly for Request {RequestId} with data", 
            step.Id, step.RequestId);

        // Check if this was part of a parallel group
        if (step.ParallelGroupId.HasValue)
        {
            await TryCompleteParallelGroupAsync(step.Request, step.ParallelGroupId.Value);
        }
        else
        {
            // activate next step (will use transition logic)
            await ActivateNextStepAsync(step.Request);
        }
    }

    private async Task TryCompleteStepAsync(Guid requestStepId, string? lastActionData = null)
    {
        var step = await _repo.GetRequestStepAsync(requestStepId);
        if (step == null) throw new InvalidOperationException("RequestStep not found");

        if (step.Status != RequestStepStatus.Active)
            return; // Step already completed or skipped

        var completedActions = step.Actions.Count(a => a.Status == RequestActionStatus.Completed);
        var totalActions = step.Actions.Count;
        bool shouldComplete = false;
        string? stepData = null;

        switch (step.ActionCompletionMode)
        {
            case ActionCompletionMode.All:
                // All actions must complete
                shouldComplete = completedActions == totalActions;
                // Collect data from last completed action if available
                stepData = step.Actions
                    .Where(a => a.Status == RequestActionStatus.Completed && !string.IsNullOrEmpty(a.Data))
                    .OrderByDescending(a => a.PerformedAt)
                    .FirstOrDefault()?.Data;
                break;

            case ActionCompletionMode.Any:
                // In approval/review steps, any single completed action should complete the step.
                // This is required for flows like "Assign Architect" where the optional soil-test
                // upload is not mandatory and should not block the handoff to the Architect step.
                shouldComplete = completedActions > 0;
                if (!string.IsNullOrEmpty(lastActionData))
                {
                    stepData = lastActionData;
                }
                else
                {
                    stepData = step.Actions
                        .Where(a => a.Status == RequestActionStatus.Completed && !string.IsNullOrEmpty(a.Data))
                        .OrderByDescending(a => a.PerformedAt)
                        .FirstOrDefault()?.Data;
                }
                break;

            case ActionCompletionMode.Minimum:
                // Minimum number of actions must complete
                if (step.MinimumActionCount.HasValue)
                {
                    shouldComplete = completedActions >= step.MinimumActionCount.Value;
                    // Use data from last completed action
                    stepData = step.Actions
                        .Where(a => a.Status == RequestActionStatus.Completed && !string.IsNullOrEmpty(a.Data))
                        .OrderByDescending(a => a.PerformedAt)
                        .FirstOrDefault()?.Data;
                }
                else
                {
                    // Default to All if MinimumActionCount not specified
                    shouldComplete = completedActions == totalActions;
                    _logger.LogWarning(
                        "Step {StepId} has Minimum action mode but no MinimumActionCount set, defaulting to All",
                        step.Id);
                }
                break;
        }

        if (!shouldComplete)
        {
            var remaining = totalActions - completedActions;
            var required = step.ActionCompletionMode == ActionCompletionMode.Minimum
                ? $"{step.MinimumActionCount.Value - completedActions} more"
                : $"{remaining}";

            _logger.LogInformation(
                "Step {StepId} requires {Required} more action completions ({Mode} mode, {Completed}/{Total} completed)",
                step.Id,
                required,
                step.ActionCompletionMode,
                completedActions,
                totalActions);
            return;
        }

        // Step can complete - skip remaining actions if applicable
        if (step.ActionCompletionMode != ActionCompletionMode.All)
        {
            foreach (var otherAction in step.Actions.Where(a => 
                a.Status != RequestActionStatus.Completed && 
                a.Status != RequestActionStatus.Failed))
            {
                otherAction.Status = RequestActionStatus.Failed;
                otherAction.Data = $"[AUTO-SKIPPED] Step completed with {step.ActionCompletionMode} mode";
                otherAction.PerformedAt = DateTime.UtcNow;
                
                _logger.LogInformation(
                    "Action {ActionId} ({ActionName}) auto-skipped due to {Mode} action completion mode",
                    otherAction.Id,
                    otherAction.StepAction?.Name ?? "Unknown",
                    step.ActionCompletionMode);
            }
        }

        if (!StepStateMachine.CanTransition(step.Status, RequestStepStatus.Completed))
            throw new InvalidOperationException("Invalid step complete transition");

        step.Status = RequestStepStatus.Completed;
        step.Data = stepData;
        await _repo.SaveChangesAsync();

        _logger.LogInformation(
            "Step {StepId} completed with {Mode} action mode ({Completed}/{Total} actions) for Request {RequestId}",
            step.Id,
            step.ActionCompletionMode,
            completedActions,
            totalActions,
            step.RequestId);

        // Check if this was part of a parallel group
        if (step.ParallelGroupId.HasValue)
        {
            await TryCompleteParallelGroupAsync(step.Request, step.ParallelGroupId.Value);
        }
        else
        {
            // activate next step
            await ActivateNextStepAsync(step.Request);
        }
    }

    private async Task TryCompleteParallelGroupAsync(Request request, Guid parallelGroupId)
    {
        // Reload request to get fresh state
        request = await _repo.GetRequestWithDetailsAsync(request.Id);

        var parallelSteps = request.Steps
            .Where(s => s.ParallelGroupId == parallelGroupId)
            .ToList();

        if (!parallelSteps.Any())
            return;

        // Get completion mode from the first step (all in group should have same mode)
        var completionMode = parallelSteps[0].ParallelCompletionMode;
        var minimumCount = parallelSteps[0].MinimumCompletionCount;

        var completedCount = parallelSteps.Count(s => s.Status == RequestStepStatus.Completed);
        var totalCount = parallelSteps.Count;
        bool shouldAdvance = false;

        switch (completionMode)
        {
            case ParallelCompletionMode.All:
                // All steps must complete
                shouldAdvance = parallelSteps.All(s => s.Status == RequestStepStatus.Completed);
                break;

            case ParallelCompletionMode.Any:
                // Any one step completing is enough
                shouldAdvance = completedCount >= 1;
                break;

            case ParallelCompletionMode.Minimum:
                // Minimum number must complete
                if (minimumCount.HasValue)
                {
                    shouldAdvance = completedCount >= minimumCount.Value;
                }
                else
                {
                    // Default to All if MinimumCount not specified
                    shouldAdvance = parallelSteps.All(s => s.Status == RequestStepStatus.Completed);
                    _logger.LogWarning(
                        "Parallel group {GroupId} has Minimum mode but no MinimumCompletionCount set, defaulting to All",
                        parallelGroupId);
                }
                break;
        }

        if (shouldAdvance)
        {
            // Skip any remaining incomplete steps in the group
            foreach (var step in parallelSteps.Where(s => s.Status == RequestStepStatus.Active))
            {
                step.Status = RequestStepStatus.Skipped;
                _logger.LogInformation(
                    "Step {StepId} ({StepName}) auto-skipped due to {Mode} completion mode",
                    step.Id,
                    step.ProcessStep.Name,
                    completionMode);
            }

            await _repo.SaveChangesAsync();

            _logger.LogInformation(
                "Parallel group {GroupId} satisfied {Mode} completion ({Completed}/{Total} completed) for Request {RequestId}",
                parallelGroupId,
                completionMode,
                completedCount,
                totalCount,
                request.Id);

            // All parallel steps completed or minimum satisfied, activate next step
            await ActivateNextStepAsync(request);
        }
        else
        {
            var remaining = parallelSteps.Count(s => s.Status == RequestStepStatus.Active);
            var required = completionMode == ParallelCompletionMode.Minimum
                ? $"{minimumCount.Value - completedCount} more"
                : $"{remaining}";

            _logger.LogInformation(
                "Parallel group {GroupId} requires {Required} more completions ({Mode} mode, {Completed}/{Total} completed) for Request {RequestId}",
                parallelGroupId,
                required,
                completionMode,
                completedCount,
                totalCount,
                request.Id);
        }
    }

    public async Task FailActionAsync(Guid requestActionId, string? reason = null)
    {
        var action = await _repo.GetRequestActionAsync(requestActionId);
        if (action == null) throw new InvalidOperationException("Action not found");

        if (!ActionStateMachine.CanTransition(action.Status, RequestActionStatus.Failed))
            throw new InvalidOperationException("Invalid action transition to failed");

        action.Status = RequestActionStatus.Failed;
        action.PerformedAt = DateTime.UtcNow;
        action.Data = reason;
        await _repo.SaveChangesAsync();

        _logger.LogWarning("Action {ActionId} failed: {Reason}", action.Id, reason);

        // mark step as failed
        var step = action.RequestStep;
        if (StepStateMachine.CanTransition(step.Status, RequestStepStatus.Failed))
        {
            step.Status = RequestStepStatus.Failed;
            await _repo.SaveChangesAsync();
        }

        // mark request as failed
        var req = step.Request;
        if (RequestStateMachine.CanTransition(req.Status, RequestStatus.Failed))
        {
            req.Status = RequestStatus.Failed;
            await _repo.SaveChangesAsync();
        }
    }

    public async Task SkipStepAsync(Guid requestStepId, string? reason = null)
    {
        var step = await _repo.GetRequestStepAsync(requestStepId);
        if (step == null) throw new InvalidOperationException("RequestStep not found");

        if (!StepStateMachine.CanTransition(step.Status, RequestStepStatus.Skipped))
            throw new InvalidOperationException("Invalid step skip transition");

        step.Status = RequestStepStatus.Skipped;
        await _repo.SaveChangesAsync();

        _logger.LogInformation("Step {StepId} skipped: {Reason}", step.Id, reason);

        // activate next step
        await ActivateNextStepAsync(step.Request);
    }

    public async Task CancelRequestAsync(Guid requestId, string? reason = null)
    {
        var request = await _repo.GetRequestWithDetailsAsync(requestId);
        if (request == null) throw new InvalidOperationException("Request not found");

        if (!RequestStateMachine.CanTransition(request.Status, RequestStatus.Cancelled))
            throw new InvalidOperationException("Invalid request cancel transition");

        request.Status = RequestStatus.Cancelled;
        await _repo.SaveChangesAsync();

        _logger.LogInformation("Request {RequestId} cancelled: {Reason}", requestId, reason);
    }

    public async Task RejectRequestAsync(Guid requestId, string? reason = null)
    {
        var request = await _repo.GetRequestWithDetailsAsync(requestId);
        if (request == null) throw new InvalidOperationException("Request not found");

        if (!RequestStateMachine.CanTransition(request.Status, RequestStatus.Rejected))
            throw new InvalidOperationException("Invalid request reject transition");

        // Skip all remaining active/pending steps
        foreach (var step in request.Steps.Where(s => 
            s.Status == RequestStepStatus.Active || 
            s.Status == RequestStepStatus.Pending))
        {
            step.Status = RequestStepStatus.Skipped;
            
            // Skip all actions in those steps
            foreach (var action in step.Actions.Where(a => 
                a.Status != RequestActionStatus.Completed && 
                a.Status != RequestActionStatus.Failed))
            {
                action.Status = RequestActionStatus.Failed;
                action.Data = "[AUTO-SKIPPED] Request rejected";
                action.PerformedAt = DateTime.UtcNow;
            }
        }

        request.Status = RequestStatus.Rejected;
        await _repo.SaveChangesAsync();

        _logger.LogWarning("Request {RequestId} rejected: {Reason}", requestId, reason);
    }

    public async Task<RequestStep> ReturnToPreviousStepAsync(Guid requestStepId, string? reason = null, string? returnData = null)
    {
        var currentStep = await _repo.GetRequestStepAsync(requestStepId);
        if (currentStep == null) 
            throw new InvalidOperationException("RequestStep not found");

        if (currentStep.Status != RequestStepStatus.Active)
            throw new InvalidOperationException("Only active steps can initiate a return to previous step");

        // Find backward transition from current step
        var backwardTransitions = currentStep.ProcessStep.TransitionsFrom?
            .Where(t => t.IsBackwardTransition)
            .OrderBy(t => t.Priority)
            .ToList();

        if (backwardTransitions == null || !backwardTransitions.Any())
            throw new InvalidOperationException(
                $"No backward transitions defined for step '{currentStep.ProcessStep.Name}'");

        // Evaluate backward transitions (may have conditions)
        var evaluatedStepIds = TransitionEvaluator.EvaluateNextSteps(backwardTransitions, returnData);
        
        if (!evaluatedStepIds.Any())
            throw new InvalidOperationException(
                "No backward transition matched the provided conditions");

        var targetProcessStepId = evaluatedStepIds.First();
        var backwardTransition = backwardTransitions.First(t => t.ToStepId == targetProcessStepId);

        // Find the target step in the request
        var targetStep = currentStep.Request.Steps.FirstOrDefault(s => s.ProcessStepId == targetProcessStepId);
        if (targetStep == null)
            throw new InvalidOperationException(
                $"Target step for backward transition not found in request");

        // Check if we should reset the target step or keep its previous state
        if (backwardTransition.ResetTargetStepOnReturn)
        {
            // Reset the target step to pending with all actions reset
            if (targetStep.Status != RequestStepStatus.Pending)
            {
                _logger.LogInformation(
                    "Resetting target step {StepId} ({StepName}) from {OldStatus} to Pending for return",
                    targetStep.Id,
                    targetStep.ProcessStep.Name,
                    targetStep.Status);
                
                targetStep.Status = RequestStepStatus.Pending;
                targetStep.Data = null; // Clear previous data
                
                // Reset all actions in the target step
                foreach (var action in targetStep.Actions)
                {
                    action.Status = RequestActionStatus.Pending;
                    action.Data = null;
                    action.PerformedBy = null;
                    action.PerformedAt = null;
                }
            }
        }
        else
        {
            // Keep previous state - just ensure it's in a workable state
            if (targetStep.Status == RequestStepStatus.Completed)
            {
                // Reopen the completed step
                _logger.LogInformation(
                    "Reopening completed target step {StepId} ({StepName}) for return (keeping previous data)",
                    targetStep.Id,
                    targetStep.ProcessStep.Name);
                
                targetStep.Status = RequestStepStatus.Pending;
                // Keep existing data for reference
            }
        }

        // Mark current step as completed with return data
        currentStep.Status = RequestStepStatus.Completed;
        currentStep.Data = returnData ?? $"{{\"returned_to\": \"{targetStep.ProcessStep.Name}\", \"reason\": \"{reason}\"}}";
        
        // Mark all incomplete actions in current step as skipped
        foreach (var action in currentStep.Actions.Where(a => 
            a.Status != RequestActionStatus.Completed && 
            a.Status != RequestActionStatus.Failed))
        {
            action.Status = RequestActionStatus.Failed;
            action.Data = $"[AUTO-SKIPPED] Step returned to previous: {reason}";
            action.PerformedAt = DateTime.UtcNow;
        }

        await _repo.SaveChangesAsync();

        // Now activate the target step
        if (targetStep.Status == RequestStepStatus.Pending)
        {
            targetStep.Status = RequestStepStatus.Active;
            targetStep.ActivationCount++; // Increment activation counter
            targetStep.PreviousStepId = currentStep.Id; // Track where we came from
            
            // Copy action completion mode from ProcessStep (in case it was reset)
            targetStep.ActionCompletionMode = targetStep.ProcessStep.ActionCompletionMode;
            targetStep.MinimumActionCount = targetStep.ProcessStep.MinimumActionCount;
            
            await _repo.SaveChangesAsync();
            
            _logger.LogInformation(
                "Returned from step {FromStepId} ({FromStepName}) to step {ToStepId} ({ToStepName}) for Request {RequestId}. " +
                "Reason: {Reason}. Activation count: {ActivationCount}",
                currentStep.Id,
                currentStep.ProcessStep.Name,
                targetStep.Id,
                targetStep.ProcessStep.Name,
                currentStep.RequestId,
                reason ?? "Not specified",
                targetStep.ActivationCount);
        }

        return targetStep;
    }
}
