using Microsoft.EntityFrameworkCore;
using TicketingSystem.DAL.EF;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Enums;

namespace TicketingSystem.AsyncApi;

public static class SeedData
{
    public static async Task InitializeAsync(TicketingDbContext dbContext)
    {
        await dbContext.Database.EnsureCreatedAsync();

        if (await dbContext.Venues.AnyAsync() || await dbContext.Events.AnyAsync())
            return;

        var venue = new Venue
        {
            Name = "City Hall Arena",
            Address = "221 Main Street"
        };

        var sectionA = new Section
        {
            Name = "A",
            RowCount = 2,
            SeatsPerRow = 4,
            Venue = venue
        };

        var sectionB = new Section
        {
            Name = "B",
            RowCount = 2,
            SeatsPerRow = 4,
            Venue = venue
        };

        var seats = new List<Seat>();
        CreateSeats(sectionA, seats);
        CreateSeats(sectionB, seats);

        var eventEntity = new Event
        {
            Venue = venue,
            Title = "Symphonic Evening",
            Description = "Classical music gala",
            Date = DateTime.UtcNow.AddDays(10)
        };

        await dbContext.Venues.AddAsync(venue);
        await dbContext.Sections.AddRangeAsync(sectionA, sectionB);
        await dbContext.Seats.AddRangeAsync(seats);
        await dbContext.Events.AddAsync(eventEntity);
        await dbContext.SaveChangesAsync();

        var eventSeats = seats.Select((seat, idx) => new EventSeat
        {
            EventId = eventEntity.Id,
            SeatId = seat.Id,
            Price = idx < seats.Count / 2 ? 50m : 35m,
            Status = SeatStatus.Available
        });

        await dbContext.EventSeats.AddRangeAsync(eventSeats);
        await dbContext.SaveChangesAsync();
    }

    private static void CreateSeats(Section section, List<Seat> seats)
    {
        for (int row = 1; row <= section.RowCount; row++)
        {
            for (int number = 1; number <= section.SeatsPerRow; number++)
            {
                seats.Add(new Seat
                {
                    Section = section,
                    Row = row,
                    Number = number
                });
            }
        }
    }
}