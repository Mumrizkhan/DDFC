using DDFC.Application.Interfaces;
using DDFC.Domain.Entities;
using DDFC.Domain.Enums;
using DDFC.Infrastructure.Persistence;

namespace DDFC.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly DDFCDbContext _db;

    public NotificationService(DDFCDbContext db) => _db = db;

    public async Task SendToCustomerAsync(Guid customerId, Guid? requestId, string title,
        string body, NotificationChannel channel = NotificationChannel.InApp, bool requiresResponse = false)
    {
        var notification = new CustomerNotification
        {
            CustomerId       = customerId,
            RequestId        = requestId,
            Title            = title,
            MessageBody      = body,
            Channel          = channel,
            RequiresResponse = requiresResponse,
            SentAt           = DateTime.UtcNow
        };

        _db.CustomerNotifications.Add(notification);
        await _db.SaveChangesAsync();

        // Stub: wire real SMS / Email providers here
        if (channel == NotificationChannel.SMS)
            await SendSmsStubAsync(customerId, body);
        else if (channel == NotificationChannel.Email)
            await SendEmailStubAsync(customerId, title, body);
    }

    public async Task<bool> MarkReadAsync(Guid notificationId, Guid customerId)
    {
        var n = await _db.CustomerNotifications.FindAsync(notificationId);
        if (n is null || n.CustomerId != customerId) return false;
        n.IsRead = true;
        _db.CustomerNotifications.Update(n);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SubmitResponseAsync(Guid notificationId, Guid customerId, string responseText)
    {
        var n = await _db.CustomerNotifications.FindAsync(notificationId);
        if (n is null || n.CustomerId != customerId || !n.RequiresResponse) return false;
        n.ResponseText = responseText;
        n.IsRead       = true;
        _db.CustomerNotifications.Update(n);
        await _db.SaveChangesAsync();
        return true;
    }

    // ── Stubs (replace with real providers) ───────────────────────────────────
    private Task SendSmsStubAsync(Guid customerId, string body) =>
        Task.CompletedTask; // TODO: integrate SMS gateway

    private Task SendEmailStubAsync(Guid customerId, string subject, string body) =>
        Task.CompletedTask; // TODO: integrate SMTP / SendGrid
}
