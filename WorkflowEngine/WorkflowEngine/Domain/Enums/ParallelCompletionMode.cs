namespace WorkflowEngine.Domain.Enums;

/// <summary>
/// Defines how parallel steps must complete before the workflow advances
/// </summary>
public enum ParallelCompletionMode
{
    /// <summary>
    /// All parallel steps must complete before advancing (default behavior)
    /// </summary>
    All = 0,
    
    /// <summary>
    /// Only one (any) parallel step needs to complete before advancing
    /// Other steps are automatically skipped when first completes
    /// </summary>
    Any = 1,
    
    /// <summary>
    /// A minimum number of parallel steps must complete
    /// Configured via MinimumCompletionCount property
    /// </summary>
    Minimum = 2
}
