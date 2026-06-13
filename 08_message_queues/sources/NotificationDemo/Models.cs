namespace TicketingSystem.NotificationDemo;

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

public sealed record EmailResult(bool IsSuccess, string ProviderMessage);

public sealed record NotificationRecord(
    Guid TrackingId,
    string OperationName,
    DateTimeOffset CreatedAt,
    string CustomerEmail,
    string CustomerName,
    decimal OrderAmount,
    string OrderSummary,
    NotificationStatus Status,
    DateTimeOffset UpdatedAt,
    string? ProviderMessage);
