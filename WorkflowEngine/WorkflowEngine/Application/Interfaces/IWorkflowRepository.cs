using WorkflowEngine.Domain.Entities;

namespace WorkflowEngine.Application.Interfaces;

public interface IWorkflowRepository
{
    Task<Process> GetProcessAsync(Guid id);
    Task<Request> GetRequestWithDetailsAsync(Guid requestId);
    Task<RequestAction> GetRequestActionAsync(Guid requestActionId);
    Task<RequestStep> GetRequestStepAsync(Guid requestStepId);
    Task<Request> CreateRequestFromProcessAsync(Guid processId);
    Task SaveChangesAsync();
}
