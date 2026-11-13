using DDFC.Domain.Common;

namespace DDFC.Domain.Entities;

public class DraftsmanTask : BaseEntity
{
    public Guid RequestId { get; set; }
    public PossessionRequest Request { get; set; } = null!;
    public string ParentStep { get; set; } = string.Empty;
    public Guid AssignedToUserId { get; set; }
    public User AssignedToUser { get; set; } = null!;
    public Guid AssignedByUserId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public string? FileUrl { get; set; }
    public DateTime? CompletedAt { get; set; }
}