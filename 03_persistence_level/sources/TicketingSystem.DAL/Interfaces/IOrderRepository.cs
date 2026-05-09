using TicketingSystem.Domain.Entities;

namespace TicketingSystem.DAL.Interfaces;

public interface IOrderRepository : IRepository<Order>
{
    Task<Order?> GetWithItemsAsync(int orderId);
    Task<IEnumerable<Order>> GetByCustomerIdAsync(int customerId);
}
