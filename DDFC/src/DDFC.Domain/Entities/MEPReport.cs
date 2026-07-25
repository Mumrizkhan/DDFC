using DDFC.Domain.Common;

namespace DDFC.Domain.Entities;

public class MEPReport : BaseEntity
{
    public Guid RequestId { get; set; }
    public PossessionRequest Request { get; set; } = null!;
    public string FileUrl { get; set; } = string.Empty;
    public string? Observations { get; set; }
    public Guid CompletedBy { get; set; }
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}
