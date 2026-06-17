using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TicketingSystem.DAL.EF;

namespace TicketingSystem.AsyncApi.Notifications;

public sealed class NotificationProcessorHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqOptions> rabbitMqOptions,
    ILogger<NotificationProcessorHostedService> logger) : BackgroundService
{
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

        using IConnection connection = factory.CreateConnection();
        using IModel channel = connection.CreateModel();

        channel.QueueDeclare(
            queue: options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        channel.BasicQos(0, 1, false);

        EventingBasicConsumer consumer = new(channel);
        consumer.Received += async (_, eventArgs) =>
        {
            NotificationMessage? message = JsonSerializer.Deserialize<NotificationMessage>(Encoding.UTF8.GetString(eventArgs.Body.ToArray()));
            if (message is null)
            {
                logger.LogWarning("RabbitMQ message could not be deserialized.");
                channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: false);
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
                EmailSendResult result = await emailProvider.SendAsync(CreateEmailRequest(message), stoppingToken);

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

                channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
            }
            catch (Exception exception)
            {
                await statusStore.MarkFailedAsync(message.TrackingId, exception.Message, stoppingToken);

                logger.LogInformation(
                    "Email provider failed for notification {TrackingId} for {CustomerEmail}: {Message}",
                    message.TrackingId,
                    message.Parameters.CustomerEmail,
                    exception.Message);

                channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
            }
        };

        channel.BasicConsume(queue: options.QueueName, autoAck: false, consumer: consumer);

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