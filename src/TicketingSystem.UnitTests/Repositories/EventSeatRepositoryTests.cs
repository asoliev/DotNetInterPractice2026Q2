using Microsoft.EntityFrameworkCore;
using TicketingSystem.DAL.EF;
using TicketingSystem.DAL.EF.Repositories;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Enums;

namespace TicketingSystem.UnitTests.Repositories;

public class EventSeatRepositoryTests : IDisposable
{
    private readonly TicketingDbContext _context;
    private readonly EventSeatRepository _repository;

    public EventSeatRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<TicketingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new TicketingDbContext(options);
        _repository = new EventSeatRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    private async Task<(int eventId, Seat seat1, Seat seat2)> SeedBaseAsync()
    {
        var venue = new Venue { Name = "V", Address = "A" };
        var section = new Section { Venue = venue, Name = "S", RowCount = 1, SeatsPerRow = 2 };
        var seat1 = new Seat { Section = section, Row = 1, Number = 1 };
        var seat2 = new Seat { Section = section, Row = 1, Number = 2 };
        var ev = new Event { Venue = venue, Title = "Concert", Date = DateTime.UtcNow.AddDays(5) };

        await _context.AddRangeAsync(venue, section, seat1, seat2, ev);
        await _context.SaveChangesAsync();

        return (ev.Id, seat1, seat2);
    }

    [Fact]
    public async Task GetByEventIdAsync_ReturnsSeatsForEvent()
    {
        var (eventId, seat1, seat2) = await SeedBaseAsync();

        await _context.EventSeats.AddRangeAsync(
            new EventSeat { EventId = eventId, SeatId = seat1.Id, Price = 10m, Status = SeatStatus.Available },
            new EventSeat { EventId = eventId, SeatId = seat2.Id, Price = 20m, Status = SeatStatus.Booked });
        await _context.SaveChangesAsync();

        var result = (await _repository.GetByEventIdAsync(eventId)).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, s => Assert.Equal(eventId, s.EventId));
    }

    [Fact]
    public async Task GetAvailableByEventIdAsync_FiltersByAvailableStatus()
    {
        var (eventId, seat1, seat2) = await SeedBaseAsync();

        await _context.EventSeats.AddRangeAsync(
            new EventSeat { EventId = eventId, SeatId = seat1.Id, Price = 10m, Status = SeatStatus.Available },
            new EventSeat { EventId = eventId, SeatId = seat2.Id, Price = 20m, Status = SeatStatus.Sold });
        await _context.SaveChangesAsync();

        var result = (await _repository.GetAvailableByEventIdAsync(eventId)).ToList();

        Assert.Single(result);
        Assert.Equal(SeatStatus.Available, result[0].Status);
    }

    [Fact]
    public async Task GetCheapestAvailableAsync_ReturnsLowestPriceAvailable()
    {
        var (eventId, seat1, seat2) = await SeedBaseAsync();

        await _context.EventSeats.AddRangeAsync(
            new EventSeat { EventId = eventId, SeatId = seat1.Id, Price = 30m, Status = SeatStatus.Available },
            new EventSeat { EventId = eventId, SeatId = seat2.Id, Price = 10m, Status = SeatStatus.Available });
        await _context.SaveChangesAsync();

        var result = await _repository.GetCheapestAvailableAsync(eventId);

        Assert.NotNull(result);
        Assert.Equal(10m, result!.Price);
    }

    [Fact]
    public async Task GetByEventAndSeatAsync_ReturnsMatchingSeat()
    {
        var (eventId, seat1, _) = await SeedBaseAsync();
        await _context.EventSeats.AddAsync(
            new EventSeat { EventId = eventId, SeatId = seat1.Id, Price = 30m, Status = SeatStatus.Available });
        await _context.SaveChangesAsync();

        var result = await _repository.GetByEventAndSeatAsync(eventId, seat1.Id);

        Assert.NotNull(result);
        Assert.Equal(seat1.Id, result!.SeatId);
    }

    [Fact]
    public async Task TryChangeStatusAsync_ReturnsFalse_WhenSeatMissing()
    {
        var changed = await _repository.TryChangeStatusAsync(999, SeatStatus.Available, SeatStatus.Booked);
        Assert.False(changed);
    }

    [Fact]
    public async Task TryChangeStatusAsync_ReturnsFalse_WhenExpectedStatusMismatch()
    {
        var (eventId, seat1, _) = await SeedBaseAsync();
        var entity = new EventSeat { EventId = eventId, SeatId = seat1.Id, Price = 10m, Status = SeatStatus.Booked };
        await _context.EventSeats.AddAsync(entity);
        await _context.SaveChangesAsync();

        var changed = await _repository.TryChangeStatusAsync(entity.Id, SeatStatus.Available, SeatStatus.Sold);

        Assert.False(changed);
        Assert.Equal(SeatStatus.Booked, (await _context.EventSeats.FindAsync(entity.Id))!.Status);
    }

    [Fact]
    public async Task TryChangeStatusAsync_ChangesStatus_WhenExpectedMatches()
    {
        var (eventId, seat1, _) = await SeedBaseAsync();
        var entity = new EventSeat { EventId = eventId, SeatId = seat1.Id, Price = 10m, Status = SeatStatus.Available };
        await _context.EventSeats.AddAsync(entity);
        await _context.SaveChangesAsync();

        var changed = await _repository.TryChangeStatusAsync(entity.Id, SeatStatus.Available, SeatStatus.Booked);

        Assert.True(changed);
        Assert.Equal(SeatStatus.Booked, (await _context.EventSeats.FindAsync(entity.Id))!.Status);
    }
}
