namespace TicketingSystem.DAL.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IEventRepository Events { get; }
    IEventSeatRepository EventSeats { get; }
    IOrderRepository Orders { get; }
    ICustomerRepository Customers { get; }

    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
