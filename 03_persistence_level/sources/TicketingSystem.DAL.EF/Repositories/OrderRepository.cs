using Microsoft.EntityFrameworkCore;
using TicketingSystem.DAL.Interfaces;
using TicketingSystem.Domain.Entities;

namespace TicketingSystem.DAL.EF.Repositories;

public class OrderRepository(TicketingDbContext context) : Repository<Order>(context), IOrderRepository
{
    public async Task<Order?> GetWithItemsAsync(int orderId) =>
        await DbSet
            .Include(o => o.Items).ThenInclude(i => i.EventSeat)
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == orderId);

    public async Task<IEnumerable<Order>> GetByCustomerIdAsync(int customerId) =>
        await DbSet
            .Include(o => o.Items)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
}
