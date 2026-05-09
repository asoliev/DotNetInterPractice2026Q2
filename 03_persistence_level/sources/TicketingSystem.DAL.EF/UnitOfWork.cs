using Microsoft.EntityFrameworkCore.Storage;
using TicketingSystem.DAL.EF.Repositories;
using TicketingSystem.DAL.Interfaces;

namespace TicketingSystem.DAL.EF;

public class UnitOfWork(TicketingDbContext context) : IUnitOfWork
{
    private IDbContextTransaction? _transaction;

    private IEventRepository? _events;
    private IEventSeatRepository? _eventSeats;
    private IOrderRepository? _orders;
    private ICustomerRepository? _customers;

    public IEventRepository Events =>
        _events ??= new EventRepository(context);

    public IEventSeatRepository EventSeats =>
        _eventSeats ??= new EventSeatRepository(context);

    public IOrderRepository Orders =>
        _orders ??= new OrderRepository(context);

    public ICustomerRepository Customers =>
        _customers ??= new CustomerRepository(context);

    public async Task<int> SaveChangesAsync() =>
        await context.SaveChangesAsync();

    public async Task BeginTransactionAsync() =>
        _transaction = await context.Database.BeginTransactionAsync();

    public async Task CommitTransactionAsync()
    {
        if (_transaction is null)
            throw new InvalidOperationException("No active transaction.");
        await _transaction.CommitAsync();
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction is null)
            throw new InvalidOperationException("No active transaction.");
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        context.Dispose();
    }
}
