namespace WorkflowEngine.Domain.Entities;

/// <summary>
/// Represents a workflow process definition
/// </summary>
public class Process : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public List<ProcessStep> Steps { get; set; } = new();
}
