using WorkflowEngine.Domain.Enums;

namespace WorkflowEngine.Domain.Entities;

/// <summary>
/// Represents a step within a process definition
/// </summary>
public class ProcessStep : BaseEntity
{
    public Guid ProcessId { get; set; }
    public Process Process { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public List<StepAction> Actions { get; set; } = new();
    public List<StepTransition> TransitionsFrom { get; set; } = new();
    public List<StepTransition> TransitionsTo { get; set; } = new();
    
    /// <summary>
    /// Defines how many actions must complete before this step is complete.
    /// - All: All actions must complete (default)
    /// - Any: Any single action with data completes the step
    /// - Minimum: MinimumActionCount actions must complete
    /// </summary>
    public ActionCompletionMode ActionCompletionMode { get; set; } = ActionCompletionMode.All;
    
    /// <summary>
    /// When ActionCompletionMode=Minimum, this specifies the minimum number
    /// of actions that must complete before the step is complete.
    /// Must be greater than 0 and less than or equal to total action count.
    /// </summary>
    public int? MinimumActionCount { get; set; }
}
