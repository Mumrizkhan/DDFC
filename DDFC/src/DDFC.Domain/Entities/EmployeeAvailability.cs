using DDFC.Domain.Common;

namespace DDFC.Domain.Entities;

public class EmployeeAvailability : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid DepartmentId { get; set; }
    public bool IsAvailable { get; set; } = true;
    public DateTime? UnavailableFrom { get; set; }
    public DateTime? UnavailableUntil { get; set; }
    public string? Reason { get; set; }
    public Guid? SetByUserId { get; set; }
}
