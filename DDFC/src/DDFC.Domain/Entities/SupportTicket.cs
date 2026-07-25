using DDFC.Domain.Common;
using DDFC.Domain.Enums;

namespace DDFC.Domain.Entities;

public class SupportTicket : BaseEntity
{
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public Guid? RequestId { get; set; }
    public PossessionRequest? Request { get; set; }
    public string Subject { get; set; } = string.Empty;
    public TicketCategory Category { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }
    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public Guid? AssignedDepartmentId { get; set; }
    public Department? AssignedDepartment { get; set; }
    public Guid? AssignedUserId { get; set; }
    public User? AssignedUser { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public int? SatisfactionRating { get; set; }   // 1-5

    // Navigation
    public ICollection<TicketReply> Replies { get; set; } = new List<TicketReply>();
}
