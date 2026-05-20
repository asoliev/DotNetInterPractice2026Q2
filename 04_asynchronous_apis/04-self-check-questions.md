1.  What are the benefits and drawbacks of async programming?
Benefits:
- Better scalability for I/O-heavy workloads: while awaiting DB/network/file operations, request threads are returned to the thread pool, so the server can process more concurrent requests.
- Better responsiveness and throughput under load: fewer blocked threads reduces request queueing.
- More efficient resource usage: CPU threads are not occupied by waiting.

Drawbacks:
- Higher code complexity: async/await chains are harder to reason about than simple synchronous flows.
- Harder debugging/tracing: call stacks are split across awaited continuations.
- Potential pitfalls: deadlocks (mainly when blocking on async), accidental sync-over-async, and lost cancellation propagation.
- Async does not improve CPU-bound operations by itself; for CPU-heavy work, parallelism/background processing is needed.

2.  How to make APS.NET controller action support async flow?
In ASP.NET Core, define action methods as asynchronous and use Task-based return types.

Example pattern:
```csharp
[HttpGet("{id:int}")]
public async Task<IActionResult> GetByIdAsync(int id)
{
	var entity = await repository.GetByIdAsync(id);
	if (entity is null) return NotFound();
	return Ok(entity);
}
```

Key points:
- Use `async` + `await` and return `Task<IActionResult>` (or `Task<T>`).
- Call async APIs end-to-end (`ToListAsync`, `SaveChangesAsync`, repository async methods).
- Avoid blocking calls like `.Result`/`.Wait()`.
- Optionally accept `CancellationToken` and pass it to async operations.

3.  How does async flow influences APS.NET request executions (life cycle)?
With synchronous code, a worker thread is blocked for the entire request lifetime whenever I/O waits occur.

With async flow:
- Request enters middleware pipeline and controller action on a thread.
- At `await` of incomplete I/O, the thread is released to the pool.
- When I/O completes, continuation resumes on a pool thread and processing continues.

Impact:
- Better concurrency under I/O pressure because threads are not tied up waiting.
- Lower chance of thread pool starvation and timeout cascades.
- Middleware/action filters still execute in the same logical request flow; only thread usage model changes.

4.  List at least 5 tips on ASP.NET API performance best practices.
1. Use async end-to-end for I/O operations (DB, HTTP, file, cache).
2. Use pagination/filtering; never return unbounded large datasets.
3. Minimize payload size: return DTOs, avoid over-fetching, enable compression when appropriate.
4. Optimize data access: proper indexes, projection queries, avoid N+1 queries, use `AsNoTracking()` for read-only queries.
5. Add caching (response caching/output caching/distributed cache) for frequently requested data.
6. Reuse expensive resources correctly (`HttpClientFactory`, connection pooling).
7. Measure and tune with logging/metrics/tracing and load testing before optimization.
8. Keep hot paths allocation-friendly and avoid unnecessary serialization work.

5.  Vertical vs Horizontal scalability. Where to use each?
Vertical scalability (scale up):
- Increase resources on one node (more CPU/RAM/faster disk).
- Good for: quick growth phase, simpler operations, stateful legacy systems.
- Limits: hardware ceiling and larger blast radius (single-node bottleneck).

Horizontal scalability (scale out):
- Add more nodes/instances behind a load balancer.
- Good for: high-traffic web APIs, fault tolerance, elastic cloud workloads.
- Requires stateless app design or externalized shared state (DB/cache/message broker).

Practical approach:
- Start with moderate vertical scaling for simplicity.
- Move toward horizontal scaling for sustained growth, resilience, and high availability.

6.  Explain why the PUT method was suggested for the book action on the order.
`PUT /orders/carts/{cart_id}/book` is reasonable because booking is modeled as setting cart/order resource state to a specific target state (booked).

Why PUT fits:
- Semantic alignment: client requests a deterministic state transition of an existing resource.
- Idempotency expectation: repeated equivalent requests should ideally not create multiple bookings/payments (or should be handled safely with idempotency controls).

Important implementation note:
- If each repeated call creates a new side effect (for example new payment each time), behavior becomes non-idempotent and drifts toward POST semantics.
- So, when using PUT here, server logic should enforce idempotent booking behavior for the same cart.