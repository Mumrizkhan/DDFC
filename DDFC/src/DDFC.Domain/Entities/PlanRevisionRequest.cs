using DDFC.Domain.Common;

namespace DDFC.Domain.Entities;

public class PlanRevisionRequest : BaseEntity
{
    public Guid PlanId { get; set; }
    public ArchitecturalPlan Plan { get; set; } = null!;
    public Guid RequestedBy { get; set; }      // CustomerId or UserId
    public string Comments { get; set; } = string.Empty;
    public string? MarkupFileUrl { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
}
