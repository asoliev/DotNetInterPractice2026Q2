# Module 3 — Persistence Level: Implementation Notes

## Task Summary

| Task | Score | Status |
|------|-------|--------|
| Task 1 — CRUD Data Access Layer | 0–69% | ✅ Done |
| Task 2 — Code-first database design + ER model | 70–89% | ✅ Done |
| Self-check questions answered | 90–100% | ✅ Done |

---

## Solution Structure

```
sources/
├── TicketingSystem.sln
├── run.sh                          ← one-command run script
├── TicketingSystem.Domain/         ← entities, enums (no dependencies)
├── TicketingSystem.DAL/            ← interfaces / abstractions only
├── TicketingSystem.DAL.EF/         ← EF Core implementation + migrations
└── TicketingSystem.App/            ← console demo (entry point)
```

### Project dependency graph

```
App → DAL.EF → DAL → Domain
App → DAL
App → Domain
```

---

## Step-by-Step Implementation

### Step 1 — Create solution and projects

```bash
cd intermediate+/03_persistence_level
mkdir -p sources && cd sources

dotnet new sln -n TicketingSystem
dotnet new classlib -n TicketingSystem.Domain
dotnet new classlib -n TicketingSystem.DAL
dotnet new classlib -n TicketingSystem.DAL.EF
dotnet new console  -n TicketingSystem.App

dotnet sln add TicketingSystem.Domain/TicketingSystem.Domain.csproj \
               TicketingSystem.DAL/TicketingSystem.DAL.csproj \
               TicketingSystem.DAL.EF/TicketingSystem.DAL.EF.csproj \
               TicketingSystem.App/TicketingSystem.App.csproj
```

### Step 2 — Add project references

```bash
dotnet add TicketingSystem.DAL/TicketingSystem.DAL.csproj \
       reference TicketingSystem.Domain/TicketingSystem.Domain.csproj

dotnet add TicketingSystem.DAL.EF/TicketingSystem.DAL.EF.csproj \
       reference TicketingSystem.Domain/TicketingSystem.Domain.csproj
dotnet add TicketingSystem.DAL.EF/TicketingSystem.DAL.EF.csproj \
       reference TicketingSystem.DAL/TicketingSystem.DAL.csproj

dotnet add TicketingSystem.App/TicketingSystem.App.csproj \
       reference TicketingSystem.Domain/TicketingSystem.Domain.csproj
dotnet add TicketingSystem.App/TicketingSystem.App.csproj \
       reference TicketingSystem.DAL/TicketingSystem.DAL.csproj
dotnet add TicketingSystem.App/TicketingSystem.App.csproj \
       reference TicketingSystem.DAL.EF/TicketingSystem.DAL.EF.csproj
```

### Step 3 — Add NuGet packages

```bash
# EF Core + SQLite + Design tools on DAL.EF
dotnet add TicketingSystem.DAL.EF/TicketingSystem.DAL.EF.csproj \
       package Microsoft.EntityFrameworkCore
dotnet add TicketingSystem.DAL.EF/TicketingSystem.DAL.EF.csproj \
       package Microsoft.EntityFrameworkCore.Sqlite
dotnet add TicketingSystem.DAL.EF/TicketingSystem.DAL.EF.csproj \
       package Microsoft.EntityFrameworkCore.Design

# App also needs Sqlite runtime + Design (for dotnet ef) + Logging
dotnet add TicketingSystem.App/TicketingSystem.App.csproj \
       package Microsoft.EntityFrameworkCore.Sqlite
dotnet add TicketingSystem.App/TicketingSystem.App.csproj \
       package Microsoft.EntityFrameworkCore.Design
dotnet add TicketingSystem.App/TicketingSystem.App.csproj \
       package Microsoft.Extensions.Logging.Console
```

> **Note:** `Microsoft.EntityFrameworkCore.Design` must be in the startup project for `dotnet ef` tools to work.

---

### Step 4 — Domain model (TicketingSystem.Domain)

Based on the ticketing domain from module 2.

**Entities:**

| Entity | Key relationships |
|--------|------------------|
| `Venue` | Has many `Section`, has many `Event` |
| `Section` | Belongs to `Venue`, has many `Seat` |
| `Seat` | Belongs to `Section`, has many `EventSeat` |
| `Event` | Belongs to `Venue`, has many `EventSeat` |
| `EventSeat` | Links `Event` + `Seat`, has price + `SeatStatus`, has one `OrderItem` |
| `Customer` | Has many `Order` |
| `Order` | Belongs to `Customer`, has many `OrderItem` |
| `OrderItem` | Belongs to `Order`, linked to one `EventSeat` |

**Enums:**
- `SeatStatus`: `Available`, `Booked`, `Sold`
- `OrderStatus`: `Pending`, `Confirmed`, `Cancelled`

---

### Step 5 — DAL abstractions (TicketingSystem.DAL)

**Generic interface:**
```csharp
IRepository<T>
  GetByIdAsync(int id)
  GetAllAsync()
  FindAsync(Expression<Func<T, bool>> predicate)
  AddAsync(T entity)
  Update(T entity)
  Delete(T entity)
```

**Specialised interfaces:**
- `IEventRepository` — `GetUpcomingAsync()`, `GetWithSeatsAsync(int)`
- `IEventSeatRepository` — `GetByEventIdAsync()`, `GetAvailableByEventIdAsync()`, `GetCheapestAvailableAsync()`, `TryChangeStatusAsync()`
- `IOrderRepository` — `GetWithItemsAsync()`, `GetByCustomerIdAsync()`
- `ICustomerRepository` — `GetByEmailAsync()`

**Unit of Work interface:**
```csharp
IUnitOfWork
  Events, EventSeats, Orders, Customers   // repositories
  SaveChangesAsync()
  BeginTransactionAsync()
  CommitTransactionAsync()
  RollbackTransactionAsync()
```

---

### Step 6 — EF Core implementation (TicketingSystem.DAL.EF)

**Files created:**

| File | Purpose |
|------|---------|
| `TicketingDbContext.cs` | DbContext with fluent API model configuration |
| `TicketingDbContextFactory.cs` | `IDesignTimeDbContextFactory` — required for `dotnet ef` commands |
| `Repositories/Repository.cs` | Generic EF implementation of `IRepository<T>` |
| `Repositories/EventRepository.cs` | Includes eager-load for venue/seats |
| `Repositories/EventSeatRepository.cs` | Implements optimistic-style `TryChangeStatusAsync` |
| `Repositories/OrderRepository.cs` | Eager-loads items + customer |
| `Repositories/CustomerRepository.cs` | Lookup by email |
| `UnitOfWork.cs` | Wraps DbContext, creates repositories lazily, manages transactions |

**Key EF configurations:**
- `EventSeat` has unique index on `(EventId, SeatId)` — one seat per event
- `Customer.Email` is unique
- `OrderItem → EventSeat` is 1-to-1 with cascade delete
- Enum columns stored as `int`
- Decimal columns typed as `decimal(18,2)`

---

### Step 7 — EF Core migrations

```bash
# Install dotnet-ef tool (once)
dotnet tool install --global dotnet-ef
export PATH="$PATH:$HOME/.dotnet/tools"

# Generate initial migration
dotnet ef migrations add InitialCreate \
  --project TicketingSystem.DAL.EF/TicketingSystem.DAL.EF.csproj \
  --startup-project TicketingSystem.App/TicketingSystem.App.csproj \
  --output-dir Migrations

# Fix cascade on EventSeat → OrderItem, then add second migration
dotnet ef migrations add CascadeOrderItem \
  --project TicketingSystem.DAL.EF/TicketingSystem.DAL.EF.csproj \
  --startup-project TicketingSystem.App/TicketingSystem.App.csproj \
  --output-dir Migrations

# Apply to DB
dotnet ef database update \
  --project TicketingSystem.DAL.EF/TicketingSystem.DAL.EF.csproj \
  --startup-project TicketingSystem.App/TicketingSystem.App.csproj
```

> **Gotcha:** `IDesignTimeDbContextFactory` is needed because the DbContext uses constructor injection. Without it, `dotnet ef` throws `Unable to resolve DbContextOptions`.

---

### Step 8 — Console demo (TicketingSystem.App/Program.cs)

Demonstrates the full CRUD lifecycle:

1. **Seed** — venue, section, seats, event, customer, event seats
2. **Read** — upcoming events, available seats, cheapest seat
3. **Create (with transaction)** — book seat → create order → mark seat as Sold → commit
4. **Read** — customer orders
5. **Update** — rename event title
6. **Delete (validation demo)** — tries to delete event and is blocked when sold tickets exist

---

## Enhancements Added (Mentor Extra Requirements)

### 1) Transactions

- Booking flow is fully transactional using Unit of Work methods:
       - `BeginTransactionAsync()`
       - `CommitTransactionAsync()`
       - `RollbackTransactionAsync()`
- If booking fails at any point, rollback is executed and the original exception is preserved.

### 2) Validation / Business Rules

- Added domain-level business exceptions:
       - `BusinessRuleViolationException`
       - `SoldTicketDeletionNotAllowedException`
       - `SeatUnavailableException`
- Added validated deletion in event repository:
       - `DeleteEventAsync(int eventId)` checks whether any seats for the event are already `Sold`.
       - If sold seats exist, deletion is blocked with `SoldTicketDeletionNotAllowedException`.
- Result: rule “cannot delete sold ticket/event with sold tickets” is enforced in DAL logic.

### 3) Query Logging

- Added EF Core SQL query logging to file.
- Logs are written at `Information` level with UTC timestamps.
- Log files are generated per run under:
       - `TicketingSystem.App/bin/Debug/net10.0/logs/ef-queries-*.log`
- Purpose: auditing/debugging of executed DB commands.

### 4) Error Handling

- Introduced centralized exception handling in `Program.cs` via `HandleException(Exception ex)`.
- Exceptions are translated into clear messages for:
       - `BusinessRuleViolationException`
       - `DbUpdateConcurrencyException`
       - `DbUpdateException`
       - `KeyNotFoundException`
       - fallback for unexpected exceptions
- App sets non-zero exit code on failure.

### 5) Demo Stability Improvement

- Added idempotent startup for local demo runs:
       - `EnsureDeletedAsync()` then `EnsureCreatedAsync()`.
- This avoids duplicate-seed failures across repeated runs.

---

### Step 9 — Run script

```bash
bash sources/run.sh
```

The script installs `dotnet-ef` if missing, applies migrations, then runs the demo app.

---

## Issues Encountered & Fixes

| Issue | Cause | Fix |
|-------|-------|-----|
| `dotnet add package` exit code 1 | Network blocked in sandboxed terminal | Ran with `requestUnsandboxedExecution` |
| `dotnet build` timeout (300s) | Sandbox terminal blocks restore network calls | Used unsandboxed execution for build/run |
| `dotnet ef` — `Unable to resolve DbContextOptions` | No `IDesignTimeDbContextFactory` | Added `TicketingDbContextFactory.cs` |
| `dotnet ef` — `startup project doesn't reference Design` | `Design` package missing from App | Added `Microsoft.EntityFrameworkCore.Design` to App.csproj |
| Delete event crash — `association severed` | `OrderItem.EventSeatId` FK was `Restrict` | Changed to `DeleteBehavior.Cascade` + new migration |
| Seed fails on rerun | Duplicate data violated unique constraints | Added `EnsureDeletedAsync()` before `EnsureCreatedAsync()` in demo startup |
| No business-level delete protection | Generic delete allowed invalid domain behavior | Added `DeleteEventAsync` validation + business exceptions |
| Query logs only to console warnings | No persistent auditing trail | Added timestamped SQL file logging at Information level |

---

## Self-Check Answers (04-self-check-questions.md)

1. **Steps to start designing a database** — clarify domain/use-cases → identify entities/relationships → choose SQL vs NoSQL → ER model → normalize → define indexes/constraints → validate with sample queries → plan migrations.

2. **When is the model correct?** — Supports all business operations without anomalies, integrity enforced by schema, queries are efficient, concurrency is safe, model is maintainable.

3. **What is DAL?** — Abstraction layer between business logic and storage; centralises CRUD, queries, transactions, mapping; improves testability and maintainability.

4. **SQL vs NoSQL selection** — SQL for relational integrity, joins, ACID; NoSQL for scale-out, flexible schema, document/key access; validate with prototyping and workload benchmarks.
