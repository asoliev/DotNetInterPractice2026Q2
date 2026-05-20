# Caching And Multithreading Module - Home Task Report

## Scope
This report covers implementation from `03-home-task.md`:
- Task 1: Server-side caching for Event resource + cache invalidation from Order POST/PUT.
- Task 2: HTTP request caching for `/events/*` endpoints.

## Implemented Work

### 1) Server-side Event Caching (Task 1)
Implemented in-memory cache service:
- File: `src/TicketingSystem.AsyncApi/Caching/EventResourceCache.cs`
- Added API:
  - `GetEventsAsync(...)`
  - `GetSectionSeatsAsync(...)`
  - `Invalidate()`

Registered cache in DI:
- File: `src/TicketingSystem.AsyncApi/Program.cs`
- Added:
  - `AddMemoryCache()`
  - `AddSingleton<IEventResourceCache, EventResourceCache>()`

Used cache in Event endpoints:
- File: `src/TicketingSystem.AsyncApi/Controllers/EventsController.cs`
- `/events` and `/events/{eventId}/sections/{sectionId}/seats` now read via cache service.

### 2) Cache Invalidation on Order POST/PUT (Task 1)
- File: `src/TicketingSystem.AsyncApi/Controllers/OrdersController.cs`
- Invalidate Event cache after successful:
  - `POST /orders/carts/{cartId}` (add seat)
  - `PUT /orders/carts/{cartId}/book` (book cart)

`DELETE /orders/carts/{cartId}/events/{eventId}/seats/{seatId}` was intentionally left unchanged per task requirement.

### 3) HTTP Request Caching for Event Endpoints (Task 2)
- File: `src/TicketingSystem.AsyncApi/Controllers/EventsController.cs`
- Added response headers:
  - `Cache-Control: public, max-age=30, must-revalidate`
  - `ETag`
  - `Last-Modified`
  - `Expires`
  - `Vary: Accept`
- Added conditional request handling:
  - If `If-None-Match` or `If-Modified-Since` matches current metadata -> `304 Not Modified`.

### 4) Integration Tests for Caching
- File: `src/TicketingSystem.IntegrationTests/EventCachingIntegrationTests.cs`
- Covered cases:
  - `GET /events` returns cache headers and `304` for matching ETag.
  - `GET /events/{eventId}/sections/{sectionId}/seats` returns cache headers and `304` for matching ETag.
  - Event and seat caches are invalidated after successful `POST /orders/carts/{cartId}`.
  - Event and seat caches are invalidated after successful `PUT /orders/carts/{cartId}/book`.

## Validation
Build command used:
- `dotnet build src/TicketingSystem.slnx`

Result:
- Build succeeded for Async API and referenced projects.

## Notes
- Local folder `.dotnet-cli-home/` appeared because build was run with local `DOTNET_CLI_HOME`; it should be ignored by git.
- Observed improvement in the live run:
  - First `GET /events` took about 34-57 ms and returned `200`.
  - Conditional `GET /events` with `If-None-Match` returned `304` in about 2 ms.
  - First `GET /events/{eventId}/sections/{sectionId}/seats` took about 58-63 ms and returned `200`.
  - Conditional seat request returned `304` in about 0.16-2 ms.
  - After successful order POST/PUT, the next Event requests returned fresh `ETag` values, confirming invalidation worked.
