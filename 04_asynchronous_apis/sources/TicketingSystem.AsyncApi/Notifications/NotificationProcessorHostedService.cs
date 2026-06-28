using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace TicketingSystem.AsyncApi.Notifications;

public sealed class NotificationProcessorHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqOptions> rabbitMqOptions,
    ILogger<NotificationProcessorHostedService> logger) : BackgroundService
{
    private static readonly AsyncRetryPolicy<EmailSendResult> EmailSendRetryPolicy = Policy<EmailSendResult>
        .Handle<Exception>()
        .OrResult(result => !result.IsSuccess)
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: _ => TimeSpan.FromSeconds(2));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        RabbitMqOptions options = rabbitMqOptions.Value;

        ConnectionFactory factory = new()
        {
            HostName = options.HostName,
            Port = options.Port,
            UserName = options.UserName,
            Password = options.Password,
            VirtualHost = options.VirtualHost
        };

        await using IConnection connection = await factory.CreateConnectionAsync(stoppingToken);
        await using IChannel channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        await channel.BasicQosAsync(0, 1, false, stoppingToken);

        AsyncEventingBasicConsumer consumer = new(channel);
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            NotificationMessage? message = JsonSerializer.Deserialize<NotificationMessage>(Encoding.UTF8.GetString(eventArgs.Body.ToArray()));
            if (message is null)
            {
                logger.LogWarning("RabbitMQ message could not be deserialized.");
                await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
                return;
            }

            using IServiceScope scope = scopeFactory.CreateScope();
            INotificationStatusStore statusStore = scope.ServiceProvider.GetRequiredService<INotificationStatusStore>();
            IEmailProviderClient emailProvider = scope.ServiceProvider.GetRequiredService<IEmailProviderClient>();

            logger.LogInformation(
                "Notification {TrackingId} moved to in progress for operation {OperationName}.",
                message.TrackingId,
                message.OperationName);

            await statusStore.MarkInProgressAsync(message, stoppingToken);

            try
            {
                EmailSendResult result = await EmailSendRetryPolicy.ExecuteAsync(
                    async ct => await emailProvider.SendAsync(CreateEmailRequest(message), ct),
                    stoppingToken);

                if (result.IsSuccess)
                {
                    await statusStore.MarkSentAsync(message.TrackingId, result.Message, stoppingToken);

                    logger.LogInformation(
                        "Email provider accepted notification {TrackingId} for {CustomerEmail}.",
                        message.TrackingId,
                        message.Parameters.CustomerEmail);
                }
                else
                {
                    await statusStore.MarkFailedAsync(message.TrackingId, result.Message, stoppingToken);

                    logger.LogWarning(
                        "Email provider rejected notification {TrackingId} for {CustomerEmail}: {Message}",
                        message.TrackingId,
                        message.Parameters.CustomerEmail,
                        result.Message);
                }

                await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
            }
            catch (Exception exception)
            {
                await statusStore.MarkFailedAsync(message.TrackingId, exception.Message, stoppingToken);

                logger.LogInformation(
                    "Email provider failed for notification {TrackingId} for {CustomerEmail}: {Message}",
                    message.TrackingId,
                    message.Parameters.CustomerEmail,
                    exception.Message);

                await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(
            queue: options.QueueName,
            autoAck: false,
            consumerTag: string.Empty,
            noLocal: false,
            exclusive: false,
            arguments: null,
            consumer: consumer,
            cancellationToken: stoppingToken);

        TaskCompletionSource stopSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using CancellationTokenRegistration registration = stoppingToken.Register(() => stopSignal.TrySetResult());
        await stopSignal.Task;
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