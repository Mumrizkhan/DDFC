using WorkflowEngine.Domain.Enums;

namespace WorkflowEngine.Domain.Entities;

/// <summary>
/// Represents an action instance within a request step execution
/// </summary>
public class RequestAction : BaseEntity
{
    public Guid RequestStepId { get; set; }
    public RequestStep RequestStep { get; set; } = null!;
    public Guid StepActionId { get; set; }
    public StepAction StepAction { get; set; } = null!;
    public RequestActionStatus Status { get; set; } = RequestActionStatus.Pending;
    public string? PerformedBy { get; set; }
    public DateTime? PerformedAt { get; set; }
    public string? Data { get; set; }
}
