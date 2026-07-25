using WorkflowEngine.Domain.Enums;

namespace WorkflowEngine.Domain.Entities;

/// <summary>
/// Represents a step instance within a workflow request execution
/// </summary>
public class RequestStep : BaseEntity
{
    public Guid RequestId { get; set; }
    public Request Request { get; set; } = null!;
    public Guid ProcessStepId { get; set; }
    public ProcessStep ProcessStep { get; set; } = null!;
    public RequestStepStatus Status { get; set; } = RequestStepStatus.Pending;
    public List<RequestAction> Actions { get; set; } = new();
    
    /// <summary>
    /// JSON data collected during this step for transition decisions
    /// Format: {"key": "value", "approval": "approved", "amount": 15000}
    /// </summary>
    public string? Data { get; set; }
    
    /// <summary>
    /// Groups parallel steps together. Steps with the same ParallelGroupId value
    /// were activated together.
    /// Null means sequential execution.
    /// </summary>
    public Guid? ParallelGroupId { get; set; }
    
    /// <summary>
    /// Defines how this parallel group must complete.
    /// Only relevant when ParallelGroupId is not null.
    /// </summary>
    public ParallelCompletionMode ParallelCompletionMode { get; set; } = ParallelCompletionMode.All;
    
    /// <summary>
    /// Minimum number of parallel steps that must complete when ParallelCompletionMode=Minimum.
    /// Only relevant when in a parallel group.
    /// </summary>
    public int? MinimumCompletionCount { get; set; }
    
    /// <summary>
    /// Defines how many actions in this step must complete before the step is complete.
    /// Copied from ProcessStep.ActionCompletionMode at runtime.
    /// </summary>
    public ActionCompletionMode ActionCompletionMode { get; set; } = ActionCompletionMode.All;
    
    /// <summary>
    /// Minimum number of actions that must complete when ActionCompletionMode=Minimum.
    /// Copied from ProcessStep.MinimumActionCount at runtime.
    /// </summary>
    public int? MinimumActionCount { get; set; }
    
    /// <summary>
    /// Tracks the ID of the step that was active immediately before this one.
    /// Used for backward navigation and audit trail.
    /// Null for the first step in a workflow.
    /// </summary>
    public Guid? PreviousStepId { get; set; }
    
    /// <summary>
    /// The number of times this step has been activated in the workflow.
    /// Increments each time the step is returned to (via backward transition).
    /// Helps track rework cycles and prevent infinite loops.
    /// </summary>
    public int ActivationCount { get; set; } = 0;
}
