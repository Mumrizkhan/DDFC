using DDFC.Domain.Common;

namespace DDFC.Domain.Entities;

public class Document : BaseEntity
{
    public Guid RequestId { get; set; }
    public PossessionRequest Request { get; set; } = null!;
    public string StepName { get; set; } = string.Empty;
    public string DocType { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public Guid UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public bool IsArchived { get; set; } = false;
}
