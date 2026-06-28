namespace TicketingSystem.AsyncApi.Notifications;

public sealed class LocalMockEmailProviderClient(ILogger<LocalMockEmailProviderClient> logger) : IEmailProviderClient
{
    public Task<EmailSendResult> SendAsync(EmailRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Mock email provider accepted notification {TrackingId} for {CustomerEmail}.",
            request.TrackingId,
            request.ToEmail);

        return Task.FromResult(new EmailSendResult(true, "Mock email provider accepted the message."));
    }
}
