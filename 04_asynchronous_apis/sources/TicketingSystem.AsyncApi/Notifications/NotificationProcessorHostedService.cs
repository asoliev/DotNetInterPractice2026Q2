using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TicketingSystem.AsyncApi.Notifications;

public sealed class NotificationProcessorHostedService(
    INotificationQueue notificationQueue,
    ILogger<NotificationProcessorHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (NotificationMessage message in notificationQueue.ReadAllAsync(stoppingToken))
        {
            logger.LogInformation(
                "Notification {TrackingId} moved to in progress for operation {OperationName}.",
                message.TrackingId,
                message.OperationName);

            await Task.Delay(150, stoppingToken);

            bool isSuccess = !message.Parameters.CustomerEmail.Contains("fail", StringComparison.OrdinalIgnoreCase);
            if (isSuccess)
            {
                logger.LogInformation(
                    "Email provider accepted notification {TrackingId} for {CustomerEmail}.",
                    message.TrackingId,
                    message.Parameters.CustomerEmail);
                continue;
            }

            logger.LogWarning(
                "Email provider rejected notification {TrackingId} for {CustomerEmail}.",
                message.TrackingId,
                message.Parameters.CustomerEmail);
        }
    }
}