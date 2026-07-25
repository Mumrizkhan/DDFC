using DDFC.Application.Interfaces;
using DDFC.Domain.Entities;
using DDFC.Domain.Enums;
using DDFC.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DDFC.Infrastructure.Services;

public class TicketService : ITicketService
{
    private readonly DDFCDbContext _db;
    public TicketService(DDFCDbContext db) => _db = db;

    public async Task<SupportTicket> CreateTicketAsync(Guid customerId, Guid? requestId,
        string subject, TicketCategory category, string description)
    {
        // Auto-assign new tickets to the Technical Support department
        var tsDept = await _db.Departments.FirstOrDefaultAsync(d => d.DepartmentCode == "TS");

        var ticket = new SupportTicket
        {
            CustomerId           = customerId,
            RequestId            = requestId,
            Subject              = subject,
            Category             = category,
            Description          = description,
            Status               = TicketStatus.Open,
            AssignedDepartmentId = tsDept?.Id
        };
        _db.SupportTickets.Add(ticket);
        await _db.SaveChangesAsync();
        return ticket;
    }

    public Task<SupportTicket?> GetTicketAsync(Guid ticketId) =>
        _db.SupportTickets
           .Include(t => t.Replies)
           .Include(t => t.Customer)
           .Include(t => t.AssignedDepartment)
           .FirstOrDefaultAsync(t => t.Id == ticketId);

    public Task<List<SupportTicket>> GetTicketsForCustomerAsync(Guid customerId) =>
        _db.SupportTickets
           .Include(t => t.Replies)
           .Where(t => t.CustomerId == customerId)
           .OrderByDescending(t => t.CreatedAt)
           .ToListAsync();

    public Task<List<SupportTicket>> GetTicketsForDepartmentAsync(Guid departmentId) =>
        _db.SupportTickets
           .Include(t => t.Customer)
           .Where(t => t.AssignedDepartmentId == departmentId)
           .OrderByDescending(t => t.CreatedAt)
           .ToListAsync();

    public Task<List<SupportTicket>> GetAllTicketsAsync() =>
        _db.SupportTickets
           .Include(t => t.Customer)
           .Include(t => t.AssignedDepartment)
           .OrderByDescending(t => t.CreatedAt)
           .ToListAsync();

    public async Task<TicketReply> ReplyAsync(Guid ticketId, Guid authorId, AuthorType authorType,
        string body, string? attachmentUrl = null)
    {
        var ticket = await _db.SupportTickets.FindAsync(ticketId)
            ?? throw new KeyNotFoundException($"Ticket {ticketId} not found.");

        if (ticket.Status == TicketStatus.Closed)
            throw new InvalidOperationException("Cannot reply to a closed ticket.");

        if (ticket.Status == TicketStatus.Open && authorType == AuthorType.Staff)
            ticket.Status = TicketStatus.InProgress;

        var reply = new TicketReply
        {
            TicketId      = ticketId,
            AuthorId      = authorId,
            AuthorType    = authorType,
            MessageBody   = body,
            AttachmentUrl = attachmentUrl
        };

        _db.TicketReplies.Add(reply);
        _db.SupportTickets.Update(ticket);
        await _db.SaveChangesAsync();
        return reply;
    }

    public async Task<SupportTicket> CloseTicketAsync(Guid ticketId, int? satisfactionRating = null)
    {
        var ticket = await _db.SupportTickets.FindAsync(ticketId)
            ?? throw new KeyNotFoundException($"Ticket {ticketId} not found.");

        ticket.Status             = TicketStatus.Closed;
        ticket.SatisfactionRating = satisfactionRating;
        _db.SupportTickets.Update(ticket);
        await _db.SaveChangesAsync();
        return ticket;
    }

    public async Task<SupportTicket> AssignTicketAsync(Guid ticketId, Guid departmentId)
    {
        var ticket = await _db.SupportTickets.FindAsync(ticketId)
            ?? throw new KeyNotFoundException($"Ticket {ticketId} not found.");

        ticket.AssignedDepartmentId = departmentId;
        _db.SupportTickets.Update(ticket);
        await _db.SaveChangesAsync();
        return ticket;
    }

    public async Task<SupportTicket> ResolveTicketAsync(Guid ticketId, Guid staffUserId)
    {
        var ticket = await _db.SupportTickets.FindAsync(ticketId)
            ?? throw new KeyNotFoundException($"Ticket {ticketId} not found.");

        ticket.Status     = TicketStatus.Resolved;
        ticket.ResolvedAt = DateTime.UtcNow;
        _db.SupportTickets.Update(ticket);
        await _db.SaveChangesAsync();
        return ticket;
    }

    public async Task<SupportTicket> ReopenTicketAsync(Guid ticketId)
    {
        var ticket = await _db.SupportTickets.FindAsync(ticketId)
            ?? throw new KeyNotFoundException($"Ticket {ticketId} not found.");

        ticket.Status     = TicketStatus.Open;
        ticket.ResolvedAt = null;
        _db.SupportTickets.Update(ticket);
        await _db.SaveChangesAsync();
        return ticket;
    }

    public async Task<SupportTicket> AssignToUserAsync(Guid ticketId, Guid userId)
    {
        var ticket = await _db.SupportTickets.FindAsync(ticketId)
            ?? throw new KeyNotFoundException($"Ticket {ticketId} not found.");

        ticket.AssignedUserId = userId;
        if (ticket.Status == TicketStatus.Open)
            ticket.Status = TicketStatus.InProgress;
        _db.SupportTickets.Update(ticket);
        await _db.SaveChangesAsync();
        return ticket;
    }

    public Task<List<SupportTicket>> GetTicketsForUserAsync(Guid userId) =>
        _db.SupportTickets
           .Include(t => t.Customer)
           .Include(t => t.Replies)
           .Where(t => t.AssignedUserId == userId)
           .OrderByDescending(t => t.CreatedAt)
           .ToListAsync();
}
