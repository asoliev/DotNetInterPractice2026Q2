using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
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
            IReadOnlyCollection<INotificationDistributionChannel> channels = scope.ServiceProvider.GetServices<INotificationDistributionChannel>().ToArray();

            logger.LogInformation(
                "Notification {TrackingId} moved to in progress for operation {OperationName}.",
                message.TrackingId,
                message.OperationName);

            await statusStore.MarkInProgressAsync(message, stoppingToken);

            try
            {
                if (channels.Count == 0)
                {
                    const string noChannelsMessage = "No notification distribution channels are registered.";
                    await statusStore.MarkFailedAsync(message.TrackingId, noChannelsMessage, stoppingToken);

                    logger.LogWarning(
                        "Notification {TrackingId} for {CustomerEmail} could not be processed: {Message}",
                        message.TrackingId,
                        message.Parameters.CustomerEmail,
                        noChannelsMessage);
                }
                else
                {
                    List<string> failureMessages = [];
                    string? successMessage = null;
                    bool anySuccess = false;

                    foreach (INotificationDistributionChannel distributionChannel in channels)
                    {
                        string channelName = distributionChannel.GetType().Name;

                        try
                        {
                            EmailSendResult result = await EmailSendRetryPolicy.ExecuteAsync(
                                async ct => await distributionChannel.ProcessAsync(message, ct),
                                stoppingToken);

                            if (result.IsSuccess)
                            {
                                anySuccess = true;
                                successMessage ??= result.Message;

                                logger.LogInformation(
                                    "Notification channel {ChannelName} accepted notification {TrackingId} for {CustomerEmail}.",
                                    channelName,
                                    message.TrackingId,
                                    message.Parameters.CustomerEmail);
                            }
                            else
                            {
                                failureMessages.Add($"{channelName}: {result.Message}");

                                logger.LogWarning(
                                    "Notification channel {ChannelName} rejected notification {TrackingId} for {CustomerEmail}: {Message}",
                                    channelName,
                                    message.TrackingId,
                                    message.Parameters.CustomerEmail,
                                    result.Message);
                            }
                        }
                        catch (Exception exception)
                        {
                            failureMessages.Add($"{channelName}: {exception.Message}");

                            logger.LogWarning(
                                exception,
                                "Notification channel {ChannelName} failed for notification {TrackingId} for {CustomerEmail}.",
                                channelName,
                                message.TrackingId,
                                message.Parameters.CustomerEmail);
                        }
                    }

                    if (anySuccess)
                    {
                        await statusStore.MarkSentAsync(message.TrackingId, successMessage ?? "Notification accepted by at least one channel.", stoppingToken);

                        logger.LogInformation(
                            "Notification {TrackingId} was accepted by at least one distribution channel for {CustomerEmail}.",
                            message.TrackingId,
                            message.Parameters.CustomerEmail);
                    }
                    else
                    {
                        string failureMessage = failureMessages.Count > 0
                            ? string.Join(" | ", failureMessages)
                            : "Notification processing failed for all registered channels.";

                        await statusStore.MarkFailedAsync(message.TrackingId, failureMessage, stoppingToken);

                        logger.LogWarning(
                            "Notification {TrackingId} failed for {CustomerEmail}: {Message}",
                            message.TrackingId,
                            message.Parameters.CustomerEmail,
                            failureMessage);
                    }
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