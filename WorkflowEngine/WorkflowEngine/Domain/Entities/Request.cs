using WorkflowEngine.Domain.Enums;

namespace WorkflowEngine.Domain.Entities;

/// <summary>
/// Represents an instance of a workflow process execution
/// </summary>
public class Request : BaseEntity
{
    public Guid ProcessId { get; set; }
    public Process Process { get; set; } = null!;
    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    public List<RequestStep> Steps { get; set; } = new();
}
