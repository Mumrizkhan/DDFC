using Microsoft.EntityFrameworkCore;
using WorkflowEngine.Application.Interfaces;
using WorkflowEngine.Domain.Entities;
using WorkflowEngine.Domain.Enums;
using WorkflowEngine.Infrastructure.Persistence;

namespace WorkflowEngine.Infrastructure.Repositories;

public class WorkflowRepository : IWorkflowRepository
{
    private readonly WorkflowDbContext _db;

    public WorkflowRepository(WorkflowDbContext db) => _db = db;

    public async Task<Process> GetProcessAsync(Guid id)
    {
        return await _db.Processes
            .Include(p => p.Steps.OrderBy(s => s.Order))
                .ThenInclude(s => s.Actions)
            .Include(p => p.Steps)
                .ThenInclude(s => s.TransitionsFrom)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Request> CreateRequestFromProcessAsync(Guid processId)
    {
        var process = await GetProcessAsync(processId) 
            ?? throw new InvalidOperationException("Process not found");

        var request = new Request
        {
            ProcessId = process.Id,
            Status = RequestStatus.Pending,
            CreatedAt = DateTime.UtcNow,
        };

        // Create RequestSteps from ProcessSteps
        foreach (var ps in process.Steps.OrderBy(s => s.Order))
        {
            var rs = new RequestStep
            {
                ProcessStepId = ps.Id,
                Status = RequestStepStatus.Pending,
            };

            foreach (var a in ps.Actions)
            {
                rs.Actions.Add(new RequestAction
                {
                    StepActionId = a.Id,
                    Status = RequestActionStatus.Pending
                });
            }

            request.Steps.Add(rs);
        }

        _db.Requests.Add(request);
        await _db.SaveChangesAsync();
        return request;
    }

    public async Task<Request> GetRequestWithDetailsAsync(Guid requestId)
    {
        return await _db.Requests
            .Include(r => r.Process)
            .Include(r => r.Steps).ThenInclude(rs => rs.ProcessStep).ThenInclude(ps => ps.TransitionsFrom)
            .Include(r => r.Steps).ThenInclude(rs => rs.Actions).ThenInclude(a => a.StepAction)
            .FirstOrDefaultAsync(r => r.Id == requestId);
    }

    public async Task<RequestAction> GetRequestActionAsync(Guid requestActionId)
    {
        return await _db.RequestActions
            .Include(a => a.RequestStep)
                .ThenInclude(rs => rs.Request)
            .Include(a => a.StepAction)
            .FirstOrDefaultAsync(a => a.Id == requestActionId);
    }

    public async Task<RequestStep> GetRequestStepAsync(Guid requestStepId)
    {
        return await _db.RequestSteps
            .Include(s => s.Actions).ThenInclude(a => a.StepAction)
            .Include(s => s.Request)
            .Include(s => s.ProcessStep)
                .ThenInclude(ps => ps.TransitionsFrom)
            .FirstOrDefaultAsync(s => s.Id == requestStepId);
    }

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
