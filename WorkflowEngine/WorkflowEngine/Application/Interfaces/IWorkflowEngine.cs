using WorkflowEngine.Domain.Entities;

namespace WorkflowEngine.Application.Interfaces;

public interface IWorkflowEngine
{
    Task<Request> StartRequestAsync(Guid processId);
    Task ActivateNextStepAsync(Request request);
    Task CompleteActionAsync(Guid requestActionId, string performedBy, string? data = null);
    Task CompleteStepAsync(Guid requestStepId, string? stepData = null);
    Task FailActionAsync(Guid requestActionId, string? reason = null);
    Task SkipStepAsync(Guid requestStepId, string? reason = null);
    Task CancelRequestAsync(Guid requestId, string? reason = null);
    Task RejectRequestAsync(Guid requestId, string? reason = null);
    Task<Request> GetRequestAsync(Guid requestId);
    
    /// <summary>
    /// Returns the workflow to a previous step based on backward transitions.
    /// The current active step must have a backward transition defined to the target step.
    /// </summary>
    /// <param name="requestStepId">The ID of the current active step to return from</param>
    /// <param name="reason">Optional reason for returning to previous step (for audit trail)</param>
    /// <param name="returnData">Optional data to pass when returning (e.g., rejection reason)</param>
    /// <returns>The activated previous step</returns>
    Task<RequestStep> ReturnToPreviousStepAsync(Guid requestStepId, string? reason = null, string? returnData = null);
}
