using DDFC.Domain.Entities;
using DDFC.Domain.Enums;

namespace DDFC.Application.Interfaces;

public interface ITicketService
{
    Task<SupportTicket> CreateTicketAsync(Guid customerId, Guid? requestId, string subject,
        TicketCategory category, string description);

    Task<SupportTicket?> GetTicketAsync(Guid ticketId);
    Task<List<SupportTicket>> GetTicketsForCustomerAsync(Guid customerId);
    Task<List<SupportTicket>> GetTicketsForDepartmentAsync(Guid departmentId);
    Task<List<SupportTicket>> GetAllTicketsAsync();

    Task<TicketReply> ReplyAsync(Guid ticketId, Guid authorId, AuthorType authorType,
        string body, string? attachmentUrl = null);

    Task<SupportTicket> CloseTicketAsync(Guid ticketId, int? satisfactionRating = null);
    Task<SupportTicket> AssignTicketAsync(Guid ticketId, Guid departmentId);

    // Technical Support operations
    Task<SupportTicket> ResolveTicketAsync(Guid ticketId, Guid staffUserId);
    Task<SupportTicket> ReopenTicketAsync(Guid ticketId);
    Task<SupportTicket> AssignToUserAsync(Guid ticketId, Guid userId);
    Task<List<SupportTicket>> GetTicketsForUserAsync(Guid userId);
}
