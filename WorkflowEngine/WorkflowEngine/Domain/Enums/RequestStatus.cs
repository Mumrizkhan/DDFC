namespace WorkflowEngine.Domain.Enums;

public enum RequestStatus
{
    Pending,
    InProgress,
    Completed,
    Cancelled,
    Failed,
    Rejected  // Request was explicitly rejected (e.g., application declined, loan rejected)
}
