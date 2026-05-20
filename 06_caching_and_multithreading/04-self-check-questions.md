1. How ASP.NET API handles multiple requests?

ASP.NET Core processes requests asynchronously on top of the .NET thread pool and Kestrel I/O pipeline. Each incoming request gets its own execution context, and `async/await` frees worker threads while waiting on I/O (database, network, file access). This allows high concurrency without creating one dedicated thread per request. The runtime schedules continuations and tries to maximize throughput while preserving request isolation.

2. What are the benefits and downsides of caching? When should we consider applying caching?

Benefits:
- Lower latency and faster responses.
- Reduced load on database/external systems.
- Better throughput and improved scalability.

Downsides:
- Risk of stale data.
- Added complexity (invalidation, expiration, consistency).
- Extra memory or infrastructure costs.

When to apply caching:
- Data is read frequently and changes relatively infrequently.
- Expensive operations are repeated often.
- Occasional staleness is acceptable, or you have clear invalidation strategy.

3. What are the differences between In-memory, Distributed or Request caching options?

- In-memory cache:
	- Stored inside one app instance process.
	- Fastest access and simplest setup.
	- Not shared across instances; cache is lost on restart.

- Distributed cache (Redis, SQL, etc.):
	- Shared cache storage used by multiple app instances.
	- Better for scaled-out deployments.
	- Slightly slower than in-process and requires external infrastructure.

- Request (HTTP) caching:
	- Client/proxy/browser stores responses based on HTTP headers (`Cache-Control`, `ETag`, `Last-Modified`).
	- Can avoid sending full responses when data is unchanged (`304 Not Modified`).
	- Works across network boundary, depends on proper header design.

4. What does session affinity and thread affinity mean? When do we have to consider session affinity?

- Session affinity (sticky sessions): requests from the same client are routed to the same server instance.
- Thread affinity: code expects execution to continue on the same thread.

When to consider session affinity:
- When session/state is stored in memory of a specific app instance.
- It is generally better to avoid this dependency by externalizing state (distributed cache/database), especially in load-balanced systems.

In ASP.NET Core server code, thread affinity should not be assumed; continuations may resume on different threads.

5. What are race conditions and deadlocks? Are they possible in a single threaded application?

- Race condition: output depends on non-deterministic timing/order of operations over shared state.
- Deadlock: two or more operations wait on each other forever.

Single-threaded apps:
- Classic data races are typically a multithreading problem.
- Deadlock-like hangs can still happen in single-threaded async/event-loop models when work blocks the only execution path or waits cyclically.

6. Why is it not safe to use static constructors/fields when your code is running in a multithreaded application?

Static state is shared by all requests/threads. If mutable static data is accessed without synchronization, race conditions and visibility issues can occur. Even with static constructors being type-initialization-safe, mutable static fields remain global shared state and can become contention/hotspot points. Prefer immutable static data or proper synchronization.

7. What objects and features does .NET propose to solve race conditions and deadlocks?

- `lock` / `Monitor` for critical sections.
- `Mutex`, `SemaphoreSlim`, `ReaderWriterLockSlim` for controlled access patterns.
- `Interlocked` and `Volatile` for low-level atomic/visibility operations.
- Thread-safe collections (`ConcurrentDictionary`, `ConcurrentQueue`, etc.).
- Async coordination primitives (`SemaphoreSlim.WaitAsync`, channels).
- `CancellationToken`, timeouts, and consistent lock ordering to prevent deadlocks.
- Higher-level models like TPL, `Task`, and dataflow/channel patterns to reduce manual shared-state synchronization.