using DDFC.Domain.Common;
using DDFC.Domain.Enums;

namespace DDFC.Domain.Entities;

public class CustomerNotification : BaseEntity
{
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public Guid? RequestId { get; set; }
    public PossessionRequest? Request { get; set; }
    public string Title { get; set; } = string.Empty;
    public string MessageBody { get; set; } = string.Empty;
    public bool RequiresResponse { get; set; } = false;
    public string? ResponseText { get; set; }
    public DateTime? RespondedAt { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public NotificationChannel Channel { get; set; } = NotificationChannel.InApp;
}
