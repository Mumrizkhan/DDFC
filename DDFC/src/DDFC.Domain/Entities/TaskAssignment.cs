using DDFC.Domain.Common;
using DDFC.Domain.Enums;

namespace DDFC.Domain.Entities;

public class TaskAssignment : BaseEntity
{
    public Guid RequestId { get; set; }
    public PossessionRequest Request { get; set; } = null!;
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
    public Guid? AssignedToUserId { get; set; }
    public User? AssignedToUser { get; set; }
    public Guid? AssignedByUserId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public TaskAssignmentMethod AssignmentMethod { get; set; } = TaskAssignmentMethod.RoundRobin;
    public Guid? ReassignedFromUserId { get; set; }
    public string? ReassignedReason { get; set; }
    public TaskAssignmentStatus Status { get; set; } = TaskAssignmentStatus.Pending;
    public string? StepName { get; set; }   // Which workflow step this task corresponds to
}
