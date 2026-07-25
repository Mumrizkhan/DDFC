using WorkflowEngine.Domain.Enums;

namespace WorkflowEngine.Domain.Entities;

/// <summary>
/// Defines conditional transitions between process steps
/// </summary>
public class StepTransition : BaseEntity
{
    public Guid FromStepId { get; set; }
    public ProcessStep FromStep { get; set; } = null!;
    public Guid ToStepId { get; set; }
    public ProcessStep ToStep { get; set; } = null!;
    
    /// <summary>
    /// Condition expression to evaluate. If null, this is the default transition.
    /// Format: "key=value" or "key!=value" or "key>value" etc.
    /// Example: "approval=approved" or "amount>10000"
    /// </summary>
    public string? Condition { get; set; }
    
    /// <summary>
    /// Priority for evaluation order (lower executes first)
    /// </summary>
    public int Priority { get; set; } = 0;
    
    /// <summary>
    /// Is this the default transition if no conditions match?
    /// </summary>
    public bool IsDefault { get; set; } = false;
    
    /// <summary>
    /// If true, this transition can execute in parallel with others at the same priority.
    /// Multiple transitions with IsParallel=true and matching conditions will all activate.
    /// </summary>
    public bool IsParallel { get; set; } = false;
    
    /// <summary>
    /// Defines how parallel steps must complete before the workflow advances.
    /// Only applies when IsParallel=true for multiple transitions.
    /// - All: All parallel steps must complete (default)
    /// - Any: Only one parallel step needs to complete
    /// - Minimum: A minimum number must complete (see MinimumCompletionCount)
    /// </summary>
    public ParallelCompletionMode ParallelCompletionMode { get; set; } = ParallelCompletionMode.All;
    
    /// <summary>
    /// When ParallelCompletionMode=Minimum, this specifies the minimum number
    /// of parallel steps that must complete before advancing.
    /// Must be greater than 0 and less than total parallel step count.
    /// </summary>
    public int? MinimumCompletionCount { get; set; }
    
    /// <summary>
    /// Indicates if this transition allows returning to a previous step.
    /// When true, this transition enables backward navigation in the workflow.
    /// Useful for:
    /// - Rework scenarios (e.g., "Return for corrections")
    /// - Re-approval processes
    /// - Step repetition based on conditions
    /// Note: Backward transitions may require special handling to reset step state.
    /// </summary>
    public bool IsBackwardTransition { get; set; } = false;
    
    /// <summary>
    /// Defines whether the target step should be reset when returning to it.
    /// Only applies when IsBackwardTransition=true.
    /// - true: Reset the step and its actions to Pending status (fresh start)
    /// - false: Keep the step's previous state and data (resume from where it was)
    /// </summary>
    public bool ResetTargetStepOnReturn { get; set; } = true;
}
