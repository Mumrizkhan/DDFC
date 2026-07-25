using DDFC.Domain.Entities;
using DDFC.Domain.Enums;

namespace DDFC.Application.Interfaces;

public interface ITaskAssignmentService
{
    /// <summary>Assigns a workflow step to the next available department employee via round-robin.</summary>
    Task<TaskAssignment> AutoAssignAsync(Guid requestId, Guid departmentId, string stepName, Guid? assignedBy = null);

    /// <summary>Manually reassigns an existing task to a specific user.</summary>
    Task<TaskAssignment> ReassignAsync(Guid taskId, Guid newUserId, Guid reassignedBy, string reason);

    /// <summary>Marks a task as completed.</summary>
    Task CompleteTaskAsync(Guid taskId);

    /// <summary>Returns all pending tasks for a given user.</summary>
    Task<List<TaskAssignment>> GetPendingTasksForUserAsync(Guid userId);

    /// <summary>Returns all tasks for a given request.</summary>
    Task<List<TaskAssignment>> GetTasksForRequestAsync(Guid requestId);
}
