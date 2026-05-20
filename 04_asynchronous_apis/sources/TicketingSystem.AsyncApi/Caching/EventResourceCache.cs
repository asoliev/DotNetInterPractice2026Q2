using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using TicketingSystem.AsyncApi.Contracts.Responses;

namespace TicketingSystem.AsyncApi.Caching;

public interface IEventResourceCache
{
    Task<IReadOnlyCollection<EventResponse>> GetEventsAsync(
        Func<Task<IReadOnlyCollection<EventResponse>>> factory,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<EventSeatResponse>> GetSectionSeatsAsync(
        int eventId,
        int sectionId,
        Func<Task<IReadOnlyCollection<EventSeatResponse>>> factory,
        CancellationToken cancellationToken = default);

    void Invalidate();
}

public sealed class EventResourceCache(IMemoryCache memoryCache) : IEventResourceCache
{
    private readonly ConcurrentDictionary<string, byte> _trackedCacheKeys = new(StringComparer.Ordinal);
    private long _version = 1;

    public Task<IReadOnlyCollection<EventResponse>> GetEventsAsync(
        Func<Task<IReadOnlyCollection<EventResponse>>> factory,
        CancellationToken cancellationToken = default)
    {
        string cacheKey = BuildCacheKey("events:list");
        _trackedCacheKeys.TryAdd(cacheKey, 0);

        return memoryCache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return await factory();
        })!;
    }

    public Task<IReadOnlyCollection<EventSeatResponse>> GetSectionSeatsAsync(
        int eventId,
        int sectionId,
        Func<Task<IReadOnlyCollection<EventSeatResponse>>> factory,
        CancellationToken cancellationToken = default)
    {
        string resourceKey = $"events:{eventId}:sections:{sectionId}:seats";
        string cacheKey = BuildCacheKey(resourceKey);
        _trackedCacheKeys.TryAdd(cacheKey, 0);

        return memoryCache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return await factory();
        })!;
    }

    public void Invalidate()
    {
        foreach (string key in _trackedCacheKeys.Keys)
        {
            memoryCache.Remove(key);
        }

        _trackedCacheKeys.Clear();
        Interlocked.Increment(ref _version);
    }

    private string BuildCacheKey(string resourceKey)
    {
        long version = Interlocked.Read(ref _version);
        return $"{resourceKey}:v{version}";
    }
}