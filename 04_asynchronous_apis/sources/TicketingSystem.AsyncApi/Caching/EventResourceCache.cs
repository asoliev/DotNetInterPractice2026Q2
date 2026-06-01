using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Net.Http.Headers;
using TicketingSystem.AsyncApi.Contracts.Responses;

namespace TicketingSystem.AsyncApi.Caching;

public interface IEventResourceCache
{
    Task<IReadOnlyCollection<EventResponse>> GetEventsAsync(
        Func<CancellationToken, Task<IReadOnlyCollection<EventResponse>>> factory,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<EventSeatResponse>> GetSectionSeatsAsync(
        int eventId,
        int sectionId,
        Func<CancellationToken, Task<IReadOnlyCollection<EventSeatResponse>>> factory,
        CancellationToken cancellationToken = default);

    EventCacheMetadata GetMetadata(string resourceKey);
    void Invalidate();
}

public sealed class EventResourceCache(IMemoryCache memoryCache) : IEventResourceCache
{
    public const string EventsListResourceKey = "events:list";

    private readonly ConcurrentDictionary<string, byte> _trackedCacheKeys = new(StringComparer.Ordinal);
    private long _version = 1;
    private long _lastModifiedTicks = DateTimeOffset.UtcNow.Ticks;

    public Task<IReadOnlyCollection<EventResponse>> GetEventsAsync(
        Func<CancellationToken, Task<IReadOnlyCollection<EventResponse>>> factory,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string cacheKey = BuildCacheKey(EventsListResourceKey);
        _trackedCacheKeys.TryAdd(cacheKey, 0);

        return memoryCache.GetOrCreateAsync(cacheKey, async entry =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return await factory(cancellationToken);
        })!;
    }

    public Task<IReadOnlyCollection<EventSeatResponse>> GetSectionSeatsAsync(
        int eventId,
        int sectionId,
        Func<CancellationToken, Task<IReadOnlyCollection<EventSeatResponse>>> factory,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string resourceKey = BuildSectionSeatsResourceKey(eventId, sectionId);
        string cacheKey = BuildCacheKey(resourceKey);
        _trackedCacheKeys.TryAdd(cacheKey, 0);

        return memoryCache.GetOrCreateAsync(cacheKey, async entry =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return await factory(cancellationToken);
        })!;
    }

    public EventCacheMetadata GetMetadata(string resourceKey)
    {
        long version = Interlocked.Read(ref _version);
        long ticks = Interlocked.Read(ref _lastModifiedTicks);
        DateTimeOffset lastModified = new(ticks, TimeSpan.Zero);
        string etagValue = $"\"{version}-{Math.Abs(resourceKey.GetHashCode(StringComparison.Ordinal))}\"";

        return new EventCacheMetadata(EntityTagHeaderValue.Parse(etagValue), lastModified);
    }

    public void Invalidate()
    {
        Interlocked.Increment(ref _version);
        Interlocked.Exchange(ref _lastModifiedTicks, DateTimeOffset.UtcNow.Ticks);

        foreach (string key in _trackedCacheKeys.Keys)
        {
            memoryCache.Remove(key);
        }

        _trackedCacheKeys.Clear();
    }

    public static string BuildSectionSeatsResourceKey(int eventId, int sectionId) =>
        $"events:{eventId}:sections:{sectionId}:seats";

    private string BuildCacheKey(string resourceKey)
    {
        long version = Interlocked.Read(ref _version);
        return $"{resourceKey}:v{version}";
    }
}

public sealed record EventCacheMetadata(EntityTagHeaderValue ETag, DateTimeOffset LastModified);