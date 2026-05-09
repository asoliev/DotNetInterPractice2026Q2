using Microsoft.EntityFrameworkCore;
using TicketingSystem.DAL.Interfaces;
using TicketingSystem.Domain.Entities;

namespace TicketingSystem.DAL.EF.Repositories;

public class CustomerRepository(TicketingDbContext context) : Repository<Customer>(context), ICustomerRepository
{
    public async Task<Customer?> GetByEmailAsync(string email) =>
        await DbSet.FirstOrDefaultAsync(c => c.Email == email);
}
