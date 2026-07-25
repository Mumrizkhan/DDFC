using DDFC.Application.Interfaces;
using DDFC.Domain.Entities;
using DDFC.Domain.Enums;
using DDFC.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DDFC.Infrastructure.Services;

public class TaskAssignmentService : ITaskAssignmentService
{
    private readonly DDFCDbContext _db;
    private readonly IRoundRobinService _roundRobin;

    public TaskAssignmentService(DDFCDbContext db, IRoundRobinService roundRobin)
    {
        _db = db;
        _roundRobin = roundRobin;
    }

    public async Task<TaskAssignment> AutoAssignAsync(Guid requestId, Guid departmentId,
        string stepName, Guid? assignedBy = null)
    {
        var user = await _roundRobin.GetNextAvailableUserAsync(departmentId);

        var task = new TaskAssignment
        {
            RequestId        = requestId,
            DepartmentId     = departmentId,
            AssignedToUserId = user?.Id,
            AssignedByUserId = assignedBy,
            AssignmentMethod = user is null ? TaskAssignmentMethod.RoundRobin : TaskAssignmentMethod.RoundRobin,
            StepName         = stepName,
            Status           = TaskAssignmentStatus.Pending
        };

        _db.TaskAssignments.Add(task);
        await _db.SaveChangesAsync();
        return task;
    }

    public async Task<TaskAssignment> ReassignAsync(Guid taskId, Guid newUserId, Guid reassignedBy, string reason)
    {
        var task = await _db.TaskAssignments.FindAsync(taskId)
            ?? throw new KeyNotFoundException($"Task {taskId} not found.");

        task.ReassignedFromUserId = task.AssignedToUserId;
        task.AssignedToUserId     = newUserId;
        task.ReassignedReason     = reason;
        task.AssignedByUserId     = reassignedBy;
        task.AssignmentMethod     = TaskAssignmentMethod.Manual;

        _db.TaskAssignments.Update(task);
        await _db.SaveChangesAsync();
        return task;
    }

    public async Task CompleteTaskAsync(Guid taskId)
    {
        var task = await _db.TaskAssignments.FindAsync(taskId)
            ?? throw new KeyNotFoundException($"Task {taskId} not found.");
        task.Status = TaskAssignmentStatus.Completed;
        _db.TaskAssignments.Update(task);
        await _db.SaveChangesAsync();
    }

    public Task<List<TaskAssignment>> GetPendingTasksForUserAsync(Guid userId) =>
        _db.TaskAssignments
           .Include(t => t.Request)
           .Where(t => t.AssignedToUserId == userId && t.Status == TaskAssignmentStatus.Pending)
           .OrderBy(t => t.CreatedAt)
           .ToListAsync();

    public Task<List<TaskAssignment>> GetTasksForRequestAsync(Guid requestId) =>
        _db.TaskAssignments
           .Include(t => t.AssignedToUser)
           .Include(t => t.Department)
           .Where(t => t.RequestId == requestId)
           .OrderBy(t => t.CreatedAt)
           .ToListAsync();
}
