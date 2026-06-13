using System.Collections.Concurrent;

namespace TicketingSystem.AsyncApi.Caching;

public interface ISeatBookingGate
{
    Task<T> ExecuteAsync<T>(int eventId, int seatId, Func<Task<T>> action, CancellationToken cancellationToken = default);
}

public sealed class SeatBookingGate : ISeatBookingGate
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.Ordinal);

    public async Task<T> ExecuteAsync<T>(int eventId, int seatId, Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        string key = BuildKey(eventId, seatId);
        SemaphoreSlim gate = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        await gate.WaitAsync(cancellationToken);
        try
        {
            return await action();
        }
        finally
        {
            gate.Release();
        }
    }

    private static string BuildKey(int eventId, int seatId) => $"{eventId}:{seatId}";
}