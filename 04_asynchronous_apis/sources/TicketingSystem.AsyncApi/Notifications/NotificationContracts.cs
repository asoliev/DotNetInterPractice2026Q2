namespace TicketingSystem.AsyncApi.Notifications;

public enum NotificationStatus
{
    Pending,
    InProgress,
    Sent,
    Failed,
}

public sealed record NotificationParameters(string CustomerEmail, string CustomerName);

public sealed record NotificationContent(decimal OrderAmount, string OrderSummary);

public sealed record NotificationMessage(
    Guid TrackingId,
    string OperationName,
    DateTimeOffset Timestamp,
    NotificationParameters Parameters,
    NotificationContent Content);

public sealed record EmailRequest(
    Guid TrackingId,
    string ToEmail,
    string ToName,
    string Subject,
    string Body);

public sealed record EmailSendResult(bool IsSuccess, string Message);