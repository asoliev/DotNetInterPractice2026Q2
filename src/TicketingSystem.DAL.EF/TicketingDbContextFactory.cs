using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TicketingSystem.DAL.EF;

public class TicketingDbContextFactory : IDesignTimeDbContextFactory<TicketingDbContext>
{
    private static string BuildDatabasePath() => Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "..",
        "..",
        "..",
        "..",
        "ticketing.db"));

    public TicketingDbContext CreateDbContext(string[] args)
    {
        DbContextOptions<TicketingDbContext> options = new DbContextOptionsBuilder<TicketingDbContext>()
            .UseSqlite($"Data Source={BuildDatabasePath()}")
            .Options;

        return new TicketingDbContext(options);
    }
}
