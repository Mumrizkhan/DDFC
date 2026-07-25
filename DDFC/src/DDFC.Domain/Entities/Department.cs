using DDFC.Domain.Common;

namespace DDFC.Domain.Entities;

public class Department : BaseEntity
{
    public string DepartmentName { get; set; } = string.Empty;
    public string DepartmentCode { get; set; } = string.Empty;
    public Guid? HeadUserId { get; set; }
    public User? HeadUser { get; set; }
    public bool IsActive { get; set; } = true;

    // Round-robin state
    public int LastAssignedEmployeeIndex { get; set; } = 0;

    // Navigation
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<TaskAssignment> TaskAssignments { get; set; } = new List<TaskAssignment>();
    public ICollection<SupportTicket> AssignedTickets { get; set; } = new List<SupportTicket>();
}
