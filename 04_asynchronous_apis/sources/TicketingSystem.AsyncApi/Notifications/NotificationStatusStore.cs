using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TicketingSystem.DAL.EF;

namespace TicketingSystem.AsyncApi.Notifications;

public interface INotificationStatusStore
{
    Task EnsureSchemaAsync(CancellationToken cancellationToken = default);

    Task CreatePendingAsync(NotificationMessage message, CancellationToken cancellationToken = default);

    Task MarkInProgressAsync(NotificationMessage message, CancellationToken cancellationToken = default);

    Task MarkSentAsync(Guid trackingId, string providerMessage, CancellationToken cancellationToken = default);

    Task MarkFailedAsync(Guid trackingId, string errorMessage, CancellationToken cancellationToken = default);
}

public sealed class NotificationStatusStore(TicketingDbContext dbContext) : INotificationStatusStore
{
    public async Task EnsureSchemaAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
        CREATE TABLE IF NOT EXISTS NotificationRequests (
            TrackingId TEXT NOT NULL PRIMARY KEY,
            OperationName TEXT NOT NULL,
            CustomerEmail TEXT NOT NULL,
            CustomerName TEXT NOT NULL,
            ContentJson TEXT NOT NULL,
            Status TEXT NOT NULL,
            CreatedAt TEXT NOT NULL,
            UpdatedAt TEXT NOT NULL,
            LastError TEXT NULL,
            ProviderMessage TEXT NULL
        );
        """;

        await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    public Task CreatePendingAsync(NotificationMessage message, CancellationToken cancellationToken = default) =>
        UpsertAsync(message, NotificationStatus.Pending, providerMessage: null, lastError: null, cancellationToken);

    public Task MarkInProgressAsync(NotificationMessage message, CancellationToken cancellationToken = default) =>
        UpsertAsync(message, NotificationStatus.InProgress, providerMessage: null, lastError: null, cancellationToken);

    public Task MarkSentAsync(Guid trackingId, string providerMessage, CancellationToken cancellationToken = default) =>
        UpdateStatusAsync(trackingId, NotificationStatus.Sent, providerMessage, lastError: null, cancellationToken);

    public Task MarkFailedAsync(Guid trackingId, string errorMessage, CancellationToken cancellationToken = default) =>
        UpdateStatusAsync(trackingId, NotificationStatus.Failed, providerMessage: null, lastError: errorMessage, cancellationToken);

    private async Task UpsertAsync(
        NotificationMessage message,
        NotificationStatus status,
        string? providerMessage,
        string? lastError,
        CancellationToken cancellationToken)
    {
        const string sql = """
        INSERT INTO NotificationRequests
            (TrackingId, OperationName, CustomerEmail, CustomerName, ContentJson, Status, CreatedAt, UpdatedAt, LastError, ProviderMessage)
        VALUES
            ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9})
        ON CONFLICT(TrackingId) DO UPDATE SET
            OperationName = excluded.OperationName,
            CustomerEmail = excluded.CustomerEmail,
            CustomerName = excluded.CustomerName,
            ContentJson = excluded.ContentJson,
            Status = excluded.Status,
            UpdatedAt = excluded.UpdatedAt,
            LastError = excluded.LastError,
            ProviderMessage = excluded.ProviderMessage;
        """;

        await dbContext.Database.ExecuteSqlRawAsync(
            sql,
            [
                message.TrackingId,
                message.OperationName,
                message.Parameters.CustomerEmail,
                message.Parameters.CustomerName,
                JsonSerializer.Serialize(message.Content),
                status.ToString(),
                message.Timestamp.UtcDateTime,
                DateTime.UtcNow,
                lastError is null ? DBNull.Value : lastError,
                providerMessage is null ? DBNull.Value : providerMessage
            ],
            cancellationToken);
    }

    private async Task UpdateStatusAsync(
        Guid trackingId,
        NotificationStatus status,
        string? providerMessage,
        string? lastError,
        CancellationToken cancellationToken)
    {
        const string sql = """
        UPDATE NotificationRequests
        SET Status = {1}, UpdatedAt = {2}, LastError = {3}, ProviderMessage = {4}
        WHERE TrackingId = {0};
        """;

        await dbContext.Database.ExecuteSqlRawAsync(
            sql,
            [
                trackingId,
                status.ToString(),
                DateTime.UtcNow,
                lastError is null ? DBNull.Value : lastError,
                providerMessage is null ? DBNull.Value : providerMessage
            ],
            cancellationToken);
    }
}