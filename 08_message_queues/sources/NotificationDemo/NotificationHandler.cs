namespace TicketingSystem.NotificationDemo;

public sealed class NotificationHandler
{
    private readonly INotificationQueue _queue;
    private readonly NotificationStore _store;
    private readonly IEmailProvider _emailProvider;

    public NotificationHandler(INotificationQueue queue, NotificationStore store, IEmailProvider emailProvider)
    {
        _queue = queue;
        _store = store;
        _emailProvider = emailProvider;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        await foreach (var message in _queue.ReadAllAsync(cancellationToken))
        {
            Console.WriteLine();
            Console.WriteLine($"Notification handler picked {message.TrackingId} ({message.OperationName}).");
            await _store.UpdateStatusAsync(message.TrackingId, NotificationStatus.InProgress, cancellationToken: cancellationToken);

            try
            {
                var request = CreateEmailRequest(message);
                var result = await _emailProvider.SendAsync(request, cancellationToken);

                var finalStatus = result.IsSuccess ? NotificationStatus.Sent : NotificationStatus.Failed;
                await _store.UpdateStatusAsync(message.TrackingId, finalStatus, result.ProviderMessage, cancellationToken);

                if (result.IsSuccess)
                {
                    Console.WriteLine($"Notification {message.TrackingId} sent successfully.");
                }
                else
                {
                    Console.WriteLine($"Notification {message.TrackingId} failed: {result.ProviderMessage}");
                }
            }
            catch (Exception exception)
            {
                await _store.UpdateStatusAsync(message.TrackingId, NotificationStatus.Failed, exception.Message, cancellationToken);
                Console.WriteLine($"Notification {message.TrackingId} failed with exception: {exception.Message}");
            }
        }
    }

    private static EmailRequest CreateEmailRequest(NotificationMessage message)
    {
        var subject = $"[{message.OperationName}] Order confirmation for {message.Parameters.CustomerName}";
        var body =
            $"Hello {message.Parameters.CustomerName},\n\n" +
            $"Operation: {message.OperationName}\n" +
            $"Timestamp: {message.Timestamp:O}\n" +
            $"Order amount: {message.Content.OrderAmount:C}\n" +
            $"Order summary: {message.Content.OrderSummary}\n\n" +
            $"Tracking id: {message.TrackingId}";

        return new EmailRequest(
            message.TrackingId,
            message.Parameters.CustomerEmail,
            message.Parameters.CustomerName,
            subject,
            body);
    }
}
