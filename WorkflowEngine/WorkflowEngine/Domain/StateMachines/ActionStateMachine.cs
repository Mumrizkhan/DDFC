using WorkflowEngine.Domain.Enums;

namespace WorkflowEngine.Domain.StateMachines;

public static class ActionStateMachine
{
    public static bool CanTransition(RequestActionStatus from, RequestActionStatus to)
    {
        return (from, to) switch
        {
            (RequestActionStatus.Pending, RequestActionStatus.InProgress) => true,
            (RequestActionStatus.InProgress, RequestActionStatus.Completed) => true,
            (RequestActionStatus.Pending, RequestActionStatus.Failed) => true,
            (RequestActionStatus.InProgress, RequestActionStatus.Failed) => true,
            _ => false
        };
    }
}
