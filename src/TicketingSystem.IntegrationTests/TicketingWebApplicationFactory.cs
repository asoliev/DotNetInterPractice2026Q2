using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TicketingSystem.DAL.EF;

namespace TicketingSystem.IntegrationTests;

/// <summary>
/// Custom WebApplicationFactory that replaces the SQLite file database with an
/// in-memory SQLite connection so that each test class gets a pristine,
/// isolated database with migrations applied and seed data inserted.
/// </summary>
public class TicketingWebApplicationFactory : WebApplicationFactory<Program>
{
    // Keep the connection alive for the lifetime of the factory so the
    // in-memory SQLite database is not dropped between requests.
    private readonly SqliteConnection _keepAliveConnection =
        new("Data Source=:memory:;Mode=Memory;Cache=Shared;");

    public TicketingWebApplicationFactory()
    {
        _keepAliveConnection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing TicketingDbContext registration.
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<TicketingDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            // Add SQLite in-memory using the shared connection.
            services.AddDbContext<TicketingDbContext>(options =>
                options.UseSqlite(_keepAliveConnection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _keepAliveConnection.Dispose();
    }
}
