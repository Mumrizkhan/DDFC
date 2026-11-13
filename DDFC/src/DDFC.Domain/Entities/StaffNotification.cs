using DDFC.Domain.Common;

namespace DDFC.Domain.Entities;

public class StaffNotification : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid RequestId { get; set; }
    public PossessionRequest Request { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string MessageBody { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }
}