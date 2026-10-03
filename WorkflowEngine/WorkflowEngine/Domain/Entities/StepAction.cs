using WorkflowEngine.Domain.Enums;

namespace WorkflowEngine.Domain.Entities;

/// <summary>
/// Represents an action within a process step
/// </summary>
public class StepAction : BaseEntity
{
    public Guid ProcessStepId { get; set; }
    public ProcessStep ProcessStep { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// The type/category of this action
    /// </summary>
    public ActionType ActionType { get; set; } = ActionType.General;
    
    /// <summary>
    /// If true, completing this action will mark the request as Rejected and end the process.
    /// Use for rejection/decline actions (e.g., "Reject Application", "Decline Loan").
    /// </summary>
    public bool IsRejectionAction { get; set; } = false;

    /// <summary>
    /// If true, completing this action resets the step's actions to Pending without advancing the workflow.
    /// Use for "incomplete / request more info" actions where the step must be re-reviewed.
    /// </summary>
    public bool IsLoopbackAction { get; set; } = false;
}
