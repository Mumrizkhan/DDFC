using Microsoft.AspNetCore.Identity;

namespace DDFC.Domain.Entities;

/// <summary>
/// DDFC staff role.  Extends IdentityRole&lt;Guid&gt; so ASP.NET Core Identity
/// manages role-claim relationships and normalised name lookups automatically.
/// </summary>
public class Role : IdentityRole<Guid>
{
    public Role() { Id = Guid.NewGuid(); }
    public Role(string roleName) : base(roleName) { Id = Guid.NewGuid(); }

    // ── Business fields ───────────────────────────────────────────────────────
    /// <summary>JSON array of permission strings, e.g. ["CanApproveTransfer","CanUploadPlan"]</summary>
    public string? Permissions { get; set; }
    public bool IsBuiltIn { get; set; } = false;

    // ── Audit / soft-delete ───────────────────────────────────────────────────
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
}
