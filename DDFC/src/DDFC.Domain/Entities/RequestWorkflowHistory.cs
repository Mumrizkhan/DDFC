using DDFC.Domain.Common;
using DDFC.Domain.Enums;

namespace DDFC.Domain.Entities;

public class RequestWorkflowHistory : BaseEntity
{
    public Guid RequestId { get; set; }
    public PossessionRequest Request { get; set; } = null!;

    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public string? Action { get; set; }
    public Guid? ActionByUserId { get; set; }
    public User? ActionByUser { get; set; }
    public string? Comments { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
