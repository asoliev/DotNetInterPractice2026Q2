using System.Text.Json;
using System.Threading.Channels;

namespace TicketingSystem.NotificationDemo;

public interface INotificationQueue
{
    ValueTask EnqueueAsync(NotificationMessage message, CancellationToken cancellationToken = default);

    IAsyncEnumerable<NotificationMessage> ReadAllAsync(CancellationToken cancellationToken = default);

    void Complete();
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

    public void Complete()
    {
        _channel.Writer.TryComplete();
    }
}

public sealed class NotificationStore
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
    };

    public NotificationStore(string filePath)
    {
        _filePath = filePath;
    }

    public async Task AddPendingAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var records = await ReadAllUnlockedAsync(cancellationToken);
            records.Add(new NotificationRecord(
                message.TrackingId,
                message.OperationName,
                message.Timestamp,
                message.Parameters.CustomerEmail,
                message.Parameters.CustomerName,
                message.Content.OrderAmount,
                message.Content.OrderSummary,
                NotificationStatus.Pending,
                message.Timestamp,
                null));

            await WriteAllUnlockedAsync(records, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task UpdateStatusAsync(Guid trackingId, NotificationStatus status, string? providerMessage = null, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var records = await ReadAllUnlockedAsync(cancellationToken);
            var index = records.FindIndex(record => record.TrackingId == trackingId);
            if (index < 0)
            {
                throw new InvalidOperationException($"Notification with tracking id '{trackingId}' was not found.");
            }

            var current = records[index];
            records[index] = current with
            {
                Status = status,
                UpdatedAt = DateTimeOffset.UtcNow,
                ProviderMessage = providerMessage,
            };

            await WriteAllUnlockedAsync(records, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<NotificationRecord>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var records = await ReadAllUnlockedAsync(cancellationToken);
            return records
                .OrderBy(record => record.CreatedAt)
                .ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<List<NotificationRecord>> ReadAllUnlockedAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
        {
            return new List<NotificationRecord>();
        }

        await using var stream = File.OpenRead(_filePath);
        var records = await JsonSerializer.DeserializeAsync<List<NotificationRecord>>(stream, _jsonOptions, cancellationToken);
        return records ?? new List<NotificationRecord>();
    }

    private async Task WriteAllUnlockedAsync(List<NotificationRecord> records, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, records, _jsonOptions, cancellationToken);
    }
}

public interface IEmailProvider
{
    Task<EmailResult> SendAsync(EmailRequest request, CancellationToken cancellationToken = default);
}

public sealed class ConsoleEmailProvider : IEmailProvider
{
    public async Task<EmailResult> SendAsync(EmailRequest request, CancellationToken cancellationToken = default)
    {
        Console.WriteLine();
        Console.WriteLine($"Email provider received request for {request.ToEmail}.");
        await Task.Delay(250, cancellationToken);

        if (request.ToEmail.Contains("fail", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Email provider response: failure.");
            return new EmailResult(false, "Simulated provider failure");
        }

        Console.WriteLine("Email provider response: success.");
        Console.WriteLine($"Subject: {request.Subject}");
        Console.WriteLine(request.Body);
        return new EmailResult(true, "Email was successfully sent");
    }
}
