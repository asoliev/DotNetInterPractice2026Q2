namespace TicketingSystem.AsyncApi.Notifications;

public sealed class LocalMockEmailProviderClient(ILogger<LocalMockEmailProviderClient> logger) : IEmailProviderClient
{
    public Task<EmailSendResult> ProcessAsync(NotificationMessage message, CancellationToken cancellationToken = default) =>
        SendAsync(CreateEmailRequest(message), cancellationToken);

    public Task<EmailSendResult> SendAsync(EmailRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Mock email provider accepted notification {TrackingId} for {CustomerEmail}.",
            request.TrackingId,
            request.ToEmail);

        return Task.FromResult(new EmailSendResult(true, "Mock email provider accepted the message."));
    }

    private static EmailRequest CreateEmailRequest(NotificationMessage message)
    {
        string subject = $"[{message.OperationName}] Order confirmation for {message.Parameters.CustomerName}";
        string body =
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
