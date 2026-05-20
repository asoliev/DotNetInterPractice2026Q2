using Microsoft.EntityFrameworkCore;
using TicketingSystem.DAL.EF;
using TicketingSystem.DAL.EF.Repositories;
using TicketingSystem.Domain.Entities;

namespace TicketingSystem.UnitTests.Repositories;

public class RepositoryTests : IDisposable
{
    private readonly TicketingDbContext _context;

    public RepositoryTests()
    {
        var options = new DbContextOptionsBuilder<TicketingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new TicketingDbContext(options);
    }

    public void Dispose() => _context.Dispose();

    private Repository<Event> CreateRepository() => new(_context);

    private static Event MakeEvent(string title = "Test Event") =>
        new() { VenueId = 0, Title = title, Description = "desc", Date = DateTime.UtcNow.AddDays(7) };

    [Fact]
    public async Task AddAsync_PersistsEntity()
    {
        var repo = CreateRepository();
        var ev = MakeEvent();

        await repo.AddAsync(ev);
        await _context.SaveChangesAsync();

        Assert.Equal(1, await _context.Events.CountAsync());
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsCorrectEntity()
    {
        var repo = CreateRepository();
        var ev = MakeEvent();
        await repo.AddAsync(ev);
        await _context.SaveChangesAsync();

        var found = await repo.GetByIdAsync(ev.Id);

        Assert.NotNull(found);
        Assert.Equal("Test Event", found.Title);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        var repo = CreateRepository();

        var found = await repo.GetByIdAsync(9999);

        Assert.Null(found);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        var repo = CreateRepository();
        await repo.AddAsync(MakeEvent("Event 1"));
        await repo.AddAsync(MakeEvent("Event 2"));
        await _context.SaveChangesAsync();

        var all = await repo.GetAllAsync();

        Assert.Equal(2, all.Count());
    }

    [Fact]
    public async Task FindAsync_FiltersByPredicate()
    {
        var repo = CreateRepository();
        await repo.AddAsync(MakeEvent("Rock Night"));
        await repo.AddAsync(MakeEvent("Jazz Evening"));
        await _context.SaveChangesAsync();

        var results = await repo.FindAsync(e => e.Title.Contains("Rock"));

        Assert.Single(results);
        Assert.Equal("Rock Night", results.First().Title);
    }

    [Fact]
    public async Task Update_ChangesEntityInDatabase()
    {
        var repo = CreateRepository();
        var ev = MakeEvent("Original Title");
        await repo.AddAsync(ev);
        await _context.SaveChangesAsync();

        ev.Title = "Updated Title";
        repo.Update(ev);
        await _context.SaveChangesAsync();

        var updated = await repo.GetByIdAsync(ev.Id);
        Assert.Equal("Updated Title", updated!.Title);
    }

    [Fact]
    public async Task Delete_RemovesEntityFromDatabase()
    {
        var repo = CreateRepository();
        var ev = MakeEvent();
        await repo.AddAsync(ev);
        await _context.SaveChangesAsync();

        repo.Delete(ev);
        await _context.SaveChangesAsync();

        Assert.Equal(0, await _context.Events.CountAsync());
    }
}
