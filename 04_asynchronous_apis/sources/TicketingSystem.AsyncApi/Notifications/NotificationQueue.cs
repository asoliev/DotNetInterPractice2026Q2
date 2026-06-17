using System.Threading.Channels;

namespace TicketingSystem.AsyncApi.Notifications;

public interface INotificationQueue
{
    ValueTask EnqueueAsync(NotificationMessage message, CancellationToken cancellationToken = default);

    IAsyncEnumerable<NotificationMessage> ReadAllAsync(CancellationToken cancellationToken = default);
}

public sealed class InMemoryNotificationQueue : INotificationQueue
{
    private readonly Channel<NotificationMessage> _channel = Channel.CreateUnbounded<NotificationMessage>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false,
    });

    public ValueTask EnqueueAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        return _channel.Writer.WriteAsync(message, cancellationToken);
    }

    public IAsyncEnumerable<NotificationMessage> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}