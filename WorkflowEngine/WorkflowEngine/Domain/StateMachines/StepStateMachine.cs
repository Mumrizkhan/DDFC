using WorkflowEngine.Domain.Enums;

namespace WorkflowEngine.Domain.StateMachines;

public static class StepStateMachine
{
    public static bool CanTransition(RequestStepStatus from, RequestStepStatus to)
    {
        return (from, to) switch
        {
            (RequestStepStatus.Pending, RequestStepStatus.Active) => true,
            (RequestStepStatus.Pending, RequestStepStatus.Skipped) => true,
            (RequestStepStatus.Active, RequestStepStatus.Completed) => true,
            (RequestStepStatus.Active, RequestStepStatus.Failed) => true,
            _ => false
        };
    }
}
