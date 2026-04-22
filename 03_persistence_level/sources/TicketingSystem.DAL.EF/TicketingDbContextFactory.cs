using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TicketingSystem.DAL.EF;

public class TicketingDbContextFactory : IDesignTimeDbContextFactory<TicketingDbContext>
{
    public TicketingDbContext CreateDbContext(string[] args)
    {
        DbContextOptions<TicketingDbContext> options = new DbContextOptionsBuilder<TicketingDbContext>()
            .UseSqlite("Data Source=ticketing.db")
            .Options;

        return new TicketingDbContext(options);
    }
}
