using TicketingSystem.NotificationDemo;

var storePath = Path.Combine(AppContext.BaseDirectory, "notification-store.json");
var store = new NotificationStore(storePath);
var queue = new InMemoryNotificationQueue();
var emailProvider = new ConsoleEmailProvider();
var handler = new NotificationHandler(queue, store, emailProvider);

Console.WriteLine("Starting notification demo...");
var handlerTask = handler.RunAsync();

await SeedNotificationsAsync(store, queue);
queue.Complete();
await handlerTask;

Console.WriteLine();
Console.WriteLine("Final notification states:");
foreach (var record in await store.GetAllAsync())
{
    Console.WriteLine($"{record.TrackingId} | {record.OperationName} | {record.Status} | {record.ProviderMessage}");
}

static async Task SeedNotificationsAsync(NotificationStore store, INotificationQueue queue)
{
    var notifications = new[]
    {
        new NotificationMessage(
            Guid.NewGuid(),
            "ticket added to checkout",
            DateTimeOffset.UtcNow,
            new NotificationParameters("alice@example.com", "Alice"),
            new NotificationContent(125.50m, "2 tickets for Concert A, seats A1-A2")),
        new NotificationMessage(
            Guid.NewGuid(),
            "ticket successfully checked out",
            DateTimeOffset.UtcNow,
            new NotificationParameters("fail.bob@example.com", "Bob"),
            new NotificationContent(256.00m, "1 VIP ticket for Concert B, seat B7"))
    };

    foreach (var notification in notifications)
    {
        await store.AddPendingAsync(notification);
        Console.WriteLine($"Ticketing app queued notification {notification.TrackingId} for {notification.Parameters.CustomerEmail}.");
        await queue.EnqueueAsync(notification);
    }
}
