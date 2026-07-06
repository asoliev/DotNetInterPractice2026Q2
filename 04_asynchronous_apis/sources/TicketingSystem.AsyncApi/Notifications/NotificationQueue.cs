using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace TicketingSystem.AsyncApi.Notifications;

public sealed class RabbitMqOptions
{
    public string HostName { get; set; } = "localhost";

    public int Port { get; set; } = 5672;

    public string UserName { get; set; } = "guest";

    public string Password { get; set; } = "guest";

    public string VirtualHost { get; set; } = "/";

    public string QueueName { get; set; } = "ticketing.notifications";
}

public interface INotificationPublisher
{
    Task PublishAsync(NotificationMessage message, CancellationToken cancellationToken = default);
}

public sealed class RabbitMqNotificationPublisher(IOptions<RabbitMqOptions> options) : INotificationPublisher
{
    private readonly RabbitMqOptions _options = options.Value;

    public async Task PublishAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        ConnectionFactory factory = new()
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost
        };

        await using IConnection connection = await factory.CreateConnectionAsync(cancellationToken);
        await using IChannel channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: _options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        byte[] body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: _options.QueueName,
            body: body,
            cancellationToken: cancellationToken);
    }
}