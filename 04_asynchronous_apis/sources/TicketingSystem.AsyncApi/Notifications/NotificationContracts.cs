namespace TicketingSystem.AsyncApi.Notifications;

public sealed record NotificationParameters(string CustomerEmail, string CustomerName);

public sealed record NotificationContent(decimal OrderAmount, string OrderSummary);

public sealed record NotificationMessage(
    Guid TrackingId,
    string OperationName,
    DateTimeOffset Timestamp,
    NotificationParameters Parameters,
    NotificationContent Content);