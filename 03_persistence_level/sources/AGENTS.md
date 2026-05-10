# AGENTS.md – TicketingSystem Codebase Guide

## Architecture Overview

Four-project .NET solution implementing a **Repository + Unit of Work** pattern over SQLite via EF Core (code-first).

```
TicketingSystem.Domain       – Plain entity/enum classes, no EF references
TicketingSystem.DAL          – Interfaces only (IRepository<T>, IUnitOfWork, specific repo interfaces)
TicketingSystem.DAL.EF       – EF Core implementations (DbContext, repositories, UnitOfWork, migrations)
TicketingSystem.Application  – Console demo/integration test; entry point
```

**Key rule:** `TicketingSystem.DAL` contains zero EF dependencies — it is the abstraction boundary. All EF-specific code lives exclusively in `TicketingSystem.DAL.EF`.

## Developer Workflows

**First run / DB setup:**
```bash
cd sources
bash run.sh        # installs dotnet-ef if missing, applies migrations, runs demo
```

**Apply a new migration after model changes:**
```bash
dotnet ef migrations add <MigrationName> \
  --project TicketingSystem.DAL.EF/TicketingSystem.DAL.EF.csproj \
  --startup-project TicketingSystem.Application/TicketingSystem.Application.csproj

dotnet ef database update \
  --project TicketingSystem.DAL.EF/TicketingSystem.DAL.EF.csproj \
  --startup-project TicketingSystem.Application/TicketingSystem.Application.csproj
```

**Run the demo:**
```bash
dotnet run --project TicketingSystem.Application/TicketingSystem.Application.csproj
```

The SQLite database file `ticketing.db` lives in `sources/` and is referenced as `Data Source=ticketing.db` from `Program.cs`.

## Key Patterns

### Adding a new repository method
1. Add the method signature to the interface in `TicketingSystem.DAL/Interfaces/I<Name>Repository.cs`.
2. Implement it in `TicketingSystem.DAL.EF/Repositories/<Name>Repository.cs`, extending `Repository<T>`.
3. Repositories inherit `_context` and `_dbSet` from `Repository<T>` — use these directly.

### Specialized repository example
```csharp
// EventSeatRepository uses optimistic status-change (no SaveChanges inside):
public async Task<bool> TryChangeStatusAsync(int id, SeatStatus expected, SeatStatus newStatus)
{
    var es = await _dbSet.FindAsync(id);
    if (es is null || es.Status != expected) return false;
    es.Status = newStatus;
    return true;   // caller must call uow.SaveChangesAsync()
}
```

### Transactions (booking flow)
Always wrap multi-step seat reservation in a transaction via `IUnitOfWork`:
```csharp
await uow.BeginTransactionAsync();
try { ...; await uow.SaveChangesAsync(); await uow.CommitTransactionAsync(); }
catch { await uow.RollbackTransactionAsync(); throw; }
```

### Enum storage
`SeatStatus` and `OrderStatus` are stored as `int` (`.HasConversion<int>()`). Do not rely on string values in queries or migrations.

### EF model configuration
All fluent configuration is in `TicketingDbContext.OnModelCreating` — there are **no separate `IEntityTypeConfiguration<T>` files** (the `Configurations/` folder is currently empty).

## Entity Relationships
- `Venue` → `Section[]` → `Seat[]` (cascade delete)
- `Event` links to `Venue` (restrict delete)
- `EventSeat` = junction of `Event` + `Seat`; unique index on `(EventId, SeatId)`
- `Order` → `OrderItem[]` (cascade); `OrderItem` has 1-to-1 with `EventSeat`
- `Venue`, `Section`, `Seat` are **not** exposed through `IUnitOfWork` — seed them directly via `context.Set<T>()` as shown in `Program.cs`

## Project Files of Interest
| File | Purpose |
|------|---------|
| `TicketingSystem.DAL/Interfaces/IUnitOfWork.cs` | Central access point for all repositories |
| `TicketingSystem.DAL.EF/TicketingDbContext.cs` | All EF mappings and constraints |
| `TicketingSystem.DAL.EF/UnitOfWork.cs` | Lazy-init repositories, transaction management |
| `TicketingSystem.DAL.EF/Migrations/` | EF migration history |
| `TicketingSystem.Application/Program.cs` | End-to-end usage demo (seed → CRUD → transaction) |
