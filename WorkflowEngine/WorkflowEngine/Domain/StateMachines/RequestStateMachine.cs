using WorkflowEngine.Domain.Enums;

namespace WorkflowEngine.Domain.StateMachines;

public static class RequestStateMachine
{
    public static bool CanTransition(RequestStatus from, RequestStatus to)
    {
        return (from, to) switch
        {
            (RequestStatus.Pending, RequestStatus.InProgress) => true,
            (RequestStatus.InProgress, RequestStatus.Completed) => true,
            (RequestStatus.InProgress, RequestStatus.Failed) => true,
            (RequestStatus.InProgress, RequestStatus.Rejected) => true,  // Can reject during workflow
            (RequestStatus.Pending, RequestStatus.Cancelled) => true,
            _ => false
        };
    }
}
