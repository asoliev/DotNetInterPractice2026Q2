using Microsoft.EntityFrameworkCore;
using TicketingSystem.DAL.EF;
using TicketingSystem.DAL.EF.Repositories;
using TicketingSystem.DAL.Exceptions;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Enums;

namespace TicketingSystem.UnitTests.Repositories;

public class EventRepositoryTests : IDisposable
{
    private readonly TicketingDbContext _context;
    private readonly EventRepository _repository;

    public EventRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<TicketingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new TicketingDbContext(options);
        _repository = new EventRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task GetUpcomingAsync_ReturnsOnlyFutureEventsOrderedByDate()
    {
        var venue = new Venue { Name = "V", Address = "A" };
        await _context.Venues.AddAsync(venue);

        var past = new Event { Venue = venue, Title = "Past", Date = DateTime.UtcNow.AddDays(-1) };
        var soon = new Event { Venue = venue, Title = "Soon", Date = DateTime.UtcNow.AddDays(1) };
        var later = new Event { Venue = venue, Title = "Later", Date = DateTime.UtcNow.AddDays(3) };

        await _context.Events.AddRangeAsync(past, later, soon);
        await _context.SaveChangesAsync();

        var upcoming = (await _repository.GetUpcomingAsync()).ToList();

        Assert.Equal(2, upcoming.Count);
        Assert.Equal("Soon", upcoming[0].Title);
        Assert.Equal("Later", upcoming[1].Title);
    }

    [Fact]
    public async Task GetWithSeatsAsync_ReturnsEventWithSeatGraph()
    {
        var venue = new Venue { Name = "V", Address = "A" };
        var section = new Section { Venue = venue, Name = "S", RowCount = 1, SeatsPerRow = 1 };
        var seat = new Seat { Section = section, Row = 1, Number = 1 };
        var ev = new Event { Venue = venue, Title = "Concert", Date = DateTime.UtcNow.AddDays(2) };
        var eventSeat = new EventSeat { Event = ev, Seat = seat, Price = 50m, Status = SeatStatus.Available };

        await _context.AddRangeAsync(venue, section, seat, ev, eventSeat);
        await _context.SaveChangesAsync();

        var result = await _repository.GetWithSeatsAsync(ev.Id);

        Assert.NotNull(result);
        Assert.Single(result!.EventSeats);
        Assert.NotNull(result.EventSeats.First().Seat.Section);
    }

    [Fact]
    public async Task DeleteEventAsync_ThrowsKeyNotFound_WhenEventMissing()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _repository.DeleteEventAsync(999));
    }

    [Fact]
    public async Task DeleteEventAsync_ThrowsBusinessRule_WhenSoldSeatExists()
    {
        var venue = new Venue { Name = "V", Address = "A" };
        var section = new Section { Venue = venue, Name = "S", RowCount = 1, SeatsPerRow = 1 };
        var seat = new Seat { Section = section, Row = 1, Number = 1 };
        var ev = new Event { Venue = venue, Title = "Concert", Date = DateTime.UtcNow.AddDays(2) };
        var soldEventSeat = new EventSeat { Event = ev, Seat = seat, Price = 50m, Status = SeatStatus.Sold };

        await _context.AddRangeAsync(venue, section, seat, ev, soldEventSeat);
        await _context.SaveChangesAsync();

        await Assert.ThrowsAsync<SoldTicketDeletionNotAllowedException>(() => _repository.DeleteEventAsync(ev.Id));
    }

    [Fact]
    public async Task DeleteEventAsync_RemovesEvent_WhenNoSoldSeats()
    {
        var venue = new Venue { Name = "V", Address = "A" };
        var section = new Section { Venue = venue, Name = "S", RowCount = 1, SeatsPerRow = 1 };
        var seat = new Seat { Section = section, Row = 1, Number = 1 };
        var ev = new Event { Venue = venue, Title = "Concert", Date = DateTime.UtcNow.AddDays(2) };
        var availableEventSeat = new EventSeat { Event = ev, Seat = seat, Price = 50m, Status = SeatStatus.Available };

        await _context.AddRangeAsync(venue, section, seat, ev, availableEventSeat);
        await _context.SaveChangesAsync();

        await _repository.DeleteEventAsync(ev.Id);
        await _context.SaveChangesAsync();

        Assert.Null(await _context.Events.FindAsync(ev.Id));
    }
}
