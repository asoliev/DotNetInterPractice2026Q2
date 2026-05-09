using TicketingSystem.Domain.Entities;

namespace TicketingSystem.DAL.Interfaces;

public interface ICustomerRepository : IRepository<Customer>
{
    Task<Customer?> GetByEmailAsync(string email);
}
