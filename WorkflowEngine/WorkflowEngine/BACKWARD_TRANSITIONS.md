# Backward Transitions (Return to Previous Step)

## Overview

The Workflow Engine now supports **backward transitions**, allowing workflows to return to previously completed steps. This enables rework scenarios, re-approval processes, and iterative workflows where steps may need to be repeated based on feedback or changing requirements.

## Key Features

### 1. Configurable Backward Transitions
Define transitions that point backward in your workflow using the `IsBackwardTransition` flag.

### 2. State Reset Control
Choose whether the target step should be reset to a fresh state or resume from where it left off using `ResetTargetStepOnReturn`.

### 3. Activation Tracking
The `ActivationCount` property tracks how many times a step has been activated, helping to:
- Audit rework cycles
- Prevent infinite loops
- Track workflow efficiency

### 4. Previous Step Tracking
The `PreviousStepId` property maintains a navigation trail through the workflow, useful for:
- Audit trails
- Understanding workflow paths
- Navigation history

## How It Works

### 1. Define Backward Transitions

When creating your process, add transitions with `IsBackwardTransition = true`:

```csharp
// Step definitions
var documentsStep = new ProcessStep { Name = "Submit Documents", Order = 1 };
var reviewStep = new ProcessStep { Name = "Review Documents", Order = 2 };
var approvalStep = new ProcessStep { Name = "Final Approval", Order = 3 };

// Forward transitions (normal flow)
var forwardTransition1 = new StepTransition
{
    FromStepId = documentsStep.Id,
    ToStepId = reviewStep.Id,
    IsDefault = true
};

var forwardTransition2 = new StepTransition
{
    FromStepId = reviewStep.Id,
    ToStepId = approvalStep.Id,
    Condition = "review=approved",
    Priority = 1
};

// Backward transition (return for corrections)
var backwardTransition = new StepTransition
{
    FromStepId = reviewStep.Id,
    ToStepId = documentsStep.Id,
    Condition = "review=rejected",
    Priority = 2,
    IsBackwardTransition = true,          // Marks this as a backward transition
    ResetTargetStepOnReturn = true        // Reset the target step when returning
};

db.StepTransitions.AddRange(forwardTransition1, forwardTransition2, backwardTransition);
```

### 2. Trigger Return to Previous Step

Use the `ReturnToPreviousStepAsync` method when you need to go back:

```csharp
// Current step is "Review Documents"
var currentStep = request.Steps.First(s => s.Status == RequestStepStatus.Active);

// Return to previous step with reason and data
var previousStep = await engine.ReturnToPreviousStepAsync(
    requestStepId: currentStep.Id,
    reason: "Documents incomplete - missing signature",
    returnData: "{\"review\": \"rejected\", \"missing\": [\"signature\", \"date\"]}"
);

// The workflow now returns to "Submit Documents" step
// User can resubmit corrected documents
```

### 3. Conditional Backward Transitions

You can use conditions to determine which previous step to return to:

```csharp
// Return to different steps based on the issue
var backwardTransition1 = new StepTransition
{
    FromStepId = reviewStep.Id,
    ToStepId = documentsStep.Id,
    Condition = "issue=documents",
    Priority = 1,
    IsBackwardTransition = true,
    ResetTargetStepOnReturn = true
};

var backwardTransition2 = new StepTransition
{
    FromStepId = reviewStep.Id,
    ToStepId = verificationStep.Id,
    Condition = "issue=verification",
    Priority = 1,
    IsBackwardTransition = true,
    ResetTargetStepOnReturn = true
};

// Use returnData to control which transition matches
await engine.ReturnToPreviousStepAsync(
    requestStepId: currentStep.Id,
    reason: "Identity verification failed",
    returnData: "{\"issue\": \"verification\"}"
);
```

## Reset Behavior

### Reset Target Step (ResetTargetStepOnReturn = true)

When returning with reset enabled:
- Target step status changes from Completed ? Pending ? Active
- All actions in the target step are reset to Pending
- Previous step data is cleared
- Action completion data is cleared
- User starts fresh as if the step was never completed

```csharp
var backwardTransition = new StepTransition
{
    FromStepId = reviewStep.Id,
    ToStepId = documentsStep.Id,
    IsBackwardTransition = true,
    ResetTargetStepOnReturn = true  // Fresh start
};

// After return:
// - Submit Documents step is reset
// - All upload/verification actions are reset
// - User must resubmit everything
```

**Use When:**
- Previous work is invalid and needs to be redone
- Requirements have changed
- Data needs to be completely replaced
- Clean slate is required

### Keep Target Step State (ResetTargetStepOnReturn = false)

When returning without reset:
- Target step status changes from Completed ? Pending ? Active
- Previous actions remain Completed
- Previous step data is preserved
- User can see and reference previous work
- Can supplement or modify existing work

```csharp
var backwardTransition = new StepTransition
{
    FromStepId = reviewStep.Id,
    ToStepId = documentsStep.Id,
    IsBackwardTransition = true,
    ResetTargetStepOnReturn = false  // Keep previous state
};

// After return:
// - Submit Documents step is reopened
// - Previously uploaded documents are still there
// - User can add additional documents or make corrections
```

**Use When:**
- Need to add supplementary information
- Previous work is mostly correct, just needs additions
- Want to preserve history and show what changed
- Iterative refinement workflow

## Complete Examples

### Example 1: Document Review with Corrections

**Scenario:** Documents are submitted for review. If incomplete, return for corrections.

```csharp
// Define process
var submitDocs = new ProcessStep 
{ 
    Name = "Submit Documents", 
    Order = 1,
    ActionCompletionMode = ActionCompletionMode.All
};
submitDocs.Actions.Add(new StepAction { Name = "Upload Documents", ActionType = ActionType.Upload });
submitDocs.Actions.Add(new StepAction { Name = "Verify Upload", ActionType = ActionType.Verify });

var review = new ProcessStep 
{ 
    Name = "Review Documents", 
    Order = 2,
    ActionCompletionMode = ActionCompletionMode.Any
};
review.Actions.Add(new StepAction { Name = "Approve", ActionType = ActionType.Approval });
review.Actions.Add(new StepAction { Name = "Request Corrections", ActionType = ActionType.Rejection });

var approval = new ProcessStep 
{ 
    Name = "Final Approval", 
    Order = 3 
};

process.Steps.AddRange(new[] { submitDocs, review, approval });

// Forward transitions
db.StepTransitions.Add(new StepTransition
{
    FromStepId = submitDocs.Id,
    ToStepId = review.Id,
    IsDefault = true
});

db.StepTransitions.Add(new StepTransition
{
    FromStepId = review.Id,
    ToStepId = approval.Id,
    Condition = "status=approved",
    Priority = 1
});

// Backward transition for corrections
db.StepTransitions.Add(new StepTransition
{
    FromStepId = review.Id,
    ToStepId = submitDocs.Id,
    Condition = "status=rejected",
    Priority = 2,
    IsBackwardTransition = true,
    ResetTargetStepOnReturn = true  // User must resubmit everything
});

// Workflow execution
var request = await engine.StartRequestAsync(processId);

// User submits documents
request = await engine.GetRequestAsync(request.Id);
var submitStep = request.Steps.First(s => s.Status == RequestStepStatus.Active);
foreach (var action in submitStep.Actions)
{
    await engine.CompleteActionAsync(action.Id, "user1", "{\"documents\": \"uploaded\"}");
}

// Reviewer finds issues
request = await engine.GetRequestAsync(request.Id);
var reviewStep = request.Steps.First(s => s.Status == RequestStepStatus.Active);
var requestCorrectionsAction = reviewStep.Actions.First(a => a.StepAction.Name == "Request Corrections");

// Instead of completing the action, use ReturnToPreviousStepAsync
await engine.ReturnToPreviousStepAsync(
    reviewStep.Id,
    "Missing signature page and date on form 2",
    "{\"status\": \"rejected\", \"issues\": [\"signature\", \"date\"]}"
);

// Workflow returns to Submit Documents step
// User can now resubmit corrected documents
request = await engine.GetRequestAsync(request.Id);
submitStep = request.Steps.First(s => s.Status == RequestStepStatus.Active);
// ActivationCount = 2 (second time at this step)
```

### Example 2: Multi-Level Approval with Escalation Return

**Scenario:** Three-level approval. If Level 3 rejects, return to Level 1 for major rework.

```csharp
var level1Approval = new ProcessStep { Name = "Level 1 Approval", Order = 1 };
var level2Approval = new ProcessStep { Name = "Level 2 Approval", Order = 2 };
var level3Approval = new ProcessStep { Name = "Level 3 Approval", Order = 3 };
var finalize = new ProcessStep { Name = "Finalize", Order = 4 };

// Forward flow
db.StepTransitions.AddRange(new[]
{
    new StepTransition { FromStepId = level1Approval.Id, ToStepId = level2Approval.Id, IsDefault = true },
    new StepTransition { FromStepId = level2Approval.Id, ToStepId = level3Approval.Id, IsDefault = true },
    new StepTransition 
    { 
        FromStepId = level3Approval.Id, 
        ToStepId = finalize.Id,
        Condition = "approval=approved",
        Priority = 1
    }
});

// Backward transitions
db.StepTransitions.AddRange(new[]
{
    // Level 3 can send back to Level 2 for minor changes
    new StepTransition
    {
        FromStepId = level3Approval.Id,
        ToStepId = level2Approval.Id,
        Condition = "issue=minor",
        Priority = 2,
        IsBackwardTransition = true,
        ResetTargetStepOnReturn = false  // Keep previous approvals
    },
    // Level 3 can send back to Level 1 for major rework
    new StepTransition
    {
        FromStepId = level3Approval.Id,
        ToStepId = level1Approval.Id,
        Condition = "issue=major",
        Priority = 2,
        IsBackwardTransition = true,
        ResetTargetStepOnReturn = true  // Complete rework needed
    },
    // Level 2 can send back to Level 1
    new StepTransition
    {
        FromStepId = level2Approval.Id,
        ToStepId = level1Approval.Id,
        Condition = "approval=rejected",
        Priority = 2,
        IsBackwardTransition = true,
        ResetTargetStepOnReturn = true
    }
});

// Usage
var level3Step = request.Steps.First(s => s.ProcessStep.Name == "Level 3 Approval" && 
                                           s.Status == RequestStepStatus.Active);

// Major issue found - return to Level 1
await engine.ReturnToPreviousStepAsync(
    level3Step.Id,
    "Fundamental approach needs rework",
    "{\"issue\": \"major\", \"details\": \"Requirements not met\"}"
);
// Workflow returns to Level 1 Approval with reset
```

### Example 3: Iterative Design Review

**Scenario:** Design is reviewed multiple times, with each iteration building on previous feedback.

```csharp
var designDraft = new ProcessStep 
{ 
    Name = "Create Design Draft", 
    Order = 1 
};

var designReview = new ProcessStep 
{ 
    Name = "Design Review", 
    Order = 2,
    ActionCompletionMode = ActionCompletionMode.Any
};
designReview.Actions.Add(new StepAction { Name = "Approve Design", ActionType = ActionType.Approval });
designReview.Actions.Add(new StepAction { Name = "Request Revisions", ActionType = ActionType.Review });

var implementation = new ProcessStep 
{ 
    Name = "Implementation", 
    Order = 3 
};

// Forward transitions
db.StepTransitions.AddRange(new[]
{
    new StepTransition { FromStepId = designDraft.Id, ToStepId = designReview.Id, IsDefault = true },
    new StepTransition 
    { 
        FromStepId = designReview.Id, 
        ToStepId = implementation.Id,
        Condition = "status=approved",
        Priority = 1
    }
});

// Iterative backward transition
db.StepTransitions.Add(new StepTransition
{
    FromStepId = designReview.Id,
    ToStepId = designDraft.Id,
    Condition = "status=revisions_requested",
    Priority = 2,
    IsBackwardTransition = true,
    ResetTargetStepOnReturn = false,  // Keep previous design, iterate on it
    MaxActivationCount = 5  // Prevent infinite loops (custom validation)
});

// Workflow - first iteration
request = await engine.StartRequestAsync(processId);
// ... user creates design draft v1 ...
// ... reviewer requests changes ...

var reviewStep = request.Steps.First(s => s.Status == RequestStepStatus.Active);
await engine.ReturnToPreviousStepAsync(
    reviewStep.Id,
    "Minor adjustments needed to color scheme",
    "{\"status\": \"revisions_requested\", \"iteration\": 2}"
);

// User can see v1 and make adjustments for v2
// ActivationCount = 2

// After v2 review, if more changes needed...
request = await engine.GetRequestAsync(request.Id);
reviewStep = request.Steps.First(s => s.Status == RequestStepStatus.Active);
await engine.ReturnToPreviousStepAsync(
    reviewStep.Id,
    "Typography improvements needed",
    "{\"status\": \"revisions_requested\", \"iteration\": 3}"
);

// ActivationCount = 3
// Process continues until approved or max iterations reached
```

## Preventing Infinite Loops

To prevent workflows from looping indefinitely, implement guards based on `ActivationCount`:

### Application-Level Validation

```csharp
public async Task<RequestStep> SafeReturnToPreviousStepAsync(
    Guid requestStepId, 
    string? reason = null, 
    string? returnData = null,
    int maxActivations = 5)
{
    var currentStep = await _repo.GetRequestStepAsync(requestStepId);
    
    // Check if target step has been activated too many times
    var targetSteps = await GetPotentialBackwardTargetStepsAsync(currentStep);
    foreach (var targetStep in targetSteps)
    {
        if (targetStep.ActivationCount >= maxActivations)
        {
            throw new InvalidOperationException(
                $"Cannot return to step '{targetStep.ProcessStep.Name}' - " +
                $"maximum activation count ({maxActivations}) reached. " +
                $"This prevents infinite loops.");
        }
    }
    
    return await engine.ReturnToPreviousStepAsync(requestStepId, reason, returnData);
}
```

### Database Constraint

```sql
-- Add a check constraint to limit activations
ALTER TABLE workflow.RequestSteps
ADD CONSTRAINT CK_RequestSteps_MaxActivations 
CHECK (ActivationCount <= 10);
```

### Process-Level Configuration

```csharp
var backwardTransition = new StepTransition
{
    FromStepId = reviewStep.Id,
    ToStepId = documentsStep.Id,
    IsBackwardTransition = true,
    ResetTargetStepOnReturn = true,
    // Custom property (requires extension)
    Metadata = "{\"max_activations\": 3}"
};
```

## API Reference

### ReturnToPreviousStepAsync

```csharp
Task<RequestStep> ReturnToPreviousStepAsync(
    Guid requestStepId,      // Current active step ID
    string? reason = null,   // Human-readable reason for return
    string? returnData = null // JSON data for transition evaluation
);
```

**Parameters:**
- `requestStepId`: The ID of the currently active step that is being returned from
- `reason`: Optional reason for the return (logged and stored for audit)
- `returnData`: Optional JSON data used to evaluate which backward transition to follow

**Returns:**
- The activated target step (the step we returned to)

**Throws:**
- `InvalidOperationException` if:
  - Step is not found
  - Step is not currently Active
  - No backward transitions are defined
  - No backward transition matches the provided conditions
  - Target step is not found in the request

### StepTransition Properties

```csharp
public class StepTransition
{
    // ...existing properties...
    
    public bool IsBackwardTransition { get; set; } = false;
    public bool ResetTargetStepOnReturn { get; set; } = true;
}
```

### RequestStep Properties

```csharp
public class RequestStep
{
    // ...existing properties...
    
    public Guid? PreviousStepId { get; set; }
    public int ActivationCount { get; set; } = 0;
}
```

## Best Practices

### 1. Always Provide Reason and Data

```csharp
// ? Good: Clear reason and structured data
await engine.ReturnToPreviousStepAsync(
    stepId,
    "Missing required signature on page 3",
    "{\"status\": \"rejected\", \"missing_items\": [\"signature_page3\"]}"
);

// ? Bad: No context
await engine.ReturnToPreviousStepAsync(stepId);
```

### 2. Use Conditions for Multiple Backward Paths

```csharp
// ? Good: Clear routing based on issue type
var backwardTransition1 = new StepTransition
{
    Condition = "issue_type=documents",
    ToStepId = documentsStep.Id,
    IsBackwardTransition = true
};

var backwardTransition2 = new StepTransition
{
    Condition = "issue_type=verification",
    ToStepId = verificationStep.Id,
    IsBackwardTransition = true
};
```

### 3. Choose Appropriate Reset Behavior

```csharp
// ? Reset when work is invalid
ResetTargetStepOnReturn = true  // For: incorrect data, failed validation

// ? Keep state when work is partial
ResetTargetStepOnReturn = false  // For: additions, minor corrections
```

### 4. Monitor Activation Counts

```csharp
// ? Check activation count before returning
var targetStep = await GetTargetStepAsync(currentStep);
if (targetStep.ActivationCount >= 3)
{
    // Escalate or take alternative action
    await engine.SkipStepAsync(currentStep.Id, "Too many rework cycles");
}
```

### 5. Log Backward Transitions

```csharp
// ? The engine logs automatically, but add business logging too
_logger.LogWarning(
    "Request {RequestId} returned to {StepName} for rework. " +
    "Reason: {Reason}. Activation #{ActivationCount}",
    request.Id,
    targetStep.ProcessStep.Name,
    reason,
    targetStep.ActivationCount);
```

## Limitations

### Current Limitations

1. **Single Return Only**: Can only return to one target step at a time (no parallel backward transitions)
2. **No Forward Jump After Return**: After returning, must follow normal flow from the target step
3. **No Automatic Loop Prevention**: Application must implement activation count checks
4. **State Management**: Reopening steps doesn't automatically notify users

### Future Enhancements

- **Multi-step return**: Return multiple steps back in one operation
- **Configurable loop limits**: Built-in max activation count at transition level
- **Automatic notifications**: Alert users when a step is reopened
- **State diff tracking**: Show what changed between activations
- **Conditional reset**: Reset only specific actions, not entire step
- **Return paths visualization**: UI to show possible return paths

## Troubleshooting

### Issue: "No backward transitions defined"

**Cause**: The current step doesn't have any transitions with `IsBackwardTransition = true`

**Solution**: Add backward transition to process definition:
```csharp
db.StepTransitions.Add(new StepTransition
{
    FromStepId = currentStep.Id,
    ToStepId = previousStep.Id,
    IsBackwardTransition = true
});
```

### Issue: "No backward transition matched the provided conditions"

**Cause**: Return data doesn't match any backward transition conditions

**Solution**: Check condition syntax and data:
```csharp
// Transition condition
Condition = "status=rejected"

// Make sure returnData matches
returnData = "{\"status\": \"rejected\"}"  // ? Matches
returnData = "{\"status\": \"approved\"}"  // ? Doesn't match
```

### Issue: Steps Being Activated Too Many Times

**Cause**: Workflow is in a loop without proper guards

**Solution**: Check activation count:
```csharp
if (step.ActivationCount > 5)
{
    // Escalate or fail the request
    await engine.FailRequestAsync(request.Id, "Maximum rework cycles exceeded");
}
```

## Summary

Backward transitions provide powerful workflow flexibility:

**Key Benefits:**
- ? Enable rework and correction workflows
- ? Support iterative processes
- ? Maintain audit trail with activation tracking
- ? Configurable state reset behavior
- ? Conditional routing to multiple previous steps
- ? Prevent data loss with state preservation option

**When to Use:**
- Document review and correction flows
- Multi-level approval with escalation
- Iterative design/development processes
- Quality control with rework cycles
- Any workflow requiring feedback loops

**Key Properties:**
- `IsBackwardTransition`: Marks transition as backward
- `ResetTargetStepOnReturn`: Controls state reset
- `PreviousStepId`: Tracks navigation history
- `ActivationCount`: Prevents infinite loops

Transform your workflows with flexible backward navigation! ??
