using Microsoft.AspNetCore.Identity;

namespace DDFC.Domain.Entities;

/// <summary>
/// DDFC staff user.  Extends IdentityUser&lt;Guid&gt; so ASP.NET Core Identity
/// manages password hashing, security stamps and lock-out automatically.
/// </summary>
public class User : IdentityUser<Guid>
{
    public User() { Id = Guid.NewGuid(); }

    // ── Business fields ───────────────────────────────────────────────────────
    public string FullName { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsAvailable { get; set; } = true;   // round-robin availability
    public DateTime? LastLogin { get; set; }

    // ── Audit / soft-delete (replaces BaseEntity) ─────────────────────────────
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;

    // ── Navigation ────────────────────────────────────────────────────────────
    public ICollection<TaskAssignment> AssignedTasks { get; set; } = new List<TaskAssignment>();
    public ICollection<EmployeeAvailability> Availabilities { get; set; } = new List<EmployeeAvailability>();
}
