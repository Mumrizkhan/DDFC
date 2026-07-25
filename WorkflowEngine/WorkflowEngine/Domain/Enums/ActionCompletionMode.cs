namespace WorkflowEngine.Domain.Enums;

/// <summary>
/// Defines how many actions must complete before a step is considered complete
/// </summary>
public enum ActionCompletionMode
{
    /// <summary>
    /// All actions in the step must complete (traditional behavior)
    /// </summary>
    All = 0,
    
    /// <summary>
    /// Any single action completing (with data) completes the step
    /// Other actions are automatically skipped
    /// </summary>
    Any = 1,
    
    /// <summary>
    /// A minimum number of actions must complete before step completes
    /// Configured via MinimumActionCount property
    /// </summary>
    Minimum = 2
}
