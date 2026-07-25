using DDFC.Domain.Enums;

namespace DDFC.Application.Interfaces;

public interface INotificationService
{
    Task SendToCustomerAsync(Guid customerId, Guid? requestId, string title, string body,
        NotificationChannel channel = NotificationChannel.InApp, bool requiresResponse = false);

    Task<bool> MarkReadAsync(Guid notificationId, Guid customerId);

    Task<bool> SubmitResponseAsync(Guid notificationId, Guid customerId, string responseText);
}
