using DDFC.Domain.Common;
using DDFC.Domain.Enums;

namespace DDFC.Domain.Entities;

public class TicketReply : BaseEntity
{
    public Guid TicketId { get; set; }
    public SupportTicket Ticket { get; set; } = null!;
    public Guid AuthorId { get; set; }          // UserId or CustomerId
    public AuthorType AuthorType { get; set; }
    public string MessageBody { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }
}
