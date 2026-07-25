using DDFC.Domain.Common;

namespace DDFC.Domain.Entities;

public class ArchitecturalPlan : BaseEntity
{
    public Guid RequestId { get; set; }
    public PossessionRequest Request { get; set; } = null!;
    public int Version { get; set; } = 1;
    public string FileUrl { get; set; } = string.Empty;
    public Guid UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public bool CustomerApproved { get; set; } = false;
    public DateTime? ApprovedAt { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public ICollection<PlanRevisionRequest> RevisionRequests { get; set; } = new List<PlanRevisionRequest>();
}
