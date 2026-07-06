namespace TicketingSystem.AsyncApi.Notifications;

public interface INotificationDistributionChannel
{
    Task<EmailSendResult> ProcessAsync(NotificationMessage message, CancellationToken cancellationToken = default);
}